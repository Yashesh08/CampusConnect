using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusConnect.Models;

[Table("announcements")]
public class Announcement
{
    [Key, Column("announcement_id")] public Guid AnnouncementId { get; set; } = Guid.NewGuid();
    [Column("author_user_id")] public Guid AuthorUserId { get; set; }
    [Column("department_id")] public int? DepartmentId { get; set; }
    [Required, MaxLength(250)] public string Title { get; set; } = string.Empty;
    [Required] public string Content { get; set; } = string.Empty;
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User AuthorUser { get; set; } = null!;
    public Department? Department { get; set; }
}
