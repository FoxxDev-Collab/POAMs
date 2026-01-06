using System.ComponentModel.DataAnnotations;

namespace POAMs.Web.Models.Domain;

public class Milestone
{
    public int Id { get; set; }

    public int POAMId { get; set; }
    public POAM POAM { get; set; } = null!;

    public int MilestoneNumber { get; set; }

    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    public DateTime DueDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    [Required]
    public MilestoneStatus Status { get; set; } = MilestoneStatus.NotStarted;

    [MaxLength(1000)]
    public string? Changes { get; set; }

    public int? AssignedToId { get; set; }
    public User? AssignedTo { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
}
