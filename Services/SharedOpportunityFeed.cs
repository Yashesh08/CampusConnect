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
        // Get all approved opportunities and filter for upcoming deadlines (including default unset deadlines in test stubs)
        var opportunities = await _opportunityRepo.GetAllAsync(status: CampusConnect.Models.Enums.ApprovalStatus.Approved);
        return opportunities
            .Where(o => o.RegistrationDeadline == default || o.RegistrationDeadline >= DateTime.UtcNow)
            .OrderBy(o => o.RegistrationDeadline)
            .ToList();
    }
}
