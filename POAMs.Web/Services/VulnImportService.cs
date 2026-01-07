using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using POAMs.Web.Data;
using POAMs.Web.Models.Domain;
using POAMs.Web.Models.ViewModels;

namespace POAMs.Web.Services;

public class VulnImportService : IVulnImportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VulnImportService> _logger;

    public VulnImportService(ApplicationDbContext context, ILogger<VulnImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<VulnImportResultViewModel> ImportAsync(Stream fileStream, string fileName, string importedBy)
    {
        var result = new VulnImportResultViewModel();

        try
        {
            using var reader = new StreamReader(fileStream);
            var json = await reader.ReadToEndAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var export = JsonSerializer.Deserialize<VulnExportJson>(json, options);

            if (export == null)
            {
                result.Success = false;
                result.Message = "Failed to parse JSON file. The file may be empty or malformed.";
                return result;
            }

            // Create the session
            var session = new VulnImportSession
            {
                ImportDate = DateTime.UtcNow,
                ImportedBy = importedBy,
                SourceApplication = export.ExportMetadata?.ApplicationName ?? "Unknown",
                ExportType = export.ExportMetadata?.ExportType ?? "Unknown",
                FileName = fileName,
                TotalSites = export.Summary?.TotalSites ?? 0,
                TotalHosts = export.Summary?.TotalHosts ?? 0
            };

            // Track CCIs for summary
            var allCCIs = new HashSet<string>();
            var ccisWithOpenFindings = new HashSet<string>();

            // Process sites
            if (export.Sites != null)
            {
                foreach (var site in export.Sites)
                {
                    if (site.Hosts == null) continue;

                    foreach (var hostData in site.Hosts)
                    {
                        var host = new VulnImportHost
                        {
                            SiteName = site.SiteSummary != null ? $"Site {export.Sites.IndexOf(site) + 1}" : "Unknown",
                            DnsName = hostData.DnsName ?? hostData.DisplayName ?? "Unknown",
                            IpAddress = hostData.IpAddress ?? "",
                            OperatingSystem = hostData.OperatingSystem ?? "",
                            AssetType = hostData.AssetType ?? ""
                        };

                        // Process STIG checklists
                        if (hostData.StigChecklists != null)
                        {
                            foreach (var checklist in hostData.StigChecklists)
                            {
                                session.TotalStigChecklists++;

                                if (checklist.Results != null)
                                {
                                    foreach (var stigResult in checklist.Results)
                                    {
                                        var stigRecord = new VulnImportStigResult
                                        {
                                            StigId = checklist.BenchmarkInfo?.StigId ?? "",
                                            StigTitle = checklist.BenchmarkInfo?.Title ?? checklist.Title ?? "",
                                            VulnId = stigResult.VulnId ?? "",
                                            RuleId = stigResult.RuleId ?? "",
                                            RuleTitle = stigResult.RuleTitle ?? "",
                                            Severity = stigResult.Severity ?? "Medium",
                                            Status = stigResult.Status ?? "NotReviewed",
                                            CCIs = JsonSerializer.Serialize(stigResult.CCIs ?? new List<string>())
                                        };

                                        host.StigResults.Add(stigRecord);
                                        session.TotalStigFindings++;
                                        result.StigFindingsProcessed++;

                                        // Track CCIs
                                        if (stigResult.CCIs != null)
                                        {
                                            foreach (var cci in stigResult.CCIs)
                                            {
                                                allCCIs.Add(cci);
                                                if (stigResult.Status == "Open")
                                                {
                                                    ccisWithOpenFindings.Add(cci);
                                                }
                                            }
                                        }

                                        // Update summary counts
                                        switch (stigResult.Status)
                                        {
                                            case "Open":
                                                session.StigOpen++;
                                                break;
                                            case "NotAFinding":
                                                session.StigNotAFinding++;
                                                break;
                                            case "NotApplicable":
                                                session.StigNotApplicable++;
                                                break;
                                            default:
                                                session.StigNotReviewed++;
                                                break;
                                        }
                                    }
                                }
                            }
                        }

                        // Process Nessus vulnerabilities
                        if (hostData.NessusVulnerabilities != null)
                        {
                            foreach (var nessusVuln in hostData.NessusVulnerabilities)
                            {
                                var vulnRecord = new VulnImportNessusVuln
                                {
                                    PluginId = nessusVuln.PluginId,
                                    PluginName = nessusVuln.PluginName ?? "",
                                    Family = nessusVuln.Family ?? "",
                                    Severity = nessusVuln.Severity ?? "Info",
                                    CVE = nessusVuln.CVE ?? "",
                                    Synopsis = nessusVuln.Synopsis ?? "",
                                    IsExploitable = nessusVuln.IsExploitable
                                };

                                host.NessusVulns.Add(vulnRecord);
                                session.TotalNessusVulns++;
                                result.NessusVulnsProcessed++;

                                // Update severity counts
                                switch (nessusVuln.Severity?.ToLower())
                                {
                                    case "critical":
                                        session.NessusCritical++;
                                        break;
                                    case "high":
                                        session.NessusHigh++;
                                        break;
                                    case "medium":
                                        session.NessusMedium++;
                                        break;
                                    case "low":
                                        session.NessusLow++;
                                        break;
                                    default:
                                        session.NessusInfo++;
                                        break;
                                }
                            }
                        }

                        session.Hosts.Add(host);
                        result.HostsProcessed++;
                    }
                }
            }

            session.UniqueCCIs = allCCIs.Count;
            session.CCIsWithOpenFindings = ccisWithOpenFindings.Count;

            _context.VulnImportSessions.Add(session);
            await _context.SaveChangesAsync();

            result.Success = true;
            result.SessionId = session.Id;
            result.Message = $"Successfully imported {result.HostsProcessed} hosts with {result.StigFindingsProcessed} STIG findings and {result.NessusVulnsProcessed} Nessus vulnerabilities.";

            _logger.LogInformation("Imported vulnerability data: {Hosts} hosts, {STIG} STIG findings, {Nessus} Nessus vulns",
                result.HostsProcessed, result.StigFindingsProcessed, result.NessusVulnsProcessed);
        }
        catch (JsonException ex)
        {
            result.Success = false;
            result.Message = "Failed to parse JSON file. Please ensure the file is a valid Vulnerability Management export.";
            result.Errors.Add(ex.Message);
            _logger.LogError(ex, "JSON parsing error during import");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Import failed: {ex.Message}";
            result.Errors.Add(ex.ToString());
            _logger.LogError(ex, "Error during vulnerability import");
        }

        return result;
    }

    public async Task<VulnImportSession?> GetLatestSessionAsync()
    {
        return await _context.VulnImportSessions
            .OrderByDescending(s => s.ImportDate)
            .FirstOrDefaultAsync();
    }

    public async Task<VulnImportSession?> GetSessionAsync(int id)
    {
        return await _context.VulnImportSessions
            .Include(s => s.Hosts)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<VulnImportSessionListItem>> GetAllSessionsAsync()
    {
        return await _context.VulnImportSessions
            .OrderByDescending(s => s.ImportDate)
            .Select(s => new VulnImportSessionListItem
            {
                Id = s.Id,
                ImportDate = s.ImportDate,
                ImportedBy = s.ImportedBy,
                SourceApplication = s.SourceApplication,
                FileName = s.FileName,
                TotalHosts = s.TotalHosts,
                TotalNessusVulns = s.TotalNessusVulns,
                NessusCritical = s.NessusCritical,
                NessusHigh = s.NessusHigh,
                TotalStigFindings = s.TotalStigFindings,
                StigOpen = s.StigOpen,
                StigCompliancePercentage = s.StigCompliancePercentage,
                RiskLevel = s.RiskLevel
            })
            .ToListAsync();
    }

    public async Task DeleteSessionAsync(int id)
    {
        var session = await _context.VulnImportSessions.FindAsync(id);
        if (session != null)
        {
            _context.VulnImportSessions.Remove(session);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted vulnerability import session {SessionId}", id);
        }
    }

    public async Task<VulnDashboardViewModel> GetDashboardAsync(int sessionId)
    {
        var session = await _context.VulnImportSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
        {
            throw new ArgumentException($"Session {sessionId} not found");
        }

        var dashboard = new VulnDashboardViewModel
        {
            Session = session,
            GeneratedAt = DateTime.UtcNow
        };

        // Package overview
        dashboard.PackageOverview = new PackageOverviewViewModel
        {
            TotalSites = session.TotalSites,
            TotalHosts = session.TotalHosts,
            OpenVulnerabilities = session.NessusCritical + session.NessusHigh + session.NessusMedium + session.NessusLow,
            CriticalVulnerabilities = session.NessusCritical,
            HighVulnerabilities = session.NessusHigh,
            ExploitableVulnerabilities = await _context.VulnImportNessusVulns
                .Where(v => v.Host.VulnImportSessionId == sessionId && v.IsExploitable)
                .CountAsync(),
            OverallCompliancePercentage = session.StigCompliancePercentage,
            RiskLevel = session.RiskLevel,
            VulnerabilityRiskScore = CalculateRiskScore(session)
        };

        // Get metrics
        dashboard.NessusMetrics = await GetNessusMetricsAsync(sessionId);
        dashboard.StigMetrics = await GetStigMetricsAsync(sessionId);
        dashboard.SiteBreakdown = await GetSiteBreakdownAsync(sessionId);

        // Get available systems for assessment creation
        dashboard.AvailableSystems = await _context.Systems.OrderBy(s => s.SystemName).ToListAsync();

        return dashboard;
    }

    public async Task<NessusMetricsViewModel> GetNessusMetricsAsync(int sessionId)
    {
        var session = await _context.VulnImportSessions.FindAsync(sessionId);
        if (session == null) return new NessusMetricsViewModel();

        var vulns = await _context.VulnImportNessusVulns
            .Where(v => v.Host.VulnImportSessionId == sessionId)
            .ToListAsync();

        var metrics = new NessusMetricsViewModel
        {
            Total = session.TotalNessusVulns,
            Critical = session.NessusCritical,
            High = session.NessusHigh,
            Medium = session.NessusMedium,
            Low = session.NessusLow,
            Info = session.NessusInfo,
            ExploitableCount = vulns.Count(v => v.IsExploitable),
            UniquePluginCount = vulns.Select(v => v.PluginId).Distinct().Count(),
            UniqueCveCount = vulns.Where(v => !string.IsNullOrEmpty(v.CVE)).Select(v => v.CVE).Distinct().Count(),
            StatusData = new[] { session.TotalNessusVulns, 0, 0, 0, 0 } // All open by default
        };

        // Top vulnerabilities (by affected host count)
        metrics.TopVulnerabilities = vulns
            .Where(v => v.Severity?.ToLower() != "info")
            .GroupBy(v => new { v.PluginId, v.PluginName, v.Severity, v.CVE, v.IsExploitable })
            .Select(g => new TopVulnerabilityViewModel
            {
                PluginId = g.Key.PluginId,
                PluginName = g.Key.PluginName,
                Severity = g.Key.Severity,
                CVE = g.Key.CVE,
                AffectedHostCount = g.Count(),
                IsExploitable = g.Key.IsExploitable
            })
            .OrderByDescending(v => v.Severity == "Critical" ? 4 : v.Severity == "High" ? 3 : v.Severity == "Medium" ? 2 : 1)
            .ThenByDescending(v => v.AffectedHostCount)
            .Take(10)
            .ToList();

        // By family
        metrics.ByFamily = vulns
            .GroupBy(v => v.Family)
            .Select(g => new PluginFamilySummary
            {
                Family = g.Key,
                Count = g.Count(),
                CriticalCount = g.Count(v => v.Severity?.ToLower() == "critical"),
                HighCount = g.Count(v => v.Severity?.ToLower() == "high")
            })
            .OrderByDescending(f => f.CriticalCount + f.HighCount)
            .ThenByDescending(f => f.Count)
            .Take(10)
            .ToList();

        return metrics;
    }

    public async Task<StigMetricsViewModel> GetStigMetricsAsync(int sessionId)
    {
        var session = await _context.VulnImportSessions.FindAsync(sessionId);
        if (session == null) return new StigMetricsViewModel();

        var stigResults = await _context.VulnImportStigResults
            .Where(r => r.Host.VulnImportSessionId == sessionId)
            .ToListAsync();

        var metrics = new StigMetricsViewModel
        {
            TotalRules = session.TotalStigFindings,
            Open = session.StigOpen,
            NotAFinding = session.StigNotAFinding,
            NotApplicable = session.StigNotApplicable,
            NotReviewed = session.StigNotReviewed,
            CompliancePercentage = session.StigCompliancePercentage
        };

        // CAT breakdown
        foreach (var result in stigResults)
        {
            var severity = result.Severity?.ToLower();
            var isOpen = result.Status == "Open";

            if (severity == "high")
            {
                metrics.CatITotal++;
                if (isOpen) metrics.CatIOpen++;
            }
            else if (severity == "medium")
            {
                metrics.CatIITotal++;
                if (isOpen) metrics.CatIIOpen++;
            }
            else if (severity == "low")
            {
                metrics.CatIIITotal++;
                if (isOpen) metrics.CatIIIOpen++;
            }
        }

        // By severity
        metrics.BySeverity = stigResults
            .GroupBy(r => r.Severity)
            .Select(g => new StigSeveritySummary
            {
                Severity = g.Key,
                CatLevel = g.Key?.ToLower() switch
                {
                    "high" => "CAT I",
                    "medium" => "CAT II",
                    "low" => "CAT III",
                    _ => "Unknown"
                },
                Total = g.Count(),
                Open = g.Count(r => r.Status == "Open"),
                NotAFinding = g.Count(r => r.Status == "NotAFinding")
            })
            .OrderBy(s => s.Severity == "High" ? 1 : s.Severity == "Medium" ? 2 : 3)
            .ToList();

        // By benchmark
        metrics.BenchmarkCompliance = stigResults
            .GroupBy(r => new { r.StigId, r.StigTitle })
            .Select(g => new BenchmarkComplianceViewModel
            {
                BenchmarkName = g.Key.StigTitle,
                Version = "",
                ChecklistCount = 1,
                TotalRules = g.Count(),
                OpenCount = g.Count(r => r.Status == "Open"),
                NotAFindingCount = g.Count(r => r.Status == "NotAFinding"),
                CompliancePercentage = g.Count() > 0
                    ? Math.Round((decimal)g.Count(r => r.Status == "NotAFinding") / g.Count() * 100, 1)
                    : 0
            })
            .OrderByDescending(b => b.OpenCount)
            .Take(8)
            .ToList();

        return metrics;
    }

    public async Task<List<SiteBreakdownViewModel>> GetSiteBreakdownAsync(int sessionId)
    {
        var hosts = await _context.VulnImportHosts
            .Where(h => h.VulnImportSessionId == sessionId)
            .Include(h => h.StigResults)
            .Include(h => h.NessusVulns)
            .ToListAsync();

        return hosts
            .GroupBy(h => h.SiteName)
            .Select(g =>
            {
                var siteHosts = g.ToList();
                var stigResults = siteHosts.SelectMany(h => h.StigResults).ToList();
                var nessusVulns = siteHosts.SelectMany(h => h.NessusVulns).ToList();

                var openCount = stigResults.Count(r => r.Status == "Open");
                var nafCount = stigResults.Count(r => r.Status == "NotAFinding");
                var totalEvaluated = stigResults.Count(r => r.Status == "Open" || r.Status == "NotAFinding");

                return new SiteBreakdownViewModel
                {
                    SiteName = g.Key,
                    TotalHosts = siteHosts.Count,
                    CriticalCount = nessusVulns.Count(v => v.Severity?.ToLower() == "critical"),
                    HighCount = nessusVulns.Count(v => v.Severity?.ToLower() == "high"),
                    MediumCount = nessusVulns.Count(v => v.Severity?.ToLower() == "medium"),
                    OpenVulnerabilities = nessusVulns.Count(v => v.Severity?.ToLower() != "info"),
                    StigOpenFindings = openCount,
                    StigTotalFindings = stigResults.Count,
                    StigCompliancePercentage = totalEvaluated > 0
                        ? Math.Round((decimal)nafCount / totalEvaluated * 100, 1)
                        : 0,
                    RiskLevel = CalculateSiteRiskLevel(
                        nessusVulns.Count(v => v.Severity?.ToLower() == "critical"),
                        nessusVulns.Count(v => v.Severity?.ToLower() == "high"),
                        openCount)
                };
            })
            .OrderByDescending(s => s.CriticalCount + s.HighCount)
            .ThenByDescending(s => s.StigOpenFindings)
            .ToList();
    }

    public async Task<List<HostBreakdownViewModel>> GetHostBreakdownAsync(int sessionId, string siteName)
    {
        var hosts = await _context.VulnImportHosts
            .Where(h => h.VulnImportSessionId == sessionId && h.SiteName == siteName)
            .Include(h => h.StigResults)
            .Include(h => h.NessusVulns)
            .ToListAsync();

        return hosts.Select(h =>
        {
            var openCount = h.StigResults.Count(r => r.Status == "Open");
            var nafCount = h.StigResults.Count(r => r.Status == "NotAFinding");
            var totalEvaluated = h.StigResults.Count(r => r.Status == "Open" || r.Status == "NotAFinding");

            return new HostBreakdownViewModel
            {
                HostId = h.Id,
                HostName = h.DnsName,
                IPAddress = h.IpAddress,
                OperatingSystem = h.OperatingSystem,
                AssetType = h.AssetType,
                CriticalCount = h.NessusVulns.Count(v => v.Severity?.ToLower() == "critical"),
                HighCount = h.NessusVulns.Count(v => v.Severity?.ToLower() == "high"),
                MediumCount = h.NessusVulns.Count(v => v.Severity?.ToLower() == "medium"),
                OpenVulnerabilities = h.NessusVulns.Count(v => v.Severity?.ToLower() != "info"),
                StigOpenFindings = openCount,
                StigTotalFindings = h.StigResults.Count,
                StigCompliancePercentage = totalEvaluated > 0
                    ? Math.Round((decimal)nafCount / totalEvaluated * 100, 1)
                    : 0,
                RiskLevel = CalculateSiteRiskLevel(
                    h.NessusVulns.Count(v => v.Severity?.ToLower() == "critical"),
                    h.NessusVulns.Count(v => v.Severity?.ToLower() == "high"),
                    openCount)
            };
        })
        .OrderByDescending(h => h.CriticalCount + h.HighCount)
        .ThenByDescending(h => h.StigOpenFindings)
        .ToList();
    }

    private static decimal CalculateRiskScore(VulnImportSession session)
    {
        // Simple risk score calculation (0-100)
        var score = 0m;
        score += session.NessusCritical * 10;
        score += session.NessusHigh * 5;
        score += session.NessusMedium * 2;
        score += session.NessusLow * 0.5m;
        score += session.StigOpen * 3;

        // Cap at 100
        return Math.Min(100, score);
    }

    private static string CalculateSiteRiskLevel(int critical, int high, int stigOpen)
    {
        if (critical > 0) return "Critical";
        if (high > 0 || stigOpen > 5) return "High";
        if (stigOpen > 0) return "Medium";
        return "Low";
    }

    #region JSON DTOs for parsing

    private class VulnExportJson
    {
        public ExportMetadataJson? ExportMetadata { get; set; }
        public SummaryJson? Summary { get; set; }
        public List<SiteJson>? Sites { get; set; }
    }

    private class ExportMetadataJson
    {
        public string? ExportDate { get; set; }
        public string? ExportedBy { get; set; }
        public string? ApplicationName { get; set; }
        public string? ExportType { get; set; }
    }

    private class SummaryJson
    {
        public int TotalSites { get; set; }
        public int TotalHosts { get; set; }
    }

    private class SiteJson
    {
        public SiteSummaryJson? SiteSummary { get; set; }
        public List<HostJson>? Hosts { get; set; }
    }

    private class SiteSummaryJson
    {
        public int TotalHosts { get; set; }
    }

    private class HostJson
    {
        public int Id { get; set; }
        public string? DnsName { get; set; }
        public string? DisplayName { get; set; }
        public string? IpAddress { get; set; }
        public string? OperatingSystem { get; set; }
        public string? AssetType { get; set; }
        public List<StigChecklistJson>? StigChecklists { get; set; }
        public List<NessusVulnJson>? NessusVulnerabilities { get; set; }
    }

    private class StigChecklistJson
    {
        public BenchmarkInfoJson? BenchmarkInfo { get; set; }
        public string? Title { get; set; }
        public List<StigResultJson>? Results { get; set; }
    }

    private class BenchmarkInfoJson
    {
        public string? StigId { get; set; }
        public string? Title { get; set; }
        public string? Version { get; set; }
    }

    private class StigResultJson
    {
        public string? VulnId { get; set; }
        public string? RuleId { get; set; }
        public string? RuleTitle { get; set; }
        public string? Severity { get; set; }
        public string? Status { get; set; }
        public List<string>? CCIs { get; set; }
    }

    private class NessusVulnJson
    {
        public int PluginId { get; set; }
        public string? PluginName { get; set; }
        public string? Family { get; set; }
        public string? Severity { get; set; }
        public string? CVE { get; set; }
        public string? Synopsis { get; set; }
        public bool IsExploitable { get; set; }
    }

    #endregion
}
