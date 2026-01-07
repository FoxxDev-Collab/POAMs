using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POAMs.Web.Data;
using POAMs.Web.Models.ViewModels;
using POAMs.Web.Services;
using System.Security.Claims;

namespace POAMs.Web.Controllers;

[Authorize(Policy = "CanView")]
public class VulnImportController : Controller
{
    private readonly IVulnImportService _vulnImportService;
    private readonly ICCIMappingService _cciMappingService;
    private readonly IAuditService _auditService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VulnImportController> _logger;

    public VulnImportController(
        IVulnImportService vulnImportService,
        ICCIMappingService cciMappingService,
        IAuditService auditService,
        ApplicationDbContext context,
        ILogger<VulnImportController> logger)
    {
        _vulnImportService = vulnImportService;
        _cciMappingService = cciMappingService;
        _auditService = auditService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// List all import sessions
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var sessions = await _vulnImportService.GetAllSessionsAsync();
        return View(sessions);
    }

    /// <summary>
    /// Show import upload form
    /// </summary>
    [Authorize(Policy = "CanEdit")]
    public IActionResult Import()
    {
        return View(new VulnImportUploadViewModel());
    }

    /// <summary>
    /// Process uploaded JSON file
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> Import(VulnImportUploadViewModel model)
    {
        if (model.File == null || model.File.Length == 0)
        {
            ModelState.AddModelError("File", "Please select a JSON file to upload.");
            return View(model);
        }

        var extension = Path.GetExtension(model.File.FileName).ToLowerInvariant();
        if (extension != ".json")
        {
            ModelState.AddModelError("File", "Only JSON files are accepted.");
            return View(model);
        }

        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";

        using var stream = model.File.OpenReadStream();
        var result = await _vulnImportService.ImportAsync(stream, model.File.FileName, userName);

        // Log the import
        await _auditService.LogAsync(
            GetCurrentUserId(),
            "Import",
            "VulnImport",
            result.SessionId,
            null,
            new
            {
                FileName = model.File.FileName,
                result.HostsProcessed,
                result.StigFindingsProcessed,
                result.NessusVulnsProcessed,
                result.Success
            },
            GetIpAddress());

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Dashboard), new { id = result.SessionId });
        }

        TempData["ErrorMessage"] = result.Message;
        return View(model);
    }

    /// <summary>
    /// Main dashboard view for an import session
    /// </summary>
    public async Task<IActionResult> Dashboard(int id)
    {
        try
        {
            var dashboard = await _vulnImportService.GetDashboardAsync(id);
            dashboard.NISTCompliance = await _cciMappingService.AnalyzeComplianceAsync(id);
            return View(dashboard);
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Get Nessus metrics tab content (partial view)
    /// </summary>
    public async Task<IActionResult> NessusTab(int id)
    {
        var metrics = await _vulnImportService.GetNessusMetricsAsync(id);
        return PartialView("_NessusTab", metrics);
    }

    /// <summary>
    /// Get STIG metrics tab content (partial view)
    /// </summary>
    public async Task<IActionResult> StigTab(int id)
    {
        var metrics = await _vulnImportService.GetStigMetricsAsync(id);
        return PartialView("_StigTab", metrics);
    }

    /// <summary>
    /// Get NIST compliance tab content (partial view)
    /// </summary>
    public async Task<IActionResult> NISTComplianceTab(int id, int? systemId)
    {
        var compliance = await _cciMappingService.AnalyzeComplianceAsync(id);
        ViewBag.SessionId = id;
        ViewBag.SystemId = systemId;
        ViewBag.AvailableSystems = await _context.Systems.OrderBy(s => s.SystemName).ToListAsync();
        return PartialView("_NISTComplianceTab", compliance);
    }

    /// <summary>
    /// Create a single NIST control assessment
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> CreateAssessment(int sessionId, int nistControlId, int systemId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            var assessment = await _cciMappingService.CreateAssessmentFromFindingsAsync(
                sessionId, nistControlId, systemId, userId.Value);

            await _auditService.LogAsync(
                userId,
                "Create",
                "NISTControlAssessment",
                assessment.Id,
                null,
                new { sessionId, nistControlId, systemId },
                GetIpAddress());

            return Json(new { success = true, assessmentId = assessment.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create assessment for control {ControlId}", nistControlId);
            return Json(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Create all NIST control assessments for controls with findings
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> CreateAllAssessments(int sessionId, int systemId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            var count = await _cciMappingService.CreateAllAssessmentsAsync(sessionId, systemId, userId.Value);

            await _auditService.LogAsync(
                userId,
                "BulkCreate",
                "NISTControlAssessment",
                0,
                null,
                new { sessionId, systemId, assessmentsCreated = count },
                GetIpAddress());

            TempData["SuccessMessage"] = $"Created {count} NIST control assessments.";
            return Json(new { success = true, count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create bulk assessments");
            return Json(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Delete an import session
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> Delete(int id)
    {
        var session = await _vulnImportService.GetSessionAsync(id);
        if (session == null)
        {
            return NotFound();
        }

        await _vulnImportService.DeleteSessionAsync(id);

        await _auditService.LogAsync(
            GetCurrentUserId(),
            "Delete",
            "VulnImportSession",
            id,
            new { session.FileName, session.ImportDate },
            null,
            GetIpAddress());

        TempData["SuccessMessage"] = "Import session deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Get site breakdown data (AJAX)
    /// </summary>
    public async Task<IActionResult> GetSiteBreakdown(int id)
    {
        var breakdown = await _vulnImportService.GetSiteBreakdownAsync(id);
        return Json(breakdown);
    }

    /// <summary>
    /// Get host breakdown for a site (AJAX)
    /// </summary>
    public async Task<IActionResult> GetHostBreakdown(int id, string siteName)
    {
        var breakdown = await _vulnImportService.GetHostBreakdownAsync(id, siteName);
        return Json(breakdown);
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue("UserId");
        return int.TryParse(userIdClaim, out var id) ? id : null;
    }

    private string? GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
