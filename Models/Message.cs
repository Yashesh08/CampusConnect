using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusConnect.Models;

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
