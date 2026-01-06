using System.ComponentModel.DataAnnotations;
using POAMs.Web.Models.Domain;

namespace POAMs.Web.Models.ViewModels;

public class UserEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Username is required")]
    [MaxLength(256)]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [MaxLength(256)]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Display Name is required")]
    [MaxLength(256)]
    [Display(Name = "Display Name")]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(50)]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [Display(Name = "Password")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [Display(Name = "Confirm Password")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string? ConfirmPassword { get; set; }

    [Required]
    [Display(Name = "Role")]
    public UserRole Role { get; set; }

    [Display(Name = "Windows Authentication")]
    public bool IsWindowsAuth { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
