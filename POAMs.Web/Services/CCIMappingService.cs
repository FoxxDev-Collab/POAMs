using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using POAMs.Web.Data;
using POAMs.Web.Models.Domain;
using POAMs.Web.Models.ViewModels;

namespace POAMs.Web.Services;

public class CCIMappingService : ICCIMappingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CCIMappingService> _logger;

    // NIST control family names
    private static readonly Dictionary<string, string> FamilyNames = new()
    {
        { "AC", "Access Control" },
        { "AT", "Awareness and Training" },
        { "AU", "Audit and Accountability" },
        { "CA", "Assessment, Authorization, and Monitoring" },
        { "CM", "Configuration Management" },
        { "CP", "Contingency Planning" },
        { "IA", "Identification and Authentication" },
        { "IR", "Incident Response" },
        { "MA", "Maintenance" },
        { "MP", "Media Protection" },
        { "PE", "Physical and Environmental Protection" },
        { "PL", "Planning" },
        { "PM", "Program Management" },
        { "PS", "Personnel Security" },
        { "PT", "PII Processing and Transparency" },
        { "RA", "Risk Assessment" },
        { "SA", "System and Services Acquisition" },
        { "SC", "System and Communications Protection" },
        { "SI", "System and Information Integrity" },
        { "SR", "Supply Chain Risk Management" }
    };

    public CCIMappingService(ApplicationDbContext context, ILogger<CCIMappingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<NISTFamilyComplianceViewModel>> AnalyzeComplianceAsync(int sessionId)
    {
        var controlSummaries = await GetControlSummariesAsync(sessionId);

        // Group by family
        var familyGroups = controlSummaries
            .GroupBy(c => c.ControlId.Split('-')[0])
            .Select(g =>
            {
                var familyCode = g.Key;
                var controls = g.ToList();
                var controlsWithFindings = controls.Count(c => c.OpenFindingCount > 0);

                return new NISTFamilyComplianceViewModel
                {
                    Family = familyCode,
                    FamilyName = FamilyNames.GetValueOrDefault(familyCode, familyCode),
                    TotalControls = controls.Count,
                    ControlsWithFindings = controlsWithFindings,
                    OpenFindings = controls.Sum(c => c.OpenFindingCount),
                    ComplianceRate = controls.Count > 0
                        ? Math.Round(100m - ((decimal)controlsWithFindings / controls.Count * 100), 1)
                        : 100,
                    Controls = controls.OrderBy(c => c.ControlId).ToList()
                };
            })
            .Where(f => f.ControlsWithFindings > 0 || f.TotalControls > 0)
            .OrderByDescending(f => f.OpenFindings)
            .ThenBy(f => f.Family)
            .ToList();

        return familyGroups;
    }

    public async Task<Dictionary<string, List<VulnImportStigResult>>> GetFindingsByCCIAsync(int sessionId)
    {
        var stigResults = await _context.VulnImportStigResults
            .Where(r => r.Host.VulnImportSessionId == sessionId)
            .ToListAsync();

        var findingsByCCI = new Dictionary<string, List<VulnImportStigResult>>();

        foreach (var result in stigResults)
        {
            try
            {
                var ccis = JsonSerializer.Deserialize<List<string>>(result.CCIs) ?? new List<string>();
                foreach (var cci in ccis)
                {
                    if (!findingsByCCI.ContainsKey(cci))
                    {
                        findingsByCCI[cci] = new List<VulnImportStigResult>();
                    }
                    findingsByCCI[cci].Add(result);
                }
            }
            catch (JsonException)
            {
                // Skip malformed CCI data
            }
        }

        return findingsByCCI;
    }

    public async Task<List<NISTControlComplianceResult>> GetControlSummariesAsync(int sessionId, int? systemId = null)
    {
        // Get all STIG findings for this session
        var stigResults = await _context.VulnImportStigResults
            .Where(r => r.Host.VulnImportSessionId == sessionId)
            .ToListAsync();

        // Extract all unique CCIs from findings
        var allCCIs = new HashSet<string>();
        var openCCIs = new HashSet<string>();

        foreach (var result in stigResults)
        {
            try
            {
                var ccis = JsonSerializer.Deserialize<List<string>>(result.CCIs) ?? new List<string>();
                foreach (var cci in ccis)
                {
                    allCCIs.Add(cci);
                    if (result.Status == "Open")
                    {
                        openCCIs.Add(cci);
                    }
                }
            }
            catch (JsonException)
            {
                // Skip malformed CCI data
            }
        }

        // Get CCI to NIST control mappings
        var cciMappings = await _context.CCIs
            .Include(c => c.NISTControl)
            .Where(c => allCCIs.Contains(c.CCINumber))
            .ToListAsync();

        // Get existing assessments for the system (if specified)
        Dictionary<int, NISTControlAssessment> existingAssessments = new();
        if (systemId.HasValue)
        {
            existingAssessments = await _context.NISTControlAssessments
                .Where(a => a.SystemId == systemId.Value)
                .GroupBy(a => a.NISTControlId)
                .Select(g => g.OrderByDescending(a => a.AssessedDate).First())
                .ToDictionaryAsync(a => a.NISTControlId);
        }

        // Build control summaries
        var controlSummaries = cciMappings
            .GroupBy(c => c.NISTControl)
            .Select(g =>
            {
                var control = g.Key;
                var controlCCIs = g.Select(c => c.CCINumber).ToList();
                var openCount = controlCCIs.Count(cci => openCCIs.Contains(cci));
                var totalCount = controlCCIs.Count(cci => allCCIs.Contains(cci));

                existingAssessments.TryGetValue(control.Id, out var existingAssessment);

                return new NISTControlComplianceResult
                {
                    NISTControlId = control.Id,
                    ControlId = control.ControlId,
                    ControlName = control.Name,
                    CCIs = controlCCIs,
                    OpenFindingCount = openCount,
                    TotalFindingCount = totalCount,
                    HasExistingAssessment = existingAssessment != null,
                    ExistingAssessmentId = existingAssessment?.Id,
                    AssessmentStatus = existingAssessment?.Status.ToString() ?? ""
                };
            })
            .OrderByDescending(c => c.OpenFindingCount)
            .ThenBy(c => c.ControlId)
            .ToList();

        return controlSummaries;
    }

    public async Task<NISTControlAssessment> CreateAssessmentFromFindingsAsync(
        int sessionId, int nistControlId, int systemId, int assessorId)
    {
        // Get the NIST control
        var control = await _context.NISTControls
            .Include(c => c.CCIs)
            .FirstOrDefaultAsync(c => c.Id == nistControlId);

        if (control == null)
        {
            throw new ArgumentException($"NIST Control {nistControlId} not found");
        }

        // Get CCIs for this control
        var controlCCIs = control.CCIs.Select(c => c.CCINumber).ToHashSet();

        // Get STIG findings for this session that relate to this control's CCIs
        var stigResults = await _context.VulnImportStigResults
            .Where(r => r.Host.VulnImportSessionId == sessionId)
            .ToListAsync();

        var relatedFindings = new List<VulnImportStigResult>();
        var openFindings = new List<VulnImportStigResult>();

        foreach (var result in stigResults)
        {
            try
            {
                var ccis = JsonSerializer.Deserialize<List<string>>(result.CCIs) ?? new List<string>();
                if (ccis.Any(cci => controlCCIs.Contains(cci)))
                {
                    relatedFindings.Add(result);
                    if (result.Status == "Open")
                    {
                        openFindings.Add(result);
                    }
                }
            }
            catch (JsonException) { }
        }

        // Determine assessment status
        var status = openFindings.Count > 0
            ? ControlAssessmentStatus.NonCompliant
            : ControlAssessmentStatus.Compliant;

        // Build implementation notes from findings
        var notes = new System.Text.StringBuilder();
        notes.AppendLine($"Assessment generated from Vulnerability Import Session {sessionId}");
        notes.AppendLine($"Related CCIs: {string.Join(", ", controlCCIs)}");
        notes.AppendLine($"Total related findings: {relatedFindings.Count}");
        notes.AppendLine($"Open findings: {openFindings.Count}");

        if (openFindings.Any())
        {
            notes.AppendLine("\nOpen Findings:");
            foreach (var finding in openFindings.Take(10))
            {
                notes.AppendLine($"  - {finding.VulnId}: {finding.RuleTitle} ({finding.CatLevel})");
            }
            if (openFindings.Count > 10)
            {
                notes.AppendLine($"  ... and {openFindings.Count - 10} more");
            }
        }

        // Create the assessment
        var assessment = new NISTControlAssessment
        {
            NISTControlId = nistControlId,
            SystemId = systemId,
            Status = status,
            RiskLevel = openFindings.Any(f => f.Severity?.ToLower() == "high") ? RiskLevel.High
                : openFindings.Any() ? RiskLevel.Moderate
                : RiskLevel.Low,
            AssessedById = assessorId,
            AssessedDate = DateTime.UtcNow,
            Notes = notes.ToString(),
            Implementation = openFindings.Any()
                ? "STIG findings indicate non-compliance. Review linked findings and implement required remediations."
                : "All related STIG findings are compliant (Not a Finding or Not Applicable).",
            Evidence = $"Vulnerability Import Session {sessionId} - {DateTime.UtcNow:yyyy-MM-dd}"
        };

        _context.NISTControlAssessments.Add(assessment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created NIST assessment for control {ControlId} from session {SessionId}",
            control.ControlId, sessionId);

        return assessment;
    }

    public async Task<int> CreateAllAssessmentsAsync(int sessionId, int systemId, int assessorId)
    {
        var controlSummaries = await GetControlSummariesAsync(sessionId, systemId);

        // Only create assessments for controls with findings that don't already have an assessment
        var controlsToAssess = controlSummaries
            .Where(c => c.OpenFindingCount > 0 && !c.HasExistingAssessment)
            .ToList();

        var createdCount = 0;

        foreach (var control in controlsToAssess)
        {
            try
            {
                await CreateAssessmentFromFindingsAsync(sessionId, control.NISTControlId, systemId, assessorId);
                createdCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create assessment for control {ControlId}", control.ControlId);
            }
        }

        _logger.LogInformation("Created {Count} NIST assessments from session {SessionId}", createdCount, sessionId);

        return createdCount;
    }
}
