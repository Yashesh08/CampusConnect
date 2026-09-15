using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CampusConnect.Models.Enums;

namespace CampusConnect.Models;

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
