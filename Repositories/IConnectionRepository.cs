using CampusConnect.Models;
using CampusConnect.Models.Enums;

namespace CampusConnect.Repositories;

public interface IConnectionRepository
{
    Task<List<Connection>> GetConnectionsForUserAsync(Guid userId);
    Task<List<Connection>> GetPendingRequestsReceivedAsync(Guid userId);
    Task<List<Connection>> GetPendingRequestsSentAsync(Guid userId);
    Task<Connection?> GetConnectionAsync(Guid user1Id, Guid user2Id);
    Task<Connection?> GetByIdAsync(Guid connectionId);
    Task<bool> SendRequestAsync(Guid senderId, Guid receiverId);
    Task<bool> AcceptRequestAsync(Guid connectionId, Guid currentUserId);
    Task<bool> DeclineRequestAsync(Guid connectionId, Guid currentUserId);
    Task<bool> RemoveConnectionAsync(Guid connectionId, Guid currentUserId);
    Task<Dictionary<Guid, ConnectionStatus>> GetConnectionStatusesForUserAsync(Guid currentUserId, IEnumerable<Guid> targetUserIds);
}
