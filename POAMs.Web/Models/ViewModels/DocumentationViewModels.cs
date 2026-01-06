using System.ComponentModel.DataAnnotations;
using POAMs.Web.Models.Domain;

namespace POAMs.Web.Models.ViewModels;

/// <summary>
/// View model for documentation requirement (template data)
/// </summary>
public class DocumentationRequirementViewModel
{
    public int Id { get; set; }
    public string ControlId { get; set; } = string.Empty;
    public DocumentationType DocType { get; set; }
    public DocumentationPriority Priority { get; set; }
    public string? Description { get; set; }

    public string DocTypeName => DocType.ToString();
    public string PriorityName => Priority.ToString();

    public string PriorityBadgeClass => Priority switch
    {
        DocumentationPriority.Critical => "danger",
        DocumentationPriority.High => "warning",
        DocumentationPriority.Medium => "info",
        DocumentationPriority.Low => "secondary",
        _ => "secondary"
    };

    public string DocTypeIcon => DocType switch
    {
        DocumentationType.Policy => "bi-file-earmark-ruled",
        DocumentationType.Procedure => "bi-list-check",
        DocumentationType.Plan => "bi-calendar-check",
        DocumentationType.Report => "bi-file-earmark-bar-graph",
        DocumentationType.Record => "bi-journal-text",
        DocumentationType.Template => "bi-file-earmark-plus",
        DocumentationType.Evidence => "bi-folder-check",
        _ => "bi-file-earmark"
    };
}

/// <summary>
/// View model for documentation instance (per-system tracking)
/// </summary>
public class DocumentationInstanceViewModel : DocumentationRequirementViewModel
{
    public int RequirementId { get; set; }
    public int SystemId { get; set; }
    public DocumentationStatus Status { get; set; }
    public int? OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public DateTime? TargetDate { get; set; }
    public string? EvidenceLocation { get; set; }
    public ReviewStatus ReviewStatus { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedDate { get; set; }
    public string? Notes { get; set; }
    public DateTime? LastUpdated { get; set; }

    public string StatusName => Status.ToString();

    public string StatusBadgeClass => Status switch
    {
        DocumentationStatus.NotStarted => "secondary",
        DocumentationStatus.InProgress => "primary",
        DocumentationStatus.Draft => "info",
        DocumentationStatus.UnderReview => "warning",
        DocumentationStatus.Approved => "success",
        DocumentationStatus.Complete => "success",
        _ => "secondary"
    };

    public string ReviewStatusBadgeClass => ReviewStatus switch
    {
        Domain.ReviewStatus.Pending => "secondary",
        Domain.ReviewStatus.InReview => "warning",
        Domain.ReviewStatus.ChangesRequested => "danger",
        Domain.ReviewStatus.Approved => "success",
        _ => "secondary"
    };

    public bool IsOverdue => TargetDate.HasValue && TargetDate.Value < DateTime.UtcNow && Status != DocumentationStatus.Complete && Status != DocumentationStatus.Approved;
}

/// <summary>
/// Edit view model for updating documentation status
/// </summary>
public class DocumentationEditViewModel
{
    public int Id { get; set; }

    public string ControlId { get; set; } = string.Empty;
    public string ControlName { get; set; } = string.Empty;
    public int SystemId { get; set; }
    public string SystemName { get; set; } = string.Empty;

    public DocumentationType DocType { get; set; }
    public string DocTypeName => DocType.ToString();
    public DocumentationPriority Priority { get; set; }
    public string? RequirementDescription { get; set; }

    [Required]
    public DocumentationStatus Status { get; set; }

