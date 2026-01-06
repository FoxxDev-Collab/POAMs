using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using POAMs.Web.Data;
using POAMs.Web.Services;

namespace POAMs.Web.Controllers;

[Authorize(Policy = "CanView")]
public class ExportController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IExportService _exportService;
    private readonly IImportService _importService;
    private readonly IAuditService _auditService;
    private readonly IUserService _userService;

    public ExportController(
        ApplicationDbContext context,
        IExportService exportService,
        IImportService importService,
        IAuditService auditService,
        IUserService userService)
    {
        _context = context;
        _exportService = exportService;
        _importService = importService;
        _auditService = auditService;
        _userService = userService;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Systems = new SelectList(
            await _context.Systems.OrderBy(s => s.SystemName).ToListAsync(),
            "Id", "SystemName");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportSystem(int systemId)
    {
        var system = await _context.Systems
            .Include(s => s.ISSM)
            .FirstOrDefaultAsync(s => s.Id == systemId);

        if (system == null)
        {
            TempData["Error"] = "System not found.";
            return RedirectToAction(nameof(Index));
        }

        var poams = await _context.POAMs
            .Include(p => p.System)
            .Include(p => p.POC)
            .Include(p => p.Milestones)
            .Where(p => p.SystemId == systemId)
            .OrderBy(p => p.ItemIdentifier)
            .ToListAsync();

        if (!poams.Any())
        {
            TempData["Warning"] = "No POAMs found for the selected system.";
            return RedirectToAction(nameof(Index));
        }

        var excelBytes = _exportService.ExportToXacta(poams, system);
        var fileName = $"POAM_{system.SystemName.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd}.xlsx";

        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    public async Task<IActionResult> ExportSingle(int id)
    {
        var poam = await _context.POAMs
            .Include(p => p.System)
                .ThenInclude(s => s.ISSM)
            .Include(p => p.POC)
            .Include(p => p.Milestones)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (poam == null)
        {
            TempData["Error"] = "POAM not found.";
            return RedirectToAction("Index", "POAM");
        }

        var excelBytes = _exportService.ExportSingleToXacta(poam);
        var fileName = $"POAM_{poam.ItemIdentifier}_{DateTime.UtcNow:yyyyMMdd}.xlsx";

        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportAll()
    {
        var systems = await _context.Systems
            .Include(s => s.ISSM)
            .ToListAsync();

        var poams = await _context.POAMs
            .Include(p => p.System)
            .Include(p => p.POC)
            .Include(p => p.Milestones)
            .OrderBy(p => p.SystemId)
            .ThenBy(p => p.ItemIdentifier)
            .ToListAsync();

        if (!poams.Any())
        {
            TempData["Warning"] = "No POAMs found to export.";
            return RedirectToAction(nameof(Index));
        }

        // Use first system for header or create a combined header
        var firstSystem = systems.FirstOrDefault() ?? new Models.Domain.SystemInfo
        {
            SystemName = "All Systems",
            OrganizationName = "Combined Export"
        };

        var excelBytes = _exportService.ExportToXacta(poams, firstSystem);
        var fileName = $"POAM_All_Systems_{DateTime.UtcNow:yyyyMMdd}.xlsx";

        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    // Import functionality
    [Authorize(Policy = "CanEdit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile file, int systemId, bool updateExisting = false)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please select a file to import.";
            return RedirectToAction(nameof(Index));
        }

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) &&
            !file.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Please upload an Excel file (.xlsx or .xls).";
            return RedirectToAction(nameof(Index));
        }

        var system = await _context.Systems.FindAsync(systemId);
        if (system == null)
        {
            TempData["Error"] = "Please select a valid system.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _importService.ImportFromExcelAsync(stream, systemId, updateExisting);

            // Log the import
            var currentUser = await GetCurrentUserAsync();
            await _auditService.LogAsync(
                currentUser?.Id,
                "Import",
                "POAM",
                null,
                null,
                new { FileName = file.FileName, SystemId = systemId, result.ImportedCount, result.UpdatedCount, result.SkippedCount },
                GetIpAddress());

            if (result.Errors.Any())
            {
                TempData["Error"] = $"Import completed with errors. Imported: {result.ImportedCount}, Updated: {result.UpdatedCount}, Skipped: {result.SkippedCount}. Errors: {string.Join("; ", result.Errors.Take(5))}";
            }
            else if (result.Warnings.Any())
            {
                TempData["Warning"] = $"Import completed. Imported: {result.ImportedCount}, Updated: {result.UpdatedCount}, Skipped: {result.SkippedCount}. Some items were skipped (already exist).";
            }
            else
            {
                TempData["Success"] = $"Import successful! Imported: {result.ImportedCount}, Updated: {result.UpdatedCount}.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Import failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "CanEdit")]
    [HttpPost]
    public async Task<IActionResult> PreviewImport(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return Json(new { success = false, error = "No file provided." });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var preview = await _importService.PreviewExcelAsync(stream);
            return Json(new { success = true, data = preview, count = preview.Count });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpGet]
    public IActionResult DownloadTemplate()
    {
        // Create a template Excel file
        var emptyPoams = new List<Models.Domain.POAM>();
        var templateSystem = new Models.Domain.SystemInfo
        {
            SystemName = "[System Name]",
            OrganizationName = "[Organization Name]",
            ISType = "[IS Type]",
            UID = "[UID]"
        };

        var excelBytes = _exportService.ExportToXacta(emptyPoams, templateSystem);
        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "POAM_Import_Template.xlsx");
    }

    [HttpGet]
    public IActionResult DownloadTestData()
    {
        // Create test data with 15 POAMs, each with 2 milestones
        var random = new Random(42);
        var testPoams = new List<Models.Domain.POAM>();

        var poamData = new[]
        {
            ("26-O-0001", "Windows Server 2019 - Missing Critical Patches", "SI-2", "STIG Scan", Models.Domain.RiskLevel.High, Models.Domain.POAMStatus.Ongoing),
            ("26-O-0002", "Firewall Configuration Non-Compliant", "SC-7", "Security Audit", Models.Domain.RiskLevel.High, Models.Domain.POAMStatus.Open),
            ("26-O-0003", "User Account Management Deficiency", "AC-2", "Internal Review", Models.Domain.RiskLevel.Moderate, Models.Domain.POAMStatus.Open),
            ("26-O-0004", "Audit Log Retention Below Requirements", "AU-11", "Compliance Check", Models.Domain.RiskLevel.Moderate, Models.Domain.POAMStatus.Ongoing),
            ("26-O-0005", "Password Complexity Not Enforced", "IA-5", "STIG Scan", Models.Domain.RiskLevel.High, Models.Domain.POAMStatus.Open),
            ("26-O-0006", "Antivirus Definitions Outdated", "SI-3", "Automated Scan", Models.Domain.RiskLevel.High, Models.Domain.POAMStatus.Completed),
            ("26-O-0007", "Session Timeout Not Configured", "AC-12", "Penetration Test", Models.Domain.RiskLevel.Low, Models.Domain.POAMStatus.Open),
            ("26-O-0008", "SSH Keys Not Rotated", "IA-5", "Security Assessment", Models.Domain.RiskLevel.Moderate, Models.Domain.POAMStatus.Ongoing),
            ("26-O-0009", "Network Segmentation Insufficient", "SC-7", "Architecture Review", Models.Domain.RiskLevel.High, Models.Domain.POAMStatus.Open),
            ("26-O-0010", "Database Encryption Not Enabled", "SC-28", "Data Assessment", Models.Domain.RiskLevel.High, Models.Domain.POAMStatus.Open),
            ("26-O-0011", "Backup Verification Not Performed", "CP-9", "BCP Review", Models.Domain.RiskLevel.Moderate, Models.Domain.POAMStatus.Ongoing),
            ("26-O-0012", "MFA Not Implemented for Admin Accounts", "IA-2", "Security Audit", Models.Domain.RiskLevel.High, Models.Domain.POAMStatus.Open),
            ("26-O-0013", "Vulnerability Scan Coverage Incomplete", "RA-5", "Assessment Review", Models.Domain.RiskLevel.Moderate, Models.Domain.POAMStatus.Open),
            ("26-O-0014", "Incident Response Plan Outdated", "IR-8", "Annual Review", Models.Domain.RiskLevel.Low, Models.Domain.POAMStatus.Draft),
            ("26-O-0015", "Certificate Expiration Monitoring Missing", "SC-17", "Infrastructure Audit", Models.Domain.RiskLevel.Moderate, Models.Domain.POAMStatus.Open)
        };

        foreach (var (itemId, weakness, control, source, risk, status) in poamData)
        {
            var completionDate = DateTime.UtcNow.AddDays(random.Next(30, 180));
            var poam = new Models.Domain.POAM
            {
                ItemIdentifier = itemId,
                WeaknessOrDeficiency = weakness,
                SecurityControl = control,
                ResourcesRequired = "System Administrators, Security Team",
                ScheduledCompletionDate = completionDate,
                IdentifiedBy = source,
                RiskLevel = risk,
                Status = status,
                EstimatedCost = random.Next(500, 5000),
                Comments = $"Test POAM for {control} control",
                OriginalPOAMDate = DateTime.UtcNow,
                LastUpdateDate = DateTime.UtcNow,
                Milestones = new List<Models.Domain.Milestone>
                {
                    new Models.Domain.Milestone
                    {
                        MilestoneNumber = 1,
                        Title = "Develop remediation plan",
                        DueDate = DateTime.UtcNow.AddDays(random.Next(15, 60)),
                        Status = Models.Domain.MilestoneStatus.NotStarted
                    },
                    new Models.Domain.Milestone
                    {
                        MilestoneNumber = 2,
                        Title = "Implement and verify fix",
                        DueDate = completionDate.AddDays(-14),
                        Status = Models.Domain.MilestoneStatus.NotStarted
                    }
                }
            };
            testPoams.Add(poam);
        }

        var testSystem = new Models.Domain.SystemInfo
        {
            SystemName = "Test System Alpha",
            OrganizationName = "Test Organization",
            ISType = "Major Application",
            UID = "TEST-001"
        };

        var excelBytes = _exportService.ExportToXacta(testPoams, testSystem);
        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "TestPOAMs_15_Items.xlsx");
    }

    private async Task<Models.Domain.User?> GetCurrentUserAsync()
    {
        if (User.Identity?.Name == null) return null;
        return await _userService.GetByUsernameAsync(User.Identity.Name);
    }

    private string? GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
