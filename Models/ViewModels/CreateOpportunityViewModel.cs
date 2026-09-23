using System.ComponentModel.DataAnnotations;
using CampusConnect.Models.Enums;

namespace CampusConnect.Models.ViewModels;

public class CreateOpportunityViewModel
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(250, ErrorMessage = "Title cannot exceed 250 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required")]
    public OpportunityCategory Category { get; set; }

    [Display(Name = "Target Department")]
    public int? TargetDepartmentId { get; set; }

    [Required(ErrorMessage = "Work mode is required")]
    [Display(Name = "Work Mode")]
    public WorkMode WorkMode { get; set; }

    [Display(Name = "Stipend / Salary / Perks")]
    public string? StipendSalary { get; set; }

    [Required(ErrorMessage = "Registration deadline is required")]
    [Display(Name = "Registration Deadline")]
    public DateTime RegistrationDeadline { get; set; } = DateTime.Today.AddDays(14);

    [Display(Name = "Event Date (if applicable)")]
    public DateTime? EventDate { get; set; }

    [Range(1, 10000, ErrorMessage = "Capacity must be at least 1")]
    public int? Capacity { get; set; }

    [Display(Name = "Required Skills")]
    public List<int> SelectedSkillIds { get; set; } = new List<int>();
}
