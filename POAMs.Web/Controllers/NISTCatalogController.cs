using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using POAMs.Web.Data;
using POAMs.Web.Models.Domain;
using POAMs.Web.Models.ViewModels;
using POAMs.Web.Services;

namespace POAMs.Web.Controllers;

[Authorize(Policy = "CanView")]
public class NISTCatalogController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly INISTCatalogService _nistService;
    private readonly IDocumentationService _docService;
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;

    public NISTCatalogController(
        ApplicationDbContext context,
        INISTCatalogService nistService,
        IDocumentationService docService,
        IUserService userService,
        IAuditService auditService)
    {
        _context = context;
        _nistService = nistService;
        _docService = docService;
        _userService = userService;
        _auditService = auditService;
    }

    // GET: NISTCatalog
    public async Task<IActionResult> Index(int? systemId)
    {
        var families = await _nistService.GetFamilySummariesAsync(systemId);

        var viewModel = new NISTCatalogIndexViewModel
        {
            SelectedSystemId = systemId,
            Families = families,
            TotalControls = await _nistService.GetControlCountAsync(),
            TotalCCIs = await _nistService.GetCCICountAsync()
        };

        if (systemId.HasValue)
        {
            var system = await _context.Systems.FindAsync(systemId.Value);
            viewModel.SelectedSystemName = system?.SystemName;

            viewModel.TotalCompliant = families.Sum(f => f.Compliant);
            viewModel.TotalNonCompliant = families.Sum(f => f.NonCompliant);
            viewModel.TotalNotAssessed = families.Sum(f => f.NotAssessed);

            var totalAssessed = viewModel.TotalCompliant + viewModel.TotalNonCompliant +
                families.Sum(f => f.NotApplicable) + families.Sum(f => f.Inherited);
            var totalCompliantEquivalent = viewModel.TotalCompliant +
                families.Sum(f => f.NotApplicable) + families.Sum(f => f.Inherited);

            // Compliance rate based on total controls (unassessed counts against you)
            viewModel.OverallComplianceRate = viewModel.TotalControls > 0
                ? Math.Round((decimal)totalCompliantEquivalent / viewModel.TotalControls * 100, 1)
                : 0;
            viewModel.OverallProgressRate = viewModel.TotalControls > 0
                ? Math.Round((decimal)totalAssessed / viewModel.TotalControls * 100, 1)
                : 0;
        }
        else
        {
            viewModel.TotalNotAssessed = viewModel.TotalControls;
        }

        await PopulateSystemsDropdownAsync(systemId);
        return View(viewModel);
    }

    // GET: NISTCatalog/Family/AC
    public async Task<IActionResult> Family(string id, int? systemId, string? search, ControlAssessmentStatus? status)
    {
        if (string.IsNullOrEmpty(id))
            return NotFound();

        var family = id.ToUpper();
        var controls = await _nistService.GetControlsByFamilyAsync(family, systemId, search, status);

        if (!controls.Any() && string.IsNullOrEmpty(search) && !status.HasValue)
            return NotFound();

        var summaries = await _nistService.GetFamilySummariesAsync(systemId);
        var familySummary = summaries.FirstOrDefault(s => s.Family == family) ?? new FamilySummaryViewModel
        {
            Family = family,
            FamilyName = NISTFamilyNames.GetName(family)
        };

        var viewModel = new NISTFamilyViewModel
        {
            Family = family,
            FamilyName = NISTFamilyNames.GetName(family),
            SelectedSystemId = systemId,
            SearchTerm = search,
            StatusFilter = status,
            Controls = controls,
            Summary = familySummary
        };

        if (systemId.HasValue)
        {
            var system = await _context.Systems.FindAsync(systemId.Value);
            viewModel.SelectedSystemName = system?.SystemName;
        }

        await PopulateSystemsDropdownAsync(systemId);
        ViewBag.StatusList = GetStatusSelectList(status);
        return View(viewModel);
    }

    // GET: NISTCatalog/Details/AC-1
    public async Task<IActionResult> Details(string id, int? systemId)
    {
        if (string.IsNullOrEmpty(id))
            return NotFound();

        var control = await _nistService.GetControlWithCCIsAsync(id);
        if (control == null)
            return NotFound();

        var assessments = await _nistService.GetAssessmentsForControlAsync(id);

        // Group assessments by system, with latest first
        var assessmentsBySystem = assessments
            .GroupBy(a => a.SystemId)
            .Select(g =>
            {
                var ordered = g.OrderByDescending(a => a.AssessedDate ?? a.CreatedDate).ToList();
                return new SystemAssessmentHistoryViewModel
                {
                    SystemId = g.Key,
                    SystemName = ordered.First().System?.SystemName ?? "Unknown",
                    Assessments = ordered.Select((a, index) => new AssessmentSummaryViewModel
                    {
                        Id = a.Id,
                        SystemId = a.SystemId,
                        SystemName = a.System?.SystemName ?? "Unknown",
                        Status = a.Status,
                        RiskLevel = a.RiskLevel,
                        AssessedByName = a.AssessedBy?.DisplayName,
                        AssessedDate = a.AssessedDate,
                        POAMId = a.POAMId,
                        POAMIdentifier = a.POAM?.ItemIdentifier,
                        IsLatest = index == 0
                    }).ToList()
                };
            })
            .OrderBy(s => s.SystemName)
            .ToList();

        var viewModel = new NISTControlDetailViewModel
        {
            Id = control.Id,
            ControlId = control.ControlId,
            Name = control.Name,
            Family = control.Family,
            FamilyName = NISTFamilyNames.GetName(control.Family),
            ControlText = control.ControlText,
            Discussion = control.Discussion,
            IsWithdrawn = control.IsWithdrawn,
            ParentControlId = control.ParentControlId,
            CCIs = control.CCIs.Select(c => new CCIViewModel
            {
                Id = c.Id,
                CCINumber = c.CCINumber,
                Definition = c.Definition
            }).OrderBy(c => c.CCINumber).ToList(),
            AssessmentsBySystem = assessmentsBySystem
        };

        // Parse related controls from JSON
        if (!string.IsNullOrEmpty(control.RelatedControls))
        {
            try
            {
                viewModel.RelatedControls = JsonSerializer.Deserialize<List<string>>(control.RelatedControls) ?? new List<string>();
            }
            catch
            {
                viewModel.RelatedControls = new List<string>();
            }
        }

        // Load documentation if a system is selected
        if (systemId.HasValue)
        {
            viewModel.Documentation = await _docService.GetDocumentationForControlAsync(id, systemId.Value);
        }

        ViewBag.SelectedSystemId = systemId;
        await PopulateSystemsDropdownAsync(systemId);
        return View(viewModel);
    }

    // GET: NISTCatalog/Assess?controlId=AC-1&systemId=1
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> Assess(string controlId, int systemId)
    {
        if (string.IsNullOrEmpty(controlId))
            return NotFound();

        var control = await _nistService.GetControlWithCCIsAsync(controlId);
        if (control == null)
            return NotFound();

        var system = await _context.Systems.FindAsync(systemId);
        if (system == null)
            return NotFound();

        var existingAssessment = await _nistService.GetLatestAssessmentAsync(controlId, systemId);

        // Create new assessment - ID is null so it will create, not update
        // Pre-populate implementation/evidence from previous assessment for convenience
        var viewModel = new NISTAssessmentEditViewModel
        {
            // Id is intentionally null - this creates a NEW assessment
            ControlId = controlId,
            ControlName = control.Name,
            SystemId = systemId,
            SystemName = system.SystemName,
            // Pre-fill from previous assessment for convenience (user can modify)
            Implementation = existingAssessment?.Implementation,
            Evidence = existingAssessment?.Evidence,
            POAMId = existingAssessment?.POAMId
        };

        await PopulatePOAMsDropdownAsync(systemId, viewModel.POAMId);
        return View(viewModel);
    }

    // POST: NISTCatalog/Assess
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> Assess(NISTAssessmentEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var control = await _nistService.GetControlWithCCIsAsync(model.ControlId);
            var system = await _context.Systems.FindAsync(model.SystemId);
            model.ControlName = control?.Name ?? model.ControlId;
            model.SystemName = system?.SystemName ?? "Unknown";
            await PopulatePOAMsDropdownAsync(model.SystemId, model.POAMId);
            return View(model);
        }

        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized();

        // Always create new assessment for history tracking
        var assessment = await _nistService.CreateAssessmentAsync(model, currentUser.Id);

        await _auditService.LogAsync(
            currentUser.Id,
            "Create",
            "NISTControlAssessment",
            assessment.Id,
            null,
            new { model.Status, model.RiskLevel, model.Implementation },
            GetIpAddress());

        TempData["Success"] = $"Assessment for {model.ControlId} saved successfully.";
        return RedirectToAction(nameof(Details), new { id = model.ControlId, systemId = model.SystemId });
    }

    // POST: NISTCatalog/CreatePOAM
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> CreatePOAM(string controlId, int systemId)
    {
        if (string.IsNullOrEmpty(controlId))
            return NotFound();

        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized();

        try
        {
            var poam = await _nistService.CreatePOAMFromControlAsync(controlId, systemId, currentUser.Id);

            await _auditService.LogAsync(
                currentUser.Id,
                "Create",
                "POAM",
                poam.Id,
                null,
                new { poam.ItemIdentifier, poam.SecurityControl, Source = "NISTControlAssessment" },
                GetIpAddress());

            TempData["Success"] = $"POAM {poam.ItemIdentifier} created and linked to {controlId}.";
            return RedirectToAction(nameof(Details), new { id = controlId, systemId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to create POAM: {ex.Message}";
            return RedirectToAction(nameof(Details), new { id = controlId, systemId });
        }
    }

    // GET: NISTCatalog/EditAssessment/5
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> EditAssessment(int id)
    {
        var assessment = await _nistService.GetAssessmentByIdAsync(id);
        if (assessment == null)
            return NotFound();

        var viewModel = new NISTAssessmentEditViewModel
        {
            Id = assessment.Id,
            ControlId = assessment.NISTControl.ControlId,
            ControlName = assessment.NISTControl.Name,
            SystemId = assessment.SystemId,
            SystemName = assessment.System.SystemName,
            Status = assessment.Status,
            RiskLevel = assessment.RiskLevel,
            Implementation = assessment.Implementation,
            Evidence = assessment.Evidence,
            Notes = assessment.Notes,
            POAMId = assessment.POAMId
        };

        await PopulatePOAMsDropdownAsync(assessment.SystemId, assessment.POAMId);
        return View("Assess", viewModel);
    }

    // POST: NISTCatalog/EditAssessment
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> EditAssessment(NISTAssessmentEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var control = await _nistService.GetControlWithCCIsAsync(model.ControlId);
            var system = await _context.Systems.FindAsync(model.SystemId);
            model.ControlName = control?.Name ?? model.ControlId;
            model.SystemName = system?.SystemName ?? "Unknown";
            await PopulatePOAMsDropdownAsync(model.SystemId, model.POAMId);
            return View("Assess", model);
        }

        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized();

        var oldAssessment = await _context.NISTControlAssessments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == model.Id);

        var assessment = await _nistService.UpdateAssessmentAsync(model, currentUser.Id);

        await _auditService.LogAsync(
            currentUser.Id,
            "Update",
            "NISTControlAssessment",
            assessment.Id,
            oldAssessment != null ? new { oldAssessment.Status, oldAssessment.RiskLevel, oldAssessment.Implementation } : null,
            new { model.Status, model.RiskLevel, model.Implementation },
            GetIpAddress());

        TempData["Success"] = $"Assessment updated successfully.";
        return RedirectToAction(nameof(Details), new { id = model.ControlId, systemId = model.SystemId });
    }

    // POST: NISTCatalog/DeleteAssessment/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> DeleteAssessment(int id, string controlId, int systemId)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized();

        var assessment = await _nistService.GetAssessmentByIdAsync(id);
        if (assessment == null)
        {
            TempData["Error"] = "Assessment not found.";
            return RedirectToAction(nameof(Details), new { id = controlId, systemId });
        }

        var deleted = await _nistService.DeleteAssessmentAsync(id);
        if (deleted)
        {
            await _auditService.LogAsync(
                currentUser.Id,
                "Delete",
                "NISTControlAssessment",
                id,
                new { assessment.Status, assessment.RiskLevel, assessment.Implementation },
                null,
                GetIpAddress());

            TempData["Success"] = "Assessment deleted successfully.";
        }
        else
        {
            TempData["Error"] = "Failed to delete assessment.";
        }

        return RedirectToAction(nameof(Details), new { id = controlId, systemId });
    }

    private async Task PopulateSystemsDropdownAsync(int? selectedId)
    {
        var systems = await _context.Systems
            .OrderBy(s => s.SystemName)
            .Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.SystemName,
                Selected = s.Id == selectedId
            })
            .ToListAsync();

        ViewBag.Systems = systems;
    }

    private async Task PopulatePOAMsDropdownAsync(int systemId, int? selectedId)
    {
        var poams = await _context.POAMs
            .Where(p => p.SystemId == systemId && p.Status != POAMStatus.Closed)
            .OrderBy(p => p.ItemIdentifier)
            .Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = $"{p.ItemIdentifier} - {p.SecurityControl}",
                Selected = p.Id == selectedId
            })
            .ToListAsync();

        poams.Insert(0, new SelectListItem { Value = "", Text = "-- None --" });
        ViewBag.POAMs = poams;
    }

    // GET: NISTCatalog/EditDocumentation/5
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> EditDocumentation(int id, string? returnUrl)
    {
        var instance = await _docService.GetInstanceByIdAsync(id);
        if (instance == null)
            return NotFound();

        var viewModel = new DocumentationEditViewModel
        {
            Id = instance.Id,
            ControlId = instance.Requirement.NISTControl.ControlId,
            SystemId = instance.SystemId,
            SystemName = instance.System.SystemName,
            DocType = instance.Requirement.DocType,
            RequirementDescription = instance.Requirement.Description,
            Priority = instance.Requirement.Priority,
            Status = instance.Status,
            Notes = instance.Notes,
            EvidenceLocation = instance.EvidenceLocation,
            TargetDate = instance.TargetDate,
            ReturnUrl = returnUrl
        };

        await PopulateDocumentationOwnersDropdownAsync(instance.OwnerId);
        return View(viewModel);
    }

    // POST: NISTCatalog/EditDocumentation
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> EditDocumentation(DocumentationEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDocumentationOwnersDropdownAsync(model.OwnerId);
            return View(model);
        }

        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized();

        var oldInstance = await _docService.GetInstanceByIdAsync(model.Id);

        await _docService.UpdateInstanceAsync(model, currentUser.Id);

        await _auditService.LogAsync(
            currentUser.Id,
            "Update",
            "ControlDocumentationInstance",
            model.Id,
            oldInstance != null ? new { oldInstance.Status, oldInstance.EvidenceLocation, oldInstance.Notes } : null,
            new { model.Status, model.EvidenceLocation, model.Notes },
            GetIpAddress());

        TempData["Success"] = "Documentation status updated successfully.";

        if (!string.IsNullOrEmpty(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToAction(nameof(Details), new { id = model.ControlId, systemId = model.SystemId });
    }

    private async Task PopulateDocumentationOwnersDropdownAsync(int? selectedId)
    {
        var users = await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.DisplayName)
            .Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = u.DisplayName,
                Selected = u.Id == selectedId
            })
            .ToListAsync();

        users.Insert(0, new SelectListItem { Value = "", Text = "-- No Owner --" });
        ViewBag.Owners = users;
    }

    private SelectList GetStatusSelectList(ControlAssessmentStatus? selectedStatus)
    {
        var items = Enum.GetValues<ControlAssessmentStatus>()
            .Select(s => new SelectListItem
            {
                Value = ((int)s).ToString(),
                Text = s.ToString(),
                Selected = s == selectedStatus
            })
            .ToList();

        items.Insert(0, new SelectListItem { Value = "", Text = "All Statuses" });
        return new SelectList(items, "Value", "Text", selectedStatus?.ToString());
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        if (User.Identity?.Name == null) return null;
        return await _userService.GetByUsernameAsync(User.Identity.Name);
    }

    private string? GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
