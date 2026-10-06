using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly AppDbContext _context;

    public MessageRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Message>> GetConversationAsync(Guid user1Id, Guid user2Id)
    {
        return await _context.Messages
            .Include(m => m.SenderUser)
            .Include(m => m.ReceiverUser)
            .Where(m => (m.SenderUserId == user1Id && m.ReceiverUserId == user2Id) ||
                        (m.SenderUserId == user2Id && m.ReceiverUserId == user1Id))
            .OrderBy(m => m.SentAt)
            .ToListAsync();
    }

    public async Task<List<UserConversationSummary>> GetRecentConversationsForUserAsync(Guid userId)
    {
        // Get all messages where user is sender or receiver
        var messages = await _context.Messages
            .Include(m => m.SenderUser)
                .ThenInclude(u => u.StudentProfile)
            .Include(m => m.SenderUser)
                .ThenInclude(u => u.FacultyProfile)
            .Include(m => m.ReceiverUser)
                .ThenInclude(u => u.StudentProfile)
            .Include(m => m.ReceiverUser)
                .ThenInclude(u => u.FacultyProfile)
            .Where(m => m.SenderUserId == userId || m.ReceiverUserId == userId)
            .OrderByDescending(m => m.SentAt)
            .ToListAsync();

        var summaries = new List<UserConversationSummary>();
        var seenOtherUserIds = new HashSet<Guid>();

        foreach (var msg in messages)
        {
            var otherUser = msg.SenderUserId == userId ? msg.ReceiverUser : msg.SenderUser;
            if (otherUser == null || seenOtherUserIds.Contains(otherUser.Id))
                continue;

            seenOtherUserIds.Add(otherUser.Id);

            var unreadCount = messages.Count(m => m.SenderUserId == otherUser.Id && m.ReceiverUserId == userId && !m.IsRead);

            summaries.Add(new UserConversationSummary
            {
                OtherUser = otherUser,
                LastMessage = msg,
                UnreadCount = unreadCount
            });
        }

        return summaries;
    }

    public async Task<Message> SendMessageAsync(Guid senderId, Guid receiverId, string content)
    {
        var message = new Message
        {
            MessageId = Guid.NewGuid(),
            SenderUserId = senderId,
            ReceiverUserId = receiverId,
            Content = content.Trim(),
            IsRead = false,
            SentAt = DateTime.UtcNow
        };

        await _context.Messages.AddAsync(message);
        await _context.SaveChangesAsync();
        return message;
    }

    public async Task MarkAsReadAsync(Guid senderId, Guid currentUserId)
    {
        var unread = await _context.Messages
            .Where(m => m.SenderUserId == senderId && m.ReceiverUserId == currentUserId && !m.IsRead)
            .ToListAsync();

        if (unread.Any())
        {
            foreach (var m in unread)
            {
                m.IsRead = true;
            }
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetTotalUnreadCountAsync(Guid userId)
    {
        return await _context.Messages
            .CountAsync(m => m.ReceiverUserId == userId && !m.IsRead);
    }
}
