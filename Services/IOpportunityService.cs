using CampusConnect.Models;
using CampusConnect.Models.Enums;
using CampusConnect.Models.ViewModels;

namespace CampusConnect.Services;

public interface IOpportunityService
{
    Task<OpportunityFilterViewModel> GetFilteredOpportunitiesAsync(
        string? search,
        OpportunityCategory? category,
        WorkMode? mode,
        int? departmentId,
        string? sortBy,
        int page,
        int pageSize,
        Guid? studentId = null);

    Task<Opportunity?> GetOpportunityByIdAsync(Guid id);
    Task<Opportunity> CreateOpportunityAsync(CreateOpportunityViewModel model, Guid organizerId);
    Task<EditOpportunityViewModel?> GetEditViewModelAsync(Guid opportunityId);
    Task<bool> UpdateOpportunityAsync(Guid opportunityId, EditOpportunityViewModel model, Guid userId, bool isPrivileged);
    Task<bool> DeleteOpportunityAsync(Guid opportunityId, Guid userId, bool isPrivileged);
    Task<(bool Success, string Message)> ApproveOpportunityAsync(Guid opportunityId, Guid userId, bool isAdmin, bool isHod, int? userDepartmentId);
    Task<(bool Success, string Message)> RejectOpportunityAsync(Guid opportunityId, Guid userId, bool isAdmin, bool isHod, int? userDepartmentId);
    Task<List<Opportunity>> GetPendingApprovalsAsync();
}
