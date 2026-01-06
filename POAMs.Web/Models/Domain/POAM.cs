using System.ComponentModel.DataAnnotations;

namespace POAMs.Web.Models.Domain;

public class POAM
{
    public int Id { get; set; }

    public int SystemId { get; set; }
    public SystemInfo System { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string ItemIdentifier { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string WeaknessOrDeficiency { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? SecurityControl { get; set; }

    public int? POCId { get; set; }
    public User? POC { get; set; }

    [MaxLength(500)]
    public string? ResourcesRequired { get; set; }

    public DateTime ScheduledCompletionDate { get; set; }

    [MaxLength(256)]
    public string? IdentifiedBy { get; set; }

    [Required]
    public RiskLevel RiskLevel { get; set; }

    public decimal? EstimatedCost { get; set; }

    [Required]
    public POAMStatus Status { get; set; } = POAMStatus.Draft;

    [MaxLength(2000)]
    public string? Comments { get; set; }

    public DateTime OriginalPOAMDate { get; set; } = DateTime.UtcNow;

    public DateTime LastUpdateDate { get; set; } = DateTime.UtcNow;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
    public ICollection<UserAssignment> UserAssignments { get; set; } = new List<UserAssignment>();
}
