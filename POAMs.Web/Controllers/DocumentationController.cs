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
public class DocumentationController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IDocumentationService _docService;
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;

    public DocumentationController(
        ApplicationDbContext context,
        IDocumentationService docService,
        IUserService userService,
        IAuditService auditService)
    {
        _context = context;
        _docService = docService;
        _userService = userService;
        _auditService = auditService;
    }

    // GET: Documentation
    public async Task<IActionResult> Index(int? systemId, string? family, DocumentationType? docType,
        DocumentationStatus? status, DocumentationPriority? priority, string? search)
    {
        // Must select a system to view documentation
        if (!systemId.HasValue)
        {
            await PopulateDropdownsAsync(null, family, docType, status, priority);
            return View(new DocumentationMatrixViewModel());
        }

        var system = await _context.Systems.FindAsync(systemId.Value);
        if (system == null)
            return NotFound();

        // Ensure documentation instances exist for this system
        await _docService.EnsureDocumentationInstancesAsync(systemId.Value);

        // Build query
        var query = _context.ControlDocumentationInstances
            .Include(d => d.Requirement)
                .ThenInclude(r => r.NISTControl)
            .Include(d => d.Owner)
            .Include(d => d.ReviewedBy)
            .Where(d => d.SystemId == systemId.Value);

        // Apply filters
        if (!string.IsNullOrEmpty(family))
        {
            query = query.Where(d => d.Requirement.NISTControl.Family == family);
        }

        if (docType.HasValue)
        {
            query = query.Where(d => d.Requirement.DocType == docType.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(d => d.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(d => d.Requirement.Priority == priority.Value);
        }

        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(d =>
                d.Requirement.NISTControl.ControlId.ToLower().Contains(searchLower) ||
                d.Requirement.NISTControl.Name.ToLower().Contains(searchLower) ||
                (d.Requirement.Description != null && d.Requirement.Description.ToLower().Contains(searchLower)) ||
                (d.Notes != null && d.Notes.ToLower().Contains(searchLower)));
        }

        var instances = await query
            .OrderBy(d => d.Requirement.NISTControl.Family)
            .ThenBy(d => d.Requirement.NISTControl.ControlId)
            .ThenBy(d => d.Requirement.DocType)
            .ToListAsync();

        var viewModel = new DocumentationMatrixViewModel
        {
            SystemId = systemId.Value,
            SystemName = system.SystemName,
            Family = family,
            DocTypeFilter = docType,
            StatusFilter = status,
            PriorityFilter = priority,
            SearchTerm = search,
            Items = instances.Select(d => new DocumentationMatrixItemViewModel
            {
                Id = d.Id,
                RequirementId = d.RequirementId,
                ControlId = d.Requirement.NISTControl.ControlId,
                ControlName = d.Requirement.NISTControl.Name,
                Family = d.Requirement.NISTControl.Family,
                DocType = d.Requirement.DocType,
                Priority = d.Requirement.Priority,
                Description = d.Requirement.Description,
                Status = d.Status,
                OwnerId = d.OwnerId,
                OwnerName = d.Owner?.DisplayName,
                TargetDate = d.TargetDate,
                EvidenceLocation = d.EvidenceLocation,
                Notes = d.Notes,
                LastUpdated = d.ModifiedDate,
                // Review tracking
                ReviewStatus = d.ReviewStatus,
                ReviewedDate = d.ReviewedDate,
                ReviewedByName = d.ReviewedBy?.DisplayName,
                ReviewCycleMonths = d.ReviewCycleMonths,
                NextReviewDate = d.NextReviewDate,
                // Milestone dates
                CompletedDate = d.CompletedDate,
                ApprovedDate = d.ApprovedDate,
                CreatedDate = d.CreatedDate
            }).ToList()
        };

        // Calculate summary stats
        viewModel.TotalCount = viewModel.Items.Count;
        viewModel.NotStartedCount = viewModel.Items.Count(i => i.Status == DocumentationStatus.NotStarted);
        viewModel.InProgressCount = viewModel.Items.Count(i => i.Status == DocumentationStatus.InProgress || i.Status == DocumentationStatus.Draft);
        viewModel.UnderReviewCount = viewModel.Items.Count(i => i.Status == DocumentationStatus.UnderReview);
        viewModel.CompleteCount = viewModel.Items.Count(i => i.Status == DocumentationStatus.Complete || i.Status == DocumentationStatus.Approved);
        viewModel.CriticalNotStarted = viewModel.Items.Count(i => i.Priority == DocumentationPriority.Critical && i.Status == DocumentationStatus.NotStarted);
        viewModel.HighNotStarted = viewModel.Items.Count(i => i.Priority == DocumentationPriority.High && i.Status == DocumentationStatus.NotStarted);

        await PopulateDropdownsAsync(systemId, family, docType, status, priority);
        return View(viewModel);
    }

    // GET: Documentation/Edit/5
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> Edit(int id, string? returnUrl)
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
            OwnerId = instance.OwnerId,
            Notes = instance.Notes,
            EvidenceLocation = instance.EvidenceLocation,
            TargetDate = instance.TargetDate,
            // Review fields
            ReviewStatus = instance.ReviewStatus,
            ReviewCycleMonths = instance.ReviewCycleMonths,
            NextReviewDate = instance.NextReviewDate,
            ReviewedDate = instance.ReviewedDate,
            ReviewedByName = instance.ReviewedBy?.DisplayName,
            // Milestone dates (read-only display)
            CompletedDate = instance.CompletedDate,
            ApprovedDate = instance.ApprovedDate,
            CreatedDate = instance.CreatedDate,
            LastUpdated = instance.ModifiedDate,
            ReturnUrl = returnUrl
        };

        await PopulateOwnersDropdownAsync(instance.OwnerId);
        return View(viewModel);
    }

    // POST: Documentation/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> Edit(DocumentationEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOwnersDropdownAsync(model.OwnerId);
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

        TempData["Success"] = "Documentation updated successfully.";

        if (!string.IsNullOrEmpty(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToAction(nameof(Index), new { systemId = model.SystemId });
    }

    // POST: Documentation/QuickUpdate (AJAX)
    [HttpPost]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> QuickUpdate(int id, DocumentationStatus status)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized();

        var instance = await _context.ControlDocumentationInstances.FindAsync(id);
        if (instance == null)
            return NotFound();

        var oldStatus = instance.Status;
        instance.Status = status;
        instance.ModifiedDate = DateTime.UtcNow;
        instance.LastUpdated = DateTime.UtcNow;

        // Track milestone dates based on status changes
        if (oldStatus != status)
        {
            if (status == DocumentationStatus.Complete && !instance.CompletedDate.HasValue)
            {
                instance.CompletedDate = DateTime.UtcNow;
            }
            if (status == DocumentationStatus.Approved && !instance.ApprovedDate.HasValue)
            {
                instance.ApprovedDate = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUser.Id,
            "Update",
            "ControlDocumentationInstance",
            id,
            new { Status = oldStatus },
            new { Status = status },
            GetIpAddress());

        return Json(new { success = true, newStatus = status.ToString() });
    }

    // GET: Documentation/Summary
    public async Task<IActionResult> Summary(int? systemId)
    {
        if (!systemId.HasValue)
        {
            await PopulateSystemsDropdownAsync(null);
            return View(new DocumentationSummaryPageViewModel());
        }

        var system = await _context.Systems.FindAsync(systemId.Value);
        if (system == null)
            return NotFound();

        // Ensure instances exist
        await _docService.EnsureDocumentationInstancesAsync(systemId.Value);

        // Get summary by family
        var familySummaries = await _context.ControlDocumentationInstances
            .Include(d => d.Requirement)
                .ThenInclude(r => r.NISTControl)
            .Where(d => d.SystemId == systemId.Value)
            .GroupBy(d => d.Requirement.NISTControl.Family)
            .Select(g => new DocumentationFamilySummaryViewModel
            {
                Family = g.Key,
                TotalRequirements = g.Count(),
                NotStarted = g.Count(d => d.Status == DocumentationStatus.NotStarted),
                InProgress = g.Count(d => d.Status == DocumentationStatus.InProgress || d.Status == DocumentationStatus.Draft || d.Status == DocumentationStatus.UnderReview),
                Complete = g.Count(d => d.Status == DocumentationStatus.Complete || d.Status == DocumentationStatus.Approved)
            })
            .OrderBy(f => f.Family)
            .ToListAsync();

        var viewModel = new DocumentationSummaryPageViewModel
        {
            SystemId = systemId.Value,
            SystemName = system.SystemName,
            FamilySummaries = familySummaries,
            TotalRequirements = familySummaries.Sum(f => f.TotalRequirements),
            TotalNotStarted = familySummaries.Sum(f => f.NotStarted),
            TotalInProgress = familySummaries.Sum(f => f.InProgress),
            TotalComplete = familySummaries.Sum(f => f.Complete)
        };

        await PopulateSystemsDropdownAsync(systemId);
        return View(viewModel);
    }

    private async Task PopulateDropdownsAsync(int? systemId, string? family, DocumentationType? docType,
        DocumentationStatus? status, DocumentationPriority? priority)
    {
        // Systems dropdown
        var systems = await _context.Systems
            .OrderBy(s => s.SystemName)
            .Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.SystemName,
                Selected = s.Id == systemId
            })
            .ToListAsync();
        systems.Insert(0, new SelectListItem { Value = "", Text = "-- Select System --" });
        ViewBag.Systems = systems;

        // Families dropdown
        var families = await _context.NISTControls
            .Select(c => c.Family)
            .Distinct()
            .OrderBy(f => f)
            .ToListAsync();
        ViewBag.Families = families.Select(f => new SelectListItem
        {
            Value = f,
            Text = $"{f} - {NISTFamilyNames.GetName(f)}",
            Selected = f == family
        }).ToList();

        // Doc types dropdown
        ViewBag.DocTypes = Enum.GetValues<DocumentationType>()
            .Select(d => new SelectListItem
            {
                Value = ((int)d).ToString(),
                Text = d.ToString(),
                Selected = d == docType
            }).ToList();

        // Status dropdown
        ViewBag.Statuses = Enum.GetValues<DocumentationStatus>()
            .Select(s => new SelectListItem
            {
                Value = ((int)s).ToString(),
                Text = s.ToString(),
                Selected = s == status
            }).ToList();

        // Priority dropdown
        ViewBag.Priorities = Enum.GetValues<DocumentationPriority>()
            .Select(p => new SelectListItem
            {
                Value = ((int)p).ToString(),
                Text = p.ToString(),
                Selected = p == priority
            }).ToList();
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
        systems.Insert(0, new SelectListItem { Value = "", Text = "-- Select System --" });
        ViewBag.Systems = systems;
    }

    private async Task PopulateOwnersDropdownAsync(int? selectedId)
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
