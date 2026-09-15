using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CampusConnect.Models.Enums;

namespace CampusConnect.Models;

[Table("users")]
public class User
{
    [Key, Column("user_id")]
    public Guid UserId { get; set; } = Guid.NewGuid();

    [Required, EmailAddress, MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(512), Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }

    [Required]
    public UserStatus Status { get; set; } = UserStatus.PendingVerification;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public StudentProfile? StudentProfile { get; set; }
    public FacultyProfile? FacultyProfile { get; set; }
}
