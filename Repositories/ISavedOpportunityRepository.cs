using CampusConnect.Models;

namespace CampusConnect.Repositories;

public interface ISavedOpportunityRepository
{
    Task<SavedOpportunity?> GetAsync(Guid studentId, Guid opportunityId);
    Task<bool> IsSavedAsync(Guid studentId, Guid opportunityId);
    Task AddAsync(SavedOpportunity savedOpportunity);
    Task RemoveAsync(Guid studentId, Guid opportunityId);
    Task<List<SavedOpportunity>> GetByStudentIdAsync(Guid studentId);
}
