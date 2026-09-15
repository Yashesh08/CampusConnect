using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusConnect.Models;

[Table("student_profiles")]
public class StudentProfile
{
    [Key, Column("profile_id")] public Guid ProfileId { get; set; } = Guid.NewGuid();
    [Required, Column("user_id")] public Guid UserId { get; set; }
    [Required, MaxLength(50), Column("roll_number")] public string RollNumber { get; set; } = string.Empty;
    [Column("department_id")] public int DepartmentId { get; set; }
    [Column("batch_year")] public int BatchYear { get; set; }
    public string? Bio { get; set; }
    [MaxLength(500), Column("github_url")] public string? GitHubUrl { get; set; }
    [MaxLength(500), Column("linkedin_url")] public string? LinkedInUrl { get; set; }
    [MaxLength(500), Column("resume_url")] public string? ResumeUrl { get; set; }
    [Range(0, 100), Column("profile_completion_score")] public int ProfileCompletionScore { get; set; }
    public User User { get; set; } = null!;
    public Department Department { get; set; } = null!;
    public ICollection<StudentSkill> StudentSkills { get; set; } = new List<StudentSkill>();
}
