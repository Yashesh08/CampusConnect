using System.ComponentModel.DataAnnotations;
using CampusConnect.Models;

namespace CampusConnect.Models.ViewModels;

public class OfficeHoursIndexViewModel
{
    public List<FacultyOfficeHour> AvailableSlots { get; set; } = new();
    public List<FacultyOfficeHour> MyBookedSlots { get; set; } = new();
    public List<Department> Departments { get; set; } = new();
    public List<User> FacultyMembers { get; set; } = new();

    public int? SelectedDepartmentId { get; set; }
    public Guid? SelectedFacultyUserId { get; set; }

    public bool IsStudent { get; set; }
    public bool IsFaculty { get; set; }
}

public class ManageOfficeHoursViewModel
{
    public List<FacultyOfficeHour> UpcomingSlots { get; set; } = new();
    public List<FacultyOfficeHour> PastSlots { get; set; } = new();
    public CreateOfficeHourSlotViewModel NewSlot { get; set; } = new();
}

public class CreateOfficeHourSlotViewModel
{
    [Required]
    [DataType(DataType.Date)]
    public DateTime Date { get; set; } = DateTime.UtcNow.AddDays(1).Date;

    [Required]
    [DataType(DataType.Time)]
    public TimeSpan StartTime { get; set; } = new TimeSpan(10, 0, 0);

    [Required]
    [DataType(DataType.Time)]
    public TimeSpan EndTime { get; set; } = new TimeSpan(11, 0, 0);
}
