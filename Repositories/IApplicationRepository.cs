using CampusConnect.Models;
using CampusConnect.Models.Enums;

namespace CampusConnect.Repositories;

public interface IApplicationRepository
{
    Task<Application?> GetByIdAsync(Guid id);
    Task<List<Application>> GetByStudentIdAsync(Guid studentId);
    Task<List<Application>> GetByOpportunityIdAsync(Guid opportunityId);
    Task<bool> HasAlreadyAppliedAsync(Guid opportunityId, Guid studentId);
    Task AddAsync(Application application);
    Task UpdateStatusAsync(Guid applicationId, ApplicationStatus status, string? remarks = null);
    Task DeleteAsync(Guid id);
}
