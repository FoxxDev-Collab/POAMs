using System.ComponentModel.DataAnnotations;
using POAMs.Web.Models.Domain;

namespace POAMs.Web.Models.ViewModels;

public class POAMEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "System is required")]
    [Display(Name = "System")]
    public int SystemId { get; set; }

    [Required(ErrorMessage = "Item Identifier is required")]
    [MaxLength(50)]
    [Display(Name = "Item Identifier")]
    public string ItemIdentifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "Weakness or Deficiency is required")]
    [MaxLength(1000)]
    [Display(Name = "Weakness or Deficiency")]
    public string WeaknessOrDeficiency { get; set; } = string.Empty;

    [MaxLength(50)]
    [Display(Name = "Security Control")]
    public string? SecurityControl { get; set; }

    [Display(Name = "Point of Contact")]
    public int? POCId { get; set; }

    [MaxLength(500)]
    [Display(Name = "Resources Required")]
    public string? ResourcesRequired { get; set; }

    [Required(ErrorMessage = "Scheduled Completion Date is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Scheduled Completion Date")]
    public DateTime ScheduledCompletionDate { get; set; }

    [MaxLength(256)]
    [Display(Name = "Identified By")]
    public string? IdentifiedBy { get; set; }

    [Required]
    [Display(Name = "Risk Level")]
    public RiskLevel RiskLevel { get; set; }

    [Display(Name = "Estimated Cost")]
    [DataType(DataType.Currency)]
    public decimal? EstimatedCost { get; set; }

    [Required]
    [Display(Name = "Status")]
    public POAMStatus Status { get; set; }

    [MaxLength(2000)]
    [Display(Name = "Comments")]
    public string? Comments { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Original POA&M Date")]
    public DateTime OriginalPOAMDate { get; set; }
}
