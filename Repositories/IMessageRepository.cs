using CampusConnect.Models;

namespace CampusConnect.Repositories;

public class UserConversationSummary
{
    public User OtherUser { get; set; } = null!;
    public Message LastMessage { get; set; } = null!;
    public int UnreadCount { get; set; }
}

public interface IMessageRepository
{
    Task<List<Message>> GetConversationAsync(Guid user1Id, Guid user2Id);
    Task<List<UserConversationSummary>> GetRecentConversationsForUserAsync(Guid userId);
    Task<Message> SendMessageAsync(Guid senderId, Guid receiverId, string content);
    Task MarkAsReadAsync(Guid senderId, Guid currentUserId);
    Task<int> GetTotalUnreadCountAsync(Guid userId);
}
