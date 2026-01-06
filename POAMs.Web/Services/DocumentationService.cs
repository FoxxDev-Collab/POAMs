using Microsoft.EntityFrameworkCore;
using POAMs.Web.Data;
using POAMs.Web.Models.Domain;
using POAMs.Web.Models.ViewModels;

namespace POAMs.Web.Services;

public class DocumentationService : IDocumentationService
{
    private readonly ApplicationDbContext _context;

    public DocumentationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<DocumentationRequirementViewModel>> GetRequirementsForControlAsync(string controlId)
    {
        return await _context.ControlDocumentationRequirements
            .Include(r => r.NISTControl)
            .Where(r => r.NISTControl.ControlId == controlId)
            .OrderBy(r => r.DocType)
            .Select(r => new DocumentationRequirementViewModel
            {
                Id = r.Id,
                ControlId = r.NISTControl.ControlId,
                DocType = r.DocType,
                Priority = r.Priority,
                Description = r.Description
            })
            .ToListAsync();
    }

    public async Task<List<DocumentationInstanceViewModel>> GetDocumentationForControlAsync(string controlId, int systemId)
    {
        // Get all requirements for the control
        var requirements = await _context.ControlDocumentationRequirements
            .Include(r => r.NISTControl)
            .Where(r => r.NISTControl.ControlId == controlId)
            .ToListAsync();

        var result = new List<DocumentationInstanceViewModel>();

        foreach (var req in requirements)
        {
            // Get or create instance for this requirement/system
            var instance = await _context.ControlDocumentationInstances
                .Include(i => i.Owner)
                .Include(i => i.ReviewedBy)
                .FirstOrDefaultAsync(i => i.RequirementId == req.Id && i.SystemId == systemId);

            if (instance == null)
            {
                // Create a new instance
                instance = new ControlDocumentationInstance
                {
                    RequirementId = req.Id,
                    SystemId = systemId,
                    Status = DocumentationStatus.NotStarted,
                    ReviewStatus = ReviewStatus.Pending
                };
                _context.ControlDocumentationInstances.Add(instance);
                await _context.SaveChangesAsync();
            }

            result.Add(new DocumentationInstanceViewModel
            {
                Id = instance.Id,
                RequirementId = req.Id,
                ControlId = req.NISTControl.ControlId,
                DocType = req.DocType,
                Priority = req.Priority,
                Description = req.Description,
                SystemId = systemId,
                Status = instance.Status,
                OwnerId = instance.OwnerId,
                OwnerName = instance.Owner?.DisplayName,
                TargetDate = instance.TargetDate,
                EvidenceLocation = instance.EvidenceLocation,
                ReviewStatus = instance.ReviewStatus,
                ReviewedByName = instance.ReviewedBy?.DisplayName,
                ReviewedDate = instance.ReviewedDate,
                Notes = instance.Notes,
                LastUpdated = instance.LastUpdated
            });
        }

        return result.OrderBy(d => d.DocType).ThenBy(d => d.Priority).ToList();
    }

    public async Task<ControlDocumentationInstance?> GetInstanceByIdAsync(int instanceId)
    {
        return await _context.ControlDocumentationInstances
            .Include(i => i.Requirement)
                .ThenInclude(r => r.NISTControl)
            .Include(i => i.System)
            .Include(i => i.Owner)
            .Include(i => i.ReviewedBy)
            .FirstOrDefaultAsync(i => i.Id == instanceId);
    }

