using System.ComponentModel.DataAnnotations;
using POAMs.Web.Models.Domain;

namespace POAMs.Web.Models.ViewModels;

public class MilestoneEditViewModel
{
    public int Id { get; set; }

    public int POAMId { get; set; }

    public string POAMIdentifier { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Milestone #")]
    public int MilestoneNumber { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [MaxLength(500)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Due Date")]
    public DateTime DueDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Completed Date")]
    public DateTime? CompletedDate { get; set; }

    [Required]
    [Display(Name = "Status")]
    public MilestoneStatus Status { get; set; }

    [MaxLength(1000)]
    [Display(Name = "Changes")]
    public string? Changes { get; set; }

    [Display(Name = "Assigned To")]
    public int? AssignedToId { get; set; }
}
