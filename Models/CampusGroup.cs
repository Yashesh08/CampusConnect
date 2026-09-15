using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusConnect.Models;

[Table("groups")]
public class CampusGroup
{
    [Key, Column("group_id")] public Guid GroupId { get; set; } = Guid.NewGuid();
    [Required, MaxLength(150), Column("group_name")] public string GroupName { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Column("created_by_user_id")] public Guid CreatedByUserId { get; set; }
    [Column("is_private")] public bool IsPrivate { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
}
