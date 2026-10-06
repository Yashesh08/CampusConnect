using CampusConnect.Data;
using CampusConnect.Models;
using CampusConnect.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Repositories;

public class ConnectionRepository : IConnectionRepository
{
    private readonly AppDbContext _context;

    public ConnectionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Connection>> GetConnectionsForUserAsync(Guid userId)
    {
        return await _context.Connections
            .Include(c => c.SenderUser)
                .ThenInclude(u => u.StudentProfile)
            .Include(c => c.SenderUser)
                .ThenInclude(u => u.FacultyProfile)
            .Include(c => c.ReceiverUser)
                .ThenInclude(u => u.StudentProfile)
            .Include(c => c.ReceiverUser)
                .ThenInclude(u => u.FacultyProfile)
            .Where(c => (c.SenderUserId == userId || c.ReceiverUserId == userId) && c.Status == ConnectionStatus.Accepted)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Connection>> GetPendingRequestsReceivedAsync(Guid userId)
    {
        return await _context.Connections
            .Include(c => c.SenderUser)
                .ThenInclude(u => u.StudentProfile)
                    .ThenInclude(sp => sp!.Department)
            .Include(c => c.SenderUser)
                .ThenInclude(u => u.FacultyProfile)
                    .ThenInclude(fp => fp!.Department)
            .Where(c => c.ReceiverUserId == userId && c.Status == ConnectionStatus.Pending)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Connection>> GetPendingRequestsSentAsync(Guid userId)
    {
        return await _context.Connections
            .Include(c => c.ReceiverUser)
                .ThenInclude(u => u.StudentProfile)
                    .ThenInclude(sp => sp!.Department)
            .Include(c => c.ReceiverUser)
                .ThenInclude(u => u.FacultyProfile)
                    .ThenInclude(fp => fp!.Department)
            .Where(c => c.SenderUserId == userId && c.Status == ConnectionStatus.Pending)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Connection?> GetConnectionAsync(Guid user1Id, Guid user2Id)
    {
        return await _context.Connections
            .Include(c => c.SenderUser)
            .Include(c => c.ReceiverUser)
            .FirstOrDefaultAsync(c =>
                (c.SenderUserId == user1Id && c.ReceiverUserId == user2Id) ||
                (c.SenderUserId == user2Id && c.ReceiverUserId == user1Id));
    }

    public async Task<Connection?> GetByIdAsync(Guid connectionId)
    {
        return await _context.Connections
            .Include(c => c.SenderUser)
            .Include(c => c.ReceiverUser)
            .FirstOrDefaultAsync(c => c.ConnectionId == connectionId);
    }

    public async Task<bool> SendRequestAsync(Guid senderId, Guid receiverId)
    {
        if (senderId == receiverId) return false;

        var existing = await GetConnectionAsync(senderId, receiverId);
        if (existing != null)
        {
            // If already pending or accepted, cannot re-send
            if (existing.Status == ConnectionStatus.Pending || existing.Status == ConnectionStatus.Accepted)
                return false;

            // If declined, update back to pending from the new sender
            existing.SenderUserId = senderId;
            existing.ReceiverUserId = receiverId;
            existing.Status = ConnectionStatus.Pending;
            existing.CreatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        var conn = new Connection
        {
            ConnectionId = Guid.NewGuid(),
            SenderUserId = senderId,
            ReceiverUserId = receiverId,
            Status = ConnectionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Connections.AddAsync(conn);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AcceptRequestAsync(Guid connectionId, Guid currentUserId)
    {
        var conn = await _context.Connections.FindAsync(connectionId);
        if (conn == null || conn.ReceiverUserId != currentUserId || conn.Status != ConnectionStatus.Pending)
            return false;

        conn.Status = ConnectionStatus.Accepted;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeclineRequestAsync(Guid connectionId, Guid currentUserId)
    {
        var conn = await _context.Connections.FindAsync(connectionId);
        if (conn == null || conn.ReceiverUserId != currentUserId || conn.Status != ConnectionStatus.Pending)
            return false;

        conn.Status = ConnectionStatus.Declined;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveConnectionAsync(Guid connectionId, Guid currentUserId)
    {
        var conn = await _context.Connections.FindAsync(connectionId);
        if (conn == null || (conn.SenderUserId != currentUserId && conn.ReceiverUserId != currentUserId))
            return false;

        _context.Connections.Remove(conn);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Dictionary<Guid, ConnectionStatus>> GetConnectionStatusesForUserAsync(Guid currentUserId, IEnumerable<Guid> targetUserIds)
    {
        var ids = targetUserIds.Distinct().ToList();
        var connections = await _context.Connections
            .Where(c => (c.SenderUserId == currentUserId && ids.Contains(c.ReceiverUserId)) ||
                        (c.ReceiverUserId == currentUserId && ids.Contains(c.SenderUserId)))
            .ToListAsync();

        var dict = new Dictionary<Guid, ConnectionStatus>();
        foreach (var c in connections)
        {
            var otherId = c.SenderUserId == currentUserId ? c.ReceiverUserId : c.SenderUserId;
            dict[otherId] = c.Status;
        }

        return dict;
    }
}