    public int? OwnerId { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Target Completion Date")]
    public DateTime? TargetDate { get; set; }

    [MaxLength(500)]
    [Display(Name = "Evidence Location")]
    public string? EvidenceLocation { get; set; }

    [Display(Name = "Review Status")]
    public ReviewStatus? ReviewStatus { get; set; }

    [Display(Name = "Review Cycle (Months)")]
    [Range(1, 60, ErrorMessage = "Review cycle must be between 1 and 60 months")]
    public int? ReviewCycleMonths { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Next Review Date")]
    public DateTime? NextReviewDate { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    // Read-only milestone dates for display
    public DateTime? CompletedDate { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public DateTime? ReviewedDate { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastUpdated { get; set; }

    /// <summary>
    /// URL to return to after editing
    /// </summary>
    public string? ReturnUrl { get; set; }
}

/// <summary>
/// Summary of documentation status for a system
/// </summary>
public class DocumentationSummaryViewModel
{
    public int TotalRequirements { get; set; }
    public int NotStarted { get; set; }
    public int InProgress { get; set; }
    public int UnderReview { get; set; }
    public int Complete { get; set; }
    public int CriticalNotStarted { get; set; }
    public int HighNotStarted { get; set; }

    public decimal CompletionRate => TotalRequirements > 0
        ? Math.Round((decimal)Complete / TotalRequirements * 100, 1)
        : 0;

    public decimal ProgressRate => TotalRequirements > 0
        ? Math.Round((decimal)(Complete + InProgress + UnderReview) / TotalRequirements * 100, 1)
        : 0;
}

/// <summary>
/// Documentation summary for a control family
/// </summary>
public class DocumentationFamilySummaryViewModel
{
    public string Family { get; set; } = string.Empty;
    public int TotalRequirements { get; set; }
    public int NotStarted { get; set; }
    public int InProgress { get; set; }
    public int Complete { get; set; }
    public int TrackedCount { get; set; }

    public decimal CompletionRate => TotalRequirements > 0
        ? Math.Round((decimal)Complete / TotalRequirements * 100, 1)
        : 0;
}

/// <summary>
/// Main view model for the documentation matrix page
/// </summary>
public class DocumentationMatrixViewModel
{
    public int? SystemId { get; set; }
    public string? SystemName { get; set; }

    // Filter values
    public string? Family { get; set; }
    public DocumentationType? DocTypeFilter { get; set; }
    public DocumentationStatus? StatusFilter { get; set; }
    public DocumentationPriority? PriorityFilter { get; set; }
    public string? SearchTerm { get; set; }

    // Data
    public List<DocumentationMatrixItemViewModel> Items { get; set; } = new();

    // Summary stats
    public int TotalCount { get; set; }
    public int NotStartedCount { get; set; }
    public int InProgressCount { get; set; }
    public int UnderReviewCount { get; set; }
    public int CompleteCount { get; set; }
    public int CriticalNotStarted { get; set; }
    public int HighNotStarted { get; set; }

    public decimal CompletionRate => TotalCount > 0
        ? Math.Round((decimal)CompleteCount / TotalCount * 100, 1)
        : 0;
}

/// <summary>
/// Individual item in the documentation matrix
/// </summary>
public class DocumentationMatrixItemViewModel
{
    public int Id { get; set; }
    public int RequirementId { get; set; }
    public string ControlId { get; set; } = string.Empty;
    public string ControlName { get; set; } = string.Empty;
    public string Family { get; set; } = string.Empty;
    public DocumentationType DocType { get; set; }
    public DocumentationPriority Priority { get; set; }
    public string? Description { get; set; }
    public DocumentationStatus Status { get; set; }
    public int? OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public DateTime? TargetDate { get; set; }
    public string? EvidenceLocation { get; set; }
    public string? Notes { get; set; }
    public DateTime? LastUpdated { get; set; }

    // Review tracking
    public ReviewStatus ReviewStatus { get; set; }
    public DateTime? ReviewedDate { get; set; }
    public string? ReviewedByName { get; set; }
    public int? ReviewCycleMonths { get; set; }
    public DateTime? NextReviewDate { get; set; }

    // Milestone dates
    public DateTime? CompletedDate { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public DateTime CreatedDate { get; set; }

    public string DocTypeName => DocType.ToString();
    public string PriorityName => Priority.ToString();
    public string StatusName => Status.ToString();

    public string PriorityBadgeClass => Priority switch
    {
        DocumentationPriority.Critical => "danger",
        DocumentationPriority.High => "warning",
        DocumentationPriority.Medium => "info",
        DocumentationPriority.Low => "secondary",
        _ => "secondary"
    };

    public string StatusBadgeClass => Status switch
    {
        DocumentationStatus.NotStarted => "secondary",
        DocumentationStatus.InProgress => "primary",
        DocumentationStatus.Draft => "info",
        DocumentationStatus.UnderReview => "warning",
        DocumentationStatus.Approved => "success",
        DocumentationStatus.Complete => "success",
        _ => "secondary"
    };

    public string DocTypeIcon => DocType switch
    {
        DocumentationType.Policy => "bi-file-earmark-ruled",
        DocumentationType.Procedure => "bi-list-check",
        DocumentationType.Plan => "bi-calendar-check",
        DocumentationType.Report => "bi-file-earmark-bar-graph",
        DocumentationType.Record => "bi-journal-text",
        DocumentationType.Template => "bi-file-earmark-plus",
        DocumentationType.Evidence => "bi-folder-check",
        _ => "bi-file-earmark"
    };

    public bool IsOverdue => TargetDate.HasValue &&
        TargetDate.Value < DateTime.UtcNow &&
        Status != DocumentationStatus.Complete &&
        Status != DocumentationStatus.Approved;

    public bool IsReviewDue => NextReviewDate.HasValue && NextReviewDate.Value <= DateTime.UtcNow;

    public bool IsReviewUpcoming => NextReviewDate.HasValue &&
        NextReviewDate.Value > DateTime.UtcNow &&
        NextReviewDate.Value <= DateTime.UtcNow.AddDays(30);

    public string ReviewStatusBadgeClass => ReviewStatus switch
    {
        Domain.ReviewStatus.Pending => "secondary",
        Domain.ReviewStatus.InReview => "warning",
        Domain.ReviewStatus.ChangesRequested => "danger",
        Domain.ReviewStatus.Approved => "success",
        _ => "secondary"
    };
}

/// <summary>
/// View model for the documentation summary page
/// </summary>
public class DocumentationSummaryPageViewModel
{
    public int? SystemId { get; set; }
    public string? SystemName { get; set; }

    public List<DocumentationFamilySummaryViewModel> FamilySummaries { get; set; } = new();

    public int TotalRequirements { get; set; }
    public int TotalNotStarted { get; set; }
    public int TotalInProgress { get; set; }
    public int TotalComplete { get; set; }

    public decimal OverallCompletionRate => TotalRequirements > 0
        ? Math.Round((decimal)TotalComplete / TotalRequirements * 100, 1)
        : 0;
}
