using System.ComponentModel.DataAnnotations;
using POAMs.Web.Models.Domain;

namespace POAMs.Web.Models.ViewModels;

public class ADConfigurationViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "LDAP Server is required")]
    [Display(Name = "LDAP Server")]
    [MaxLength(256)]
    public string LdapServer { get; set; } = string.Empty;

    [Display(Name = "LDAP Port")]
    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
    public int LdapPort { get; set; } = 389;

    [Display(Name = "Use SSL/TLS (LDAPS)")]
    public bool UseSsl { get; set; }

    [Required(ErrorMessage = "Domain is required")]
    [Display(Name = "Domain")]
    [MaxLength(256)]
    public string Domain { get; set; } = string.Empty;

    [Required(ErrorMessage = "Base DN is required")]
    [Display(Name = "Base DN")]
    [MaxLength(500)]
    public string BaseDN { get; set; } = string.Empty;

    [Required(ErrorMessage = "Service Account Username is required")]
    [Display(Name = "Service Account Username")]
    [MaxLength(256)]
    public string ServiceAccountUsername { get; set; } = string.Empty;

    [Display(Name = "Service Account Password")]
    [DataType(DataType.Password)]
    public string? ServiceAccountPassword { get; set; }

    [Display(Name = "Enable AD Integration")]
    public bool IsEnabled { get; set; }

    [Display(Name = "Auto-Create Users on First Login")]
    public bool AutoCreateUsers { get; set; } = true;

    [Display(Name = "Default Role for New Users")]
    public UserRole DefaultNewUserRole { get; set; } = UserRole.ISSO;

    // Read-only status info
    public DateTime? LastConnectionTest { get; set; }
    public bool? LastConnectionSuccess { get; set; }
    public string? LastConnectionError { get; set; }

    public bool HasPassword { get; set; }
}
