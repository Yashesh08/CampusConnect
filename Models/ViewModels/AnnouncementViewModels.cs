using System.ComponentModel.DataAnnotations;
using CampusConnect.Models;

namespace CampusConnect.Models.ViewModels;

public class AnnouncementFeedViewModel
{
    public List<Announcement> Announcements { get; set; } = new();
    public List<Department> Departments { get; set; } = new();
    public string? SelectedScope { get; set; } // "all", "college", "dept"
    public int? UserDepartmentId { get; set; }
    public bool CanPost { get; set; }
}

public class CreateAnnouncementViewModel
{
    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(250, ErrorMessage = "Title cannot exceed 250 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required.")]
    [MinLength(10, ErrorMessage = "Announcement content must be at least 10 characters.")]
    public string Content { get; set; } = string.Empty;

    public bool IsCollegeWide { get; set; } = true;

    public int? DepartmentId { get; set; }
}
