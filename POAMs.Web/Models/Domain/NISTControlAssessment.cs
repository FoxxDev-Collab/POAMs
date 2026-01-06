using System.ComponentModel.DataAnnotations;

namespace POAMs.Web.Models.Domain;

/// <summary>
/// Per-System (ATO Package) compliance status for a NIST control
/// </summary>
public class NISTControlAssessment
{
    public int Id { get; set; }

    public int NISTControlId { get; set; }
    public NISTControl NISTControl { get; set; } = null!;

    public int SystemId { get; set; }
    public SystemInfo System { get; set; } = null!;

    [Required]
    public ControlAssessmentStatus Status { get; set; } = ControlAssessmentStatus.NotAssessed;

    /// <summary>
    /// Risk level for non-compliant controls
    /// </summary>
    public RiskLevel? RiskLevel { get; set; }

    /// <summary>
    /// Description of how the control is implemented
    /// </summary>
    public string? Implementation { get; set; }

    /// <summary>
    /// Evidence/artifact references supporting the assessment
    /// </summary>
    public string? Evidence { get; set; }

    /// <summary>
    /// Additional notes about the assessment
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// User who performed the assessment
    /// </summary>
    public int? AssessedById { get; set; }
    public User? AssessedBy { get; set; }

    /// <summary>
    /// Date the assessment was performed
    /// </summary>
    public DateTime? AssessedDate { get; set; }

    /// <summary>
    /// Optional soft link to POAM for non-compliant controls
    /// </summary>
    public int? POAMId { get; set; }
    public POAM? POAM { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
}
