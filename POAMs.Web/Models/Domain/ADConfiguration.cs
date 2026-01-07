using System.ComponentModel.DataAnnotations;

namespace POAMs.Web.Models.Domain;

public class ADConfiguration
{
    public int Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string LdapServer { get; set; } = string.Empty;

    public int LdapPort { get; set; } = 389;

    public bool UseSsl { get; set; }

    [Required]
    [MaxLength(256)]
    public string Domain { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string BaseDN { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string ServiceAccountUsername { get; set; } = string.Empty;

    [Required]
    public string ServiceAccountPasswordEncrypted { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public bool AutoCreateUsers { get; set; } = true;

    public UserRole DefaultNewUserRole { get; set; } = UserRole.ISSO;

    public DateTime? LastConnectionTest { get; set; }

    public bool? LastConnectionSuccess { get; set; }

    [MaxLength(500)]
    public string? LastConnectionError { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    public int? ModifiedById { get; set; }

    public User? ModifiedBy { get; set; }
}
