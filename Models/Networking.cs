using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CampusConnect.Models.Enums;

namespace CampusConnect.Models;

[Table("connections")]
public class Connection
{
    [Key, Column("connection_id")] public Guid ConnectionId { get; set; } = Guid.NewGuid();
    [Column("sender_user_id")] public Guid SenderUserId { get; set; }
    [Column("receiver_user_id")] public Guid ReceiverUserId { get; set; }
    [Required] public ConnectionStatus Status { get; set; } = ConnectionStatus.Pending;
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User SenderUser { get; set; } = null!;
    public User ReceiverUser { get; set; } = null!;
}

[Table("messages")]
public class Message
{
    [Key, Column("message_id")] public Guid MessageId { get; set; } = Guid.NewGuid();
    [Column("sender_user_id")] public Guid SenderUserId { get; set; }
    [Column("receiver_user_id")] public Guid ReceiverUserId { get; set; }
    [Required] public string Content { get; set; } = string.Empty;
    [Column("is_read")] public bool IsRead { get; set; }
    [Column("sent_at")] public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public User SenderUser { get; set; } = null!;
    public User ReceiverUser { get; set; } = null!;
}

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
