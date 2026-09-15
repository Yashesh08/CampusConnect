using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusConnect.Models;

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
