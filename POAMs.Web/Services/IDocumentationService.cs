using POAMs.Web.Models.Domain;
using POAMs.Web.Models.ViewModels;

namespace POAMs.Web.Services;

public interface IDocumentationService
{
    /// <summary>
    /// Get all documentation requirements for a control
    /// </summary>
    Task<List<DocumentationRequirementViewModel>> GetRequirementsForControlAsync(string controlId);

    /// <summary>
    /// Get documentation instances for a control and system (creates if not exist)
    /// </summary>
    Task<List<DocumentationInstanceViewModel>> GetDocumentationForControlAsync(string controlId, int systemId);

    /// <summary>
    /// Get a single documentation instance
    /// </summary>
    Task<ControlDocumentationInstance?> GetInstanceByIdAsync(int instanceId);

    /// <summary>
    /// Update documentation instance status and details
    /// </summary>
    Task<ControlDocumentationInstance> UpdateInstanceAsync(DocumentationEditViewModel model, int userId);

    /// <summary>
    /// Get documentation summary for a system (counts by status)
    /// </summary>
    Task<DocumentationSummaryViewModel> GetSystemDocumentationSummaryAsync(int systemId);

    /// <summary>
    /// Get documentation summary for a control family
    /// </summary>
    Task<DocumentationFamilySummaryViewModel> GetFamilyDocumentationSummaryAsync(string family, int systemId);

    /// <summary>
    /// Ensure documentation instances exist for all requirements for a system
    /// </summary>
    Task EnsureDocumentationInstancesAsync(int systemId);

    /// <summary>
    /// Get total documentation requirements count
    /// </summary>
    Task<int> GetRequirementCountAsync();
}
