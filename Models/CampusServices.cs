using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusConnect.Models;

[Table("faculty_office_hours")]
public class FacultyOfficeHour
{
    [Key, Column("slot_id")] public Guid SlotId { get; set; } = Guid.NewGuid();
    [Column("faculty_user_id")] public Guid FacultyUserId { get; set; }
    [Column("start_time")] public DateTime StartTime { get; set; }
    [Column("end_time")] public DateTime EndTime { get; set; }
    [Column("is_booked")] public bool IsBooked { get; set; }
    [Column("booked_by_student_id")] public Guid? BookedByStudentId { get; set; }
    public User FacultyUser { get; set; } = null!;
    public StudentProfile? BookedByStudent { get; set; }
}

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
