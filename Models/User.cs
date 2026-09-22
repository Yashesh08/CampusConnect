using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CampusConnect.Models.Enums;

using Microsoft.AspNetCore.Identity;

namespace CampusConnect.Models;

[Table("users")]
public class User : IdentityUser<Guid>
{

    [Required]
    public UserRole Role { get; set; }

    [Required]
    public UserStatus Status { get; set; } = UserStatus.PendingVerification;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public StudentProfile? StudentProfile { get; set; }
    public FacultyProfile? FacultyProfile { get; set; }
}
