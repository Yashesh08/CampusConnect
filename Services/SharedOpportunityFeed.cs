using CampusConnect.Models;
using CampusConnect.Repositories;

namespace CampusConnect.Services;

public class SharedOpportunityFeed : ISharedOpportunityFeed
{
    private readonly IOpportunityRepository _opportunityRepo;

    public SharedOpportunityFeed(IOpportunityRepository opportunityRepo)
    {
        _opportunityRepo = opportunityRepo;
    }

    public async Task<List<Opportunity>> GetUpcomingOpportunitiesAsync()
    {
        // Get all approved opportunities
        return await _opportunityRepo.GetAllAsync(status: CampusConnect.Models.Enums.ApprovalStatus.Approved);
    }
}
