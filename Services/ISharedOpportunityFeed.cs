using CampusConnect.Models;

namespace CampusConnect.Services;

/// <summary>
/// Read-only interface for Week 6 integration.
/// Person 2 will provide the real implementation of this feed.
/// For now, Person 1 uses a dummy implementation.
/// </summary>
public interface ISharedOpportunityFeed
{
    Task<List<Opportunity>> GetUpcomingOpportunitiesAsync();
}
