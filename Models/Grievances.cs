using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CampusConnect.Models.Enums;

namespace CampusConnect.Models;

[Table("grievances")]
public class Grievance
{
    [Key, Column("grievance_id")] public Guid GrievanceId { get; set; } = Guid.NewGuid();
    [Column("complainant_user_id")] public Guid? ComplainantUserId { get; set; }
    [Column("is_anonymous")] public bool IsAnonymous { get; set; }
    [Required] public GrievanceCategory Category { get; set; }
    [Column("department_id")] public int DepartmentId { get; set; }
    [Required] public string Description { get; set; } = string.Empty;
    [Required] public GrievancePriority Priority { get; set; } = GrievancePriority.Medium;
    [Column("assigned_to_user_id")] public Guid? AssignedToUserId { get; set; }
    [Required] public GrievanceStatus Status { get; set; } = GrievanceStatus.Submitted;
    [Column("sla_due_at")] public DateTime? SlaDueAt { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("resolved_at")] public DateTime? ResolvedAt { get; set; }
    public User? ComplainantUser { get; set; }
    public User? AssignedToUser { get; set; }
    public Department Department { get; set; } = null!;
    public ICollection<GrievanceLog> Logs { get; set; } = new List<GrievanceLog>();
}

[Table("grievance_logs")]
public class GrievanceLog
{
    [Key, Column("log_id")] public Guid LogId { get; set; } = Guid.NewGuid();
    [Column("grievance_id")] public Guid GrievanceId { get; set; }
    [Column("updated_by_user_id")] public Guid UpdatedByUserId { get; set; }
    [Required, MaxLength(50), Column("status_changed_to")] public string StatusChangedTo { get; set; } = string.Empty;
    [Column("resolution_note")] public string? ResolutionNote { get; set; }
    [Column("timestamp")] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Grievance Grievance { get; set; } = null!;
    public User UpdatedByUser { get; set; } = null!;
}
