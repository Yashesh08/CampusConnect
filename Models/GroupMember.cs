using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusConnect.Models;

[Table("group_members")]
public class GroupMember
{
    [Key, Column("membership_id")] public Guid MembershipId { get; set; } = Guid.NewGuid();
    [Column("group_id")] public Guid GroupId { get; set; }
    [Column("user_id")] public Guid UserId { get; set; }
    [Column("joined_at")] public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public CampusGroup Group { get; set; } = null!;
    public User User { get; set; } = null!;
}
