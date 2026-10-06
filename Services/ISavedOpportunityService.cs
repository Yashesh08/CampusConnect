using CampusConnect.Models;

namespace CampusConnect.Services;

public interface ISavedOpportunityService
{
    Task<(bool Success, string Message)> SaveOpportunityAsync(Guid studentProfileId, Guid opportunityId);
    Task<(bool Success, string Message)> UnsaveOpportunityAsync(Guid studentProfileId, Guid opportunityId);
    Task<List<SavedOpportunity>> GetSavedOpportunitiesAsync(Guid studentProfileId);
    Task<bool> IsSavedAsync(Guid studentProfileId, Guid opportunityId);
}
