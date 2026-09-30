using System.ComponentModel.DataAnnotations;
using CampusConnect.Models.Enums;

namespace CampusConnect.Models.ViewModels;

/// <summary>
/// ViewModel for the grievance submission form.
/// SLA due times are calculated server-side based on Priority.
///   - Critical → 1 business day
///   - High     → 3 business days
///   - Medium   → 5 business days
///   - Low      → 10 business days
/// </summary>
public class CreateGrievanceViewModel
{
    [Required(ErrorMessage = "Please select a category.")]
    [Display(Name = "Grievance Category")]
    public GrievanceCategory Category { get; set; }

    [Required(ErrorMessage = "Please select the routing department.")]
    [Display(Name = "Route To Department")]
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "Description is required.")]
    [MinLength(30, ErrorMessage = "Please provide at least 30 characters of detail.")]
    [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a priority level.")]
    [Display(Name = "Priority")]
    public GrievancePriority Priority { get; set; } = GrievancePriority.Medium;

    [Display(Name = "Submit Anonymously")]
    public bool IsAnonymous { get; set; } = false;
}
