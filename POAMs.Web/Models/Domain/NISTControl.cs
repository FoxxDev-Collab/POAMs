using System.ComponentModel.DataAnnotations;

namespace POAMs.Web.Models.Domain;

/// <summary>
/// NIST 800-53 Security Control - seeded from catalog.json (read-only reference data)
/// </summary>
public class NISTControl
{
    public int Id { get; set; }

    /// <summary>
    /// Control identifier (e.g., "AC-1", "AC-2(1)")
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string ControlId { get; set; } = string.Empty;

    /// <summary>
    /// Control family prefix (e.g., "AC", "AU", "CA")
    /// </summary>
    [Required]
    [MaxLength(5)]
    public string Family { get; set; } = string.Empty;

    /// <summary>
    /// Control name/title
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Full control requirement text
    /// </summary>
    public string ControlText { get; set; } = string.Empty;

    /// <summary>
    /// Supplemental guidance/discussion
    /// </summary>
    public string? Discussion { get; set; }

    /// <summary>
    /// JSON array of related control IDs
    /// </summary>
    public string? RelatedControls { get; set; }

    /// <summary>
    /// True if control is withdrawn (incorporated into another control)
    /// </summary>
    public bool IsWithdrawn { get; set; }

    /// <summary>
    /// For control enhancements, the parent control ID (e.g., "AC-2" for "AC-2(1)")
    /// </summary>
    [MaxLength(20)]
    public string? ParentControlId { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<CCI> CCIs { get; set; } = new List<CCI>();
    public ICollection<NISTControlAssessment> Assessments { get; set; } = new List<NISTControlAssessment>();
}
