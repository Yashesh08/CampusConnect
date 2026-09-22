using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CampusConnect.Models.Enums;

namespace CampusConnect.Models;

[Table("opportunity_skills")]
public class OpportunitySkill
{
    [Key, Column("opportunity_skill_id")] public int OpportunitySkillId { get; set; }
    
    [Column("opportunity_id")] public Guid OpportunityId { get; set; }
    
    [Column("skill_id")] public int SkillId { get; set; }
    
    // Optional: We can add a MinimumProficiency required, but for MVP we keep it simple.
    // [Column("minimum_proficiency")] public ProficiencyLevel? MinimumProficiency { get; set; }

    public Opportunity Opportunity { get; set; } = null!;
    public Skill Skill { get; set; } = null!;
}
