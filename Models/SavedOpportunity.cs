using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusConnect.Models;

[Table("saved_opportunities")]
public class SavedOpportunity
{
    [Key, Column("saved_opportunity_id")]
    public Guid SavedOpportunityId { get; set; } = Guid.NewGuid();

    [Column("student_id")]
    public Guid StudentId { get; set; }

    [Column("opportunity_id")]
    public Guid OpportunityId { get; set; }

    [Column("saved_at")]
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;

    public StudentProfile Student { get; set; } = null!;
    public Opportunity Opportunity { get; set; } = null!;
}
