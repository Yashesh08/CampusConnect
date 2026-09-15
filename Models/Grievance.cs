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
