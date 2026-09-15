using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusConnect.Models;

[Table("faculty_profiles")]
public class FacultyProfile
{
    [Key, Column("faculty_profile_id")] public Guid FacultyProfileId { get; set; } = Guid.NewGuid();
    [Required, Column("user_id")] public Guid UserId { get; set; }
    [Column("department_id")] public int DepartmentId { get; set; }
    [Required, MaxLength(150)] public string Designation { get; set; } = string.Empty;
    [MaxLength(50), Column("cabin_number")] public string? CabinNumber { get; set; }
    public User User { get; set; } = null!;
    public Department Department { get; set; } = null!;
}
