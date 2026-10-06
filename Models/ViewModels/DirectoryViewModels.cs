using CampusConnect.Models;
using CampusConnect.Models.Enums;

namespace CampusConnect.Models.ViewModels;

public enum DirectoryConnectionState
{
    IsSelf,
    NotConnected,
    PendingSent,
    PendingReceived,
    Connected
}

public class DirectoryMemberCard
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? DepartmentName { get; set; }
    public int? DepartmentId { get; set; }
    
    // Student specifics
    public string? RollNumber { get; set; }
    public int? BatchYear { get; set; }
    public string? Bio { get; set; }
    public List<string> Skills { get; set; } = new();
    
    // Faculty specifics
    public string? Designation { get; set; }
    public string? CabinNumber { get; set; }
    public int AvailableOfficeHoursCount { get; set; }

    // Relationship with logged-in user
    public DirectoryConnectionState ConnectionState { get; set; } = DirectoryConnectionState.NotConnected;
    public Guid? ConnectionId { get; set; }
}

public class DirectoryIndexViewModel
{
    public List<DirectoryMemberCard> Members { get; set; } = new();
    public List<Department> Departments { get; set; } = new();
    public List<string> AvailableSkills { get; set; } = new();

    public string? SearchQuery { get; set; }
    public string? SelectedRole { get; set; }
    public int? SelectedDepartmentId { get; set; }
    public string? SelectedSkill { get; set; }

    public int TotalMembers => Members.Count;
    public int StudentCount => Members.Count(m => m.Role == UserRole.Student);
    public int FacultyCount => Members.Count(m => m.Role == UserRole.Faculty || m.Role == UserRole.Hod);
}

public class UserPublicProfileViewModel
{
    public User User { get; set; } = null!;
    public StudentProfile? StudentProfile { get; set; }
    public FacultyProfile? FacultyProfile { get; set; }
    public List<FacultyOfficeHour> UpcomingOfficeHours { get; set; } = new();
    public DirectoryConnectionState ConnectionState { get; set; } = DirectoryConnectionState.NotConnected;
    public Guid? ConnectionId { get; set; }
}
