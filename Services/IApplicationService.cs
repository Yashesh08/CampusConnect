using CampusConnect.Models;
using CampusConnect.Models.Enums;

namespace CampusConnect.Services;

public interface IApplicationService
{
    Task<(bool Success, string Message, Guid? ApplicationId)> ApplyAsync(Guid opportunityId, Guid studentProfileId);
    Task<List<Application>> GetStudentApplicationsAsync(Guid studentProfileId, ApplicationStatus? status = null);
    Task<(bool Success, string Message)> WithdrawAsync(Guid applicationId, Guid studentProfileId);
    bool IsValidStatusTransition(ApplicationStatus currentStatus, ApplicationStatus newStatus);
}
