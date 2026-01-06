using POAMs.Web.Models.Domain;
using POAMs.Web.Models.ViewModels;

namespace POAMs.Web.Services;

public interface INISTCatalogService
{
    /// <summary>
    /// Get summary statistics for all control families
    /// </summary>
    Task<List<FamilySummaryViewModel>> GetFamilySummariesAsync(int? systemId = null);

    /// <summary>
    /// Get all controls in a specific family with optional assessment status
    /// </summary>
    Task<List<ControlListItemViewModel>> GetControlsByFamilyAsync(string family, int? systemId = null, string? search = null, ControlAssessmentStatus? status = null);

    /// <summary>
    /// Get a single control with its CCIs
    /// </summary>
    Task<NISTControl?> GetControlWithCCIsAsync(string controlId);

    /// <summary>
    /// Get the latest assessment for a control on a specific system
    /// </summary>
    Task<NISTControlAssessment?> GetLatestAssessmentAsync(string controlId, int systemId);

    /// <summary>
    /// Get a specific assessment by ID
    /// </summary>
    Task<NISTControlAssessment?> GetAssessmentByIdAsync(int assessmentId);

    /// <summary>
    /// Get all assessments for a control across all systems
    /// </summary>
    Task<List<NISTControlAssessment>> GetAssessmentsForControlAsync(string controlId);

    /// <summary>
    /// Get assessment history for a control on a specific system (ordered by date descending)
    /// </summary>
    Task<List<NISTControlAssessment>> GetAssessmentHistoryAsync(string controlId, int systemId);

    /// <summary>
    /// Create a new assessment (always creates new, supports history)
    /// </summary>
    Task<NISTControlAssessment> CreateAssessmentAsync(NISTAssessmentEditViewModel model, int assessorId);

    /// <summary>
    /// Update an existing assessment
    /// </summary>
    Task<NISTControlAssessment> UpdateAssessmentAsync(NISTAssessmentEditViewModel model, int assessorId);

    /// <summary>
    /// Delete an assessment
    /// </summary>
    Task<bool> DeleteAssessmentAsync(int assessmentId);

    /// <summary>
    /// Create a POAM from a non-compliant control
    /// </summary>
    Task<POAM> CreatePOAMFromControlAsync(string controlId, int systemId, int userId);

    /// <summary>
    /// Get total control count
    /// </summary>
    Task<int> GetControlCountAsync();

    /// <summary>
    /// Get total CCI count
    /// </summary>
    Task<int> GetCCICountAsync();
}
