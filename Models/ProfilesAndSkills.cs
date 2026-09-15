using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CampusConnect.Models.Enums;

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

[Table("skills")]
public class Skill
{
    [Key, Column("skill_id")] public int SkillId { get; set; }
    [Required, MaxLength(100), Column("skill_name")] public string SkillName { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Category { get; set; } = string.Empty;
    public ICollection<StudentSkill> StudentSkills { get; set; } = new List<StudentSkill>();
}

[Table("student_skills")]
public class StudentSkill
{
    [Key, Column("student_skill_id")] public int StudentSkillId { get; set; }
    [Column("student_id")] public Guid StudentId { get; set; }
    [Column("skill_id")] public int SkillId { get; set; }
    [Required, Column("proficiency_level")] public ProficiencyLevel ProficiencyLevel { get; set; }
    public StudentProfile Student { get; set; } = null!;
    public Skill Skill { get; set; } = null!;
}
