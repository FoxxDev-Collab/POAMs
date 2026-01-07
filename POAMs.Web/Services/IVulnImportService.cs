using POAMs.Web.Models.Domain;
using POAMs.Web.Models.ViewModels;

namespace POAMs.Web.Services;

public interface IVulnImportService
{
    /// <summary>
    /// Import JSON data from Vulnerability Management export
    /// </summary>
    Task<VulnImportResultViewModel> ImportAsync(Stream fileStream, string fileName, string importedBy);

    /// <summary>
    /// Get the most recent import session
    /// </summary>
    Task<VulnImportSession?> GetLatestSessionAsync();

    /// <summary>
    /// Get a specific import session by ID
    /// </summary>
    Task<VulnImportSession?> GetSessionAsync(int id);

    /// <summary>
    /// Get all import sessions ordered by date
    /// </summary>
    Task<List<VulnImportSessionListItem>> GetAllSessionsAsync();

    /// <summary>
    /// Delete an import session and all related data
    /// </summary>
    Task DeleteSessionAsync(int id);

    /// <summary>
    /// Build the dashboard view model for a session
    /// </summary>
    Task<VulnDashboardViewModel> GetDashboardAsync(int sessionId);

    /// <summary>
    /// Get Nessus metrics for a session
    /// </summary>
    Task<NessusMetricsViewModel> GetNessusMetricsAsync(int sessionId);

    /// <summary>
    /// Get STIG metrics for a session
    /// </summary>
    Task<StigMetricsViewModel> GetStigMetricsAsync(int sessionId);

    /// <summary>
    /// Get site breakdown for a session
    /// </summary>
    Task<List<SiteBreakdownViewModel>> GetSiteBreakdownAsync(int sessionId);

    /// <summary>
    /// Get host breakdown for a site
    /// </summary>
    Task<List<HostBreakdownViewModel>> GetHostBreakdownAsync(int sessionId, string siteName);
}
