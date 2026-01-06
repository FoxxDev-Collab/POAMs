using System.ComponentModel.DataAnnotations;

namespace POAMs.Web.Models.Domain;

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Phone { get; set; }

    public string? PasswordHash { get; set; }

    [Required]
    public UserRole Role { get; set; }

    public bool IsWindowsAuth { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<POAM> POAMsAsPOC { get; set; } = new List<POAM>();
    public ICollection<Milestone> AssignedMilestones { get; set; } = new List<Milestone>();
    public ICollection<UserAssignment> Assignments { get; set; } = new List<UserAssignment>();
    public ICollection<SystemInfo> ManagedSystems { get; set; } = new List<SystemInfo>();
}
