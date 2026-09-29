using System.ComponentModel.DataAnnotations;

namespace CampusConnect.Models.ViewModels;

public class FacultyProfileViewModel
{
    public Guid FacultyProfileId { get; set; }

    [Required]
    [Display(Name = "Department")]
    public int DepartmentId { get; set; }

    [Required]
    [MaxLength(150)]
    public string Designation { get; set; } = string.Empty;

    [MaxLength(50)]
    [Display(Name = "Cabin Number")]
    public string? CabinNumber { get; set; }
}
