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
