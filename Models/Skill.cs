using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusConnect.Models;

[Table("skills")]
public class Skill
{
    [Key, Column("skill_id")] public int SkillId { get; set; }
    [Required, MaxLength(100), Column("skill_name")] public string SkillName { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Category { get; set; } = string.Empty;
    public ICollection<StudentSkill> StudentSkills { get; set; } = new List<StudentSkill>();
}
