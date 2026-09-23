<<<<<<< HEAD
namespace CampusConnect.Repositories;

public interface IOpportunityRepository { }
=======
using CampusConnect.Models;
using CampusConnect.Models.Enums;

namespace CampusConnect.Repositories;

public interface IOpportunityRepository
{
    Task<Opportunity?> GetByIdAsync(Guid id);
    Task<List<Opportunity>> GetAllAsync(OpportunityCategory? category = null, WorkMode? mode = null, ApprovalStatus? status = ApprovalStatus.Approved, string? search = null);
    Task<List<Opportunity>> GetByOrganizerIdAsync(Guid organizerId);
    Task<List<Opportunity>> GetPendingApprovalsAsync();
    Task AddAsync(Opportunity opportunity, List<int>? requiredSkillIds = null);
    Task UpdateAsync(Opportunity opportunity, List<int>? requiredSkillIds = null);
    Task UpdateStatusAsync(Guid opportunityId, ApprovalStatus status);
    Task DeleteAsync(Guid id);
}
>>>>>>> 147642b (feat(person-2): complete implementation of Person 2 scope (Weeks 1, 2 & 3))