    public async Task<ControlDocumentationInstance> UpdateInstanceAsync(DocumentationEditViewModel model, int userId)
    {
        var instance = await _context.ControlDocumentationInstances.FindAsync(model.Id)
            ?? throw new ArgumentException($"Documentation instance {model.Id} not found");

        var oldStatus = instance.Status;

        instance.Status = model.Status;
        instance.OwnerId = model.OwnerId;
        instance.TargetDate = model.TargetDate;
        instance.EvidenceLocation = model.EvidenceLocation;
        instance.Notes = model.Notes;
        instance.LastUpdated = DateTime.UtcNow;
        instance.ModifiedDate = DateTime.UtcNow;

        // Update review cycle fields
        instance.ReviewCycleMonths = model.ReviewCycleMonths;
        instance.NextReviewDate = model.NextReviewDate;

        // Update review status if being reviewed
        if (model.ReviewStatus.HasValue && model.ReviewStatus != instance.ReviewStatus)
        {
            instance.ReviewStatus = model.ReviewStatus.Value;
            instance.ReviewedById = userId;
            instance.ReviewedDate = DateTime.UtcNow;

            // Auto-calculate next review date if review cycle is set and approved
            if (model.ReviewStatus == ReviewStatus.Approved && instance.ReviewCycleMonths.HasValue)
            {
                instance.NextReviewDate = DateTime.UtcNow.AddMonths(instance.ReviewCycleMonths.Value);
            }
        }

        // Track milestone dates based on status changes
        if (oldStatus != model.Status)
        {
            if (model.Status == DocumentationStatus.Complete && !instance.CompletedDate.HasValue)
            {
                instance.CompletedDate = DateTime.UtcNow;
            }
            if (model.Status == DocumentationStatus.Approved && !instance.ApprovedDate.HasValue)
            {
                instance.ApprovedDate = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
        return instance;
    }

    public async Task<DocumentationSummaryViewModel> GetSystemDocumentationSummaryAsync(int systemId)
    {
        var instances = await _context.ControlDocumentationInstances
            .Where(i => i.SystemId == systemId)
            .Select(i => new { i.Status, i.Requirement.Priority })
            .ToListAsync();

        var total = await _context.ControlDocumentationRequirements.CountAsync();

        return new DocumentationSummaryViewModel
        {
            TotalRequirements = total,
            NotStarted = instances.Count(i => i.Status == DocumentationStatus.NotStarted),
            InProgress = instances.Count(i => i.Status == DocumentationStatus.InProgress || i.Status == DocumentationStatus.Draft),
            UnderReview = instances.Count(i => i.Status == DocumentationStatus.UnderReview),
            Complete = instances.Count(i => i.Status == DocumentationStatus.Approved || i.Status == DocumentationStatus.Complete),
            CriticalNotStarted = instances.Count(i => i.Status == DocumentationStatus.NotStarted && i.Priority == DocumentationPriority.Critical),
            HighNotStarted = instances.Count(i => i.Status == DocumentationStatus.NotStarted && i.Priority == DocumentationPriority.High)
        };
    }

    public async Task<DocumentationFamilySummaryViewModel> GetFamilyDocumentationSummaryAsync(string family, int systemId)
    {
        var requirements = await _context.ControlDocumentationRequirements
            .Include(r => r.NISTControl)
            .Where(r => r.NISTControl.Family == family.ToUpper())
            .Select(r => r.Id)
            .ToListAsync();

        var instances = await _context.ControlDocumentationInstances
            .Where(i => i.SystemId == systemId && requirements.Contains(i.RequirementId))
            .Select(i => i.Status)
            .ToListAsync();

        return new DocumentationFamilySummaryViewModel
        {
            Family = family,
            TotalRequirements = requirements.Count,
            NotStarted = instances.Count(s => s == DocumentationStatus.NotStarted),
            InProgress = instances.Count(s => s == DocumentationStatus.InProgress || s == DocumentationStatus.Draft),
            Complete = instances.Count(s => s == DocumentationStatus.Approved || s == DocumentationStatus.Complete),
            TrackedCount = instances.Count
        };
    }

    public async Task EnsureDocumentationInstancesAsync(int systemId)
    {
        var allRequirements = await _context.ControlDocumentationRequirements.ToListAsync();
        var existingInstances = await _context.ControlDocumentationInstances
            .Where(i => i.SystemId == systemId)
            .Select(i => i.RequirementId)
            .ToListAsync();

        var missingRequirements = allRequirements
            .Where(r => !existingInstances.Contains(r.Id))
            .ToList();

        foreach (var req in missingRequirements)
        {
            var instance = new ControlDocumentationInstance
            {
                RequirementId = req.Id,
                SystemId = systemId,
                Status = DocumentationStatus.NotStarted,
                ReviewStatus = ReviewStatus.Pending
            };
            _context.ControlDocumentationInstances.Add(instance);
        }

        if (missingRequirements.Any())
        {
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetRequirementCountAsync()
    {
        return await _context.ControlDocumentationRequirements.CountAsync();
    }
}
