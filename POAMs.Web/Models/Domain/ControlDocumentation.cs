using System.ComponentModel.DataAnnotations;

namespace POAMs.Web.Models.Domain;

/// <summary>
/// Documentation requirement template for a NIST control (seeded from CSV - reference data)
/// One control can have multiple documentation requirements (Policy, Procedure, Report, etc.)
/// </summary>
public class ControlDocumentationRequirement
{
    public int Id { get; set; }

    /// <summary>
    /// Link to NIST Control
    /// </summary>
    public int NISTControlId { get; set; }
    public NISTControl NISTControl { get; set; } = null!;

    /// <summary>
    /// Type of documentation (Policy, Procedure, Plan, Report, Record, Template, Evidence)
    /// </summary>
    [Required]
    public DocumentationType DocType { get; set; }

    /// <summary>
    /// Priority level for this documentation requirement
    /// </summary>
    public DocumentationPriority Priority { get; set; } = DocumentationPriority.Medium;

    /// <summary>
    /// Description/notes about what this documentation should contain
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Per-system instances of this documentation requirement
    /// </summary>
    public ICollection<ControlDocumentationInstance> Instances { get; set; } = new List<ControlDocumentationInstance>();

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Per-system instance of documentation for a control requirement
/// Tracks actual status, owner, evidence location for a specific system
/// </summary>
public class ControlDocumentationInstance
{
    public int Id { get; set; }

    /// <summary>
    /// Link to the documentation requirement template
    /// </summary>
    public int RequirementId { get; set; }
    public ControlDocumentationRequirement Requirement { get; set; } = null!;

    /// <summary>
    /// System (ATO Package) this documentation applies to
    /// </summary>
    public int SystemId { get; set; }
    public SystemInfo System { get; set; } = null!;

    /// <summary>
    /// Current status of this documentation
    /// </summary>
    public DocumentationStatus Status { get; set; } = DocumentationStatus.NotStarted;

    /// <summary>
    /// User responsible for this documentation
    /// </summary>
    public int? OwnerId { get; set; }
    public User? Owner { get; set; }

    /// <summary>
    /// Target completion date
    /// </summary>
    public DateTime? TargetDate { get; set; }

    /// <summary>
    /// Location of evidence/artifact (file path, URL, SharePoint link, etc.)
    /// Will eventually link to artifact store
    /// </summary>
    [MaxLength(500)]
    public string? EvidenceLocation { get; set; }

    /// <summary>
    /// Review status
    /// </summary>
    public ReviewStatus ReviewStatus { get; set; } = ReviewStatus.Pending;

    /// <summary>
    /// User who reviewed/approved the documentation
    /// </summary>
    public int? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }

    /// <summary>
    /// Date of last review
    /// </summary>
    public DateTime? ReviewedDate { get; set; }

    /// <summary>
    /// Review cycle in months (e.g., 12 = annual review required)
    /// </summary>
    public int? ReviewCycleMonths { get; set; }

    /// <summary>
    /// Next review due date (calculated or manually set)
    /// </summary>
    public DateTime? NextReviewDate { get; set; }

    /// <summary>
    /// Date the documentation was initially completed
    /// </summary>
    public DateTime? CompletedDate { get; set; }

    /// <summary>
    /// Date the documentation was approved
    /// </summary>
    public DateTime? ApprovedDate { get; set; }

    /// <summary>
    /// Additional notes
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Last time the documentation was updated
    /// </summary>
    public DateTime? LastUpdated { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Check if a review is due based on NextReviewDate
    /// </summary>
    public bool IsReviewDue => NextReviewDate.HasValue && NextReviewDate.Value <= DateTime.UtcNow;

    /// <summary>
    /// Check if a review is coming due within 30 days
    /// </summary>
    public bool IsReviewUpcoming => NextReviewDate.HasValue &&
        NextReviewDate.Value > DateTime.UtcNow &&
        NextReviewDate.Value <= DateTime.UtcNow.AddDays(30);
}
