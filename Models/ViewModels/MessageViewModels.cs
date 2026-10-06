using CampusConnect.Models;
using CampusConnect.Repositories;

namespace CampusConnect.Models.ViewModels;

public class MessagesIndexViewModel
{
    public List<UserConversationSummary> Conversations { get; set; } = new();
    public User? ActiveChatUser { get; set; }
    public List<Message> ActiveChatMessages { get; set; } = new();
    public int TotalUnreadCount => Conversations.Sum(c => c.UnreadCount);
}
