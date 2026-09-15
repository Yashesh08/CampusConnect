using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusConnect.Models;

[Table("departments")]
public class Department
{
    [Key, Column("department_id")]
    public int DepartmentId { get; set; }

    [Required, MaxLength(150), Column("department_name")]
    public string DepartmentName { get; set; } = string.Empty;

    [Required, MaxLength(20), Column("department_code")]
    public string DepartmentCode { get; set; } = string.Empty;

    public ICollection<StudentProfile> StudentProfiles { get; set; } = new List<StudentProfile>();
    public ICollection<FacultyProfile> FacultyProfiles { get; set; } = new List<FacultyProfile>();
}
