using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CampusConnect.Models.Enums;

namespace CampusConnect.Models;

[Table("applications")]
public class Application
{
    [Key, Column("application_id")] public Guid ApplicationId { get; set; } = Guid.NewGuid();
    [Column("opportunity_id")] public Guid OpportunityId { get; set; }
    [Column("student_id")] public Guid StudentId { get; set; }
    [Required] public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;
    [Column("applied_at")] public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    [Column("organizer_remarks")] public string? OrganizerRemarks { get; set; }
    public Opportunity Opportunity { get; set; } = null!;
    public StudentProfile Student { get; set; } = null!;
}
