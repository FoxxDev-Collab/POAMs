using System.ComponentModel.DataAnnotations;

namespace POAMs.Web.Models.ViewModels;

public class SystemEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "System Name is required")]
    [MaxLength(256)]
    [Display(Name = "System Name")]
    public string SystemName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Organization Name is required")]
    [MaxLength(256)]
    [Display(Name = "Organization Name")]
    public string OrganizationName { get; set; } = string.Empty;

    [MaxLength(100)]
    [Display(Name = "IS Type")]
    public string? ISType { get; set; }

    [MaxLength(100)]
    [Display(Name = "System UID")]
    public string? UID { get; set; }

    [Display(Name = "ISSM")]
    public int? ISSMId { get; set; }
}
