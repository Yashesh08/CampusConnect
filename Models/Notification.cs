using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusConnect.Models;

[Table("notifications")]
public class Notification
{
    [Key, Column("notification_id")] public Guid NotificationId { get; set; } = Guid.NewGuid();
    [Column("user_id")] public Guid UserId { get; set; }
    [Required, MaxLength(250)] public string Title { get; set; } = string.Empty;
    [Required] public string Message { get; set; } = string.Empty;
    [Column("is_read")] public bool IsRead { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User User { get; set; } = null!;
}
