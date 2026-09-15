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
