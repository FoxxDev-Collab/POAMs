using POAMs.Web.Models.Domain;

namespace POAMs.Web.Models.ViewModels;

/// <summary>
/// Result of an import operation
/// </summary>
public class VulnImportResultViewModel
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int HostsProcessed { get; set; }
    public int StigFindingsProcessed { get; set; }
    public int NessusVulnsProcessed { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public int SessionId { get; set; }
}

/// <summary>
/// Main dashboard view model
/// </summary>
public class VulnDashboardViewModel
{
    public VulnImportSession Session { get; set; } = null!;

    // Executive summary
    public PackageOverviewViewModel PackageOverview { get; set; } = new();

    // Nessus metrics
    public NessusMetricsViewModel NessusMetrics { get; set; } = new();

    // STIG metrics
    public StigMetricsViewModel StigMetrics { get; set; } = new();

    // NIST Compliance (from CCI mapping)
    public List<NISTFamilyComplianceViewModel> NISTCompliance { get; set; } = new();

    // Site breakdown
    public List<SiteBreakdownViewModel> SiteBreakdown { get; set; } = new();

    // Available systems for assessment creation
    public List<SystemInfo> AvailableSystems { get; set; } = new();

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Package-level overview statistics
/// </summary>
public class PackageOverviewViewModel
{
    public int TotalSites { get; set; }
    public int TotalHosts { get; set; }
    public int OpenVulnerabilities { get; set; }
    public int CriticalVulnerabilities { get; set; }
    public int HighVulnerabilities { get; set; }
    public int ExploitableVulnerabilities { get; set; }
    public decimal OverallCompliancePercentage { get; set; }
    public decimal VulnerabilityRiskScore { get; set; }
    public string RiskLevel { get; set; } = "Minimal";
}

/// <summary>
/// Nessus vulnerability metrics
/// </summary>
public class NessusMetricsViewModel
{
    public int Total { get; set; }
    public int Critical { get; set; }
    public int High { get; set; }
    public int Medium { get; set; }
    public int Low { get; set; }
    public int Info { get; set; }
    public int ExploitableCount { get; set; }
    public int UniquePluginCount { get; set; }
    public int UniqueCveCount { get; set; }

    // Chart data arrays
    public int[] SeverityData => new[] { Critical, High, Medium, Low, Info };
    public int[] StatusData { get; set; } = new int[5]; // Open, InProgress, Remediated, Accepted, FalsePositive

    // Top vulnerabilities
    public List<TopVulnerabilityViewModel> TopVulnerabilities { get; set; } = new();

    // By family
    public List<PluginFamilySummary> ByFamily { get; set; } = new();
}

/// <summary>
/// STIG compliance metrics
/// </summary>
public class StigMetricsViewModel
{
    public int TotalRules { get; set; }
    public int Open { get; set; }
    public int NotAFinding { get; set; }
    public int NotApplicable { get; set; }
    public int NotReviewed { get; set; }
    public decimal CompliancePercentage { get; set; }

    // CAT breakdown
    public int CatIOpen { get; set; }
    public int CatIIOpen { get; set; }
    public int CatIIIOpen { get; set; }
    public int CatITotal { get; set; }
    public int CatIITotal { get; set; }
    public int CatIIITotal { get; set; }

    // Chart data arrays
    public int[] StatusData => new[] { Open, NotAFinding, NotApplicable, NotReviewed };
    public int[] CatOpenData => new[] { CatIOpen, CatIIOpen, CatIIIOpen };
    public int[] CatTotalData => new[] { CatITotal, CatIITotal, CatIIITotal };

    // By severity
    public List<StigSeveritySummary> BySeverity { get; set; } = new();

    // By benchmark
    public List<BenchmarkComplianceViewModel> BenchmarkCompliance { get; set; } = new();
}

/// <summary>
/// NIST control family compliance summary
/// </summary>
public class NISTFamilyComplianceViewModel
{
    public string Family { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public int TotalControls { get; set; }
    public int ControlsWithFindings { get; set; }
    public int OpenFindings { get; set; }
    public decimal ComplianceRate { get; set; }
    public List<NISTControlComplianceResult> Controls { get; set; } = new();
}

/// <summary>
/// Individual NIST control compliance result
/// </summary>
public class NISTControlComplianceResult
{
    public int NISTControlId { get; set; }
    public string ControlId { get; set; } = string.Empty;
    public string ControlName { get; set; } = string.Empty;
    public List<string> CCIs { get; set; } = new();
    public int OpenFindingCount { get; set; }
    public int TotalFindingCount { get; set; }
    public bool HasExistingAssessment { get; set; }
    public int? ExistingAssessmentId { get; set; }
    public string AssessmentStatus { get; set; } = string.Empty;
}

/// <summary>
/// Top vulnerability item for dashboard table
/// </summary>
public class TopVulnerabilityViewModel
{
    public int PluginId { get; set; }
    public string PluginName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string CVE { get; set; } = string.Empty;
    public int AffectedHostCount { get; set; }
    public bool IsExploitable { get; set; }
}

/// <summary>
/// Plugin family summary
/// </summary>
public class PluginFamilySummary
{
    public string Family { get; set; } = string.Empty;
    public int Count { get; set; }
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
}

/// <summary>
/// STIG severity summary
/// </summary>
public class StigSeveritySummary
{
    public string Severity { get; set; } = string.Empty;
    public string CatLevel { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Open { get; set; }
    public int NotAFinding { get; set; }
}

/// <summary>
/// Benchmark compliance summary
/// </summary>
public class BenchmarkComplianceViewModel
{
    public string BenchmarkName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public int ChecklistCount { get; set; }
    public int TotalRules { get; set; }
    public int OpenCount { get; set; }
    public int NotAFindingCount { get; set; }
    public decimal CompliancePercentage { get; set; }
}

/// <summary>
/// Site-level breakdown for dashboard table
/// </summary>
public class SiteBreakdownViewModel
{
    public string SiteName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int TotalHosts { get; set; }
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int OpenVulnerabilities { get; set; }
    public int StigOpenFindings { get; set; }
    public int StigTotalFindings { get; set; }
    public decimal StigCompliancePercentage { get; set; }
    public string RiskLevel { get; set; } = "Minimal";
}

/// <summary>
/// Host-level breakdown
/// </summary>
public class HostBreakdownViewModel
{
    public int HostId { get; set; }
    public string HostName { get; set; } = string.Empty;
    public string IPAddress { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int OpenVulnerabilities { get; set; }
    public int StigOpenFindings { get; set; }
    public int StigTotalFindings { get; set; }
    public decimal StigCompliancePercentage { get; set; }
    public string RiskLevel { get; set; } = "Minimal";
}

/// <summary>
/// Session list item for index page
/// </summary>
public class VulnImportSessionListItem
{
    public int Id { get; set; }
    public DateTime ImportDate { get; set; }
    public string ImportedBy { get; set; } = string.Empty;
    public string SourceApplication { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int TotalHosts { get; set; }
    public int TotalNessusVulns { get; set; }
    public int NessusCritical { get; set; }
    public int NessusHigh { get; set; }
    public int TotalStigFindings { get; set; }
    public int StigOpen { get; set; }
    public decimal StigCompliancePercentage { get; set; }
    public string RiskLevel { get; set; } = "Minimal";
}

/// <summary>
/// File upload view model
/// </summary>
public class VulnImportUploadViewModel
{
    public IFormFile? File { get; set; }
}
