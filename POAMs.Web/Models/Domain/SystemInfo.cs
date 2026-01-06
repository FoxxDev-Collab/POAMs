using System.ComponentModel.DataAnnotations;

namespace POAMs.Web.Models.Domain;

public class SystemInfo
{
    public int Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string SystemName { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string OrganizationName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ISType { get; set; }

    [MaxLength(100)]
    public string? UID { get; set; }

    public int? ISSMId { get; set; }
    public User? ISSM { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<POAM> POAMs { get; set; } = new List<POAM>();
}
