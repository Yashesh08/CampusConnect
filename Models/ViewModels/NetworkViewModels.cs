using CampusConnect.Models;

namespace CampusConnect.Models.ViewModels;

public class NetworkViewModel
{
    public List<Connection> AcceptedConnections { get; set; } = new();
    public List<Connection> ReceivedRequests { get; set; } = new();
    public List<Connection> SentRequests { get; set; } = new();

    public int TotalConnections => AcceptedConnections.Count;
    public int PendingReceivedCount => ReceivedRequests.Count;
    public int PendingSentCount => SentRequests.Count;
}
