using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CampusConnect.Models.Enums;

namespace CampusConnect.Models;

[Table("opportunities")]
public class Opportunity
{
    [Key, Column("opportunity_id")] public Guid OpportunityId { get; set; } = Guid.NewGuid();
    [Column("organizer_id")] public Guid OrganizerId { get; set; }
    [Required, MaxLength(250)] public string Title { get; set; } = string.Empty;
    [Required] public string Description { get; set; } = string.Empty;
    [Required] public OpportunityCategory Category { get; set; }
    [Column("target_department_id")] public int? TargetDepartmentId { get; set; }
    [Required, Column("work_mode")] public WorkMode WorkMode { get; set; }
    [MaxLength(100), Column("stipend_salary")] public string? StipendSalary { get; set; }
    [Column("registration_deadline")] public DateTime RegistrationDeadline { get; set; }
    [Column("event_date")] public DateTime? EventDate { get; set; }
    public int? Capacity { get; set; }
    [Required, Column("approval_status")] public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.PendingReview;
    public User Organizer { get; set; } = null!;
    public Department? TargetDepartment { get; set; }
    public ICollection<Application> Applications { get; set; } = new List<Application>();
}

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
