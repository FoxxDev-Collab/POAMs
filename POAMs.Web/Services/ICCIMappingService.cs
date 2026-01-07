using POAMs.Web.Models.Domain;
using POAMs.Web.Models.ViewModels;

namespace POAMs.Web.Services;

public interface ICCIMappingService
{
    /// <summary>
    /// Analyze compliance by mapping CCIs from STIG findings to NIST controls
    /// </summary>
    Task<List<NISTFamilyComplianceViewModel>> AnalyzeComplianceAsync(int sessionId);

    /// <summary>
    /// Get findings grouped by CCI
    /// </summary>
    Task<Dictionary<string, List<VulnImportStigResult>>> GetFindingsByCCIAsync(int sessionId);

    /// <summary>
    /// Get control summaries with finding counts
    /// </summary>
    Task<List<NISTControlComplianceResult>> GetControlSummariesAsync(int sessionId, int? systemId = null);

    /// <summary>
    /// Create a NISTControlAssessment for a specific control based on findings
    /// </summary>
    Task<NISTControlAssessment> CreateAssessmentFromFindingsAsync(int sessionId, int nistControlId, int systemId, int assessorId);

    /// <summary>
    /// Create assessments for all controls that have open findings
    /// </summary>
    Task<int> CreateAllAssessmentsAsync(int sessionId, int systemId, int assessorId);
}
