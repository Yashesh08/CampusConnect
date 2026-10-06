using CampusConnect.Controllers;
using CampusConnect.Models;
using CampusConnect.Models.Enums;
using CampusConnect.Models.ViewModels;

namespace CampusConnect.Services;

public interface IOrganizerService
{
    Task<List<Opportunity>> GetOrganizerDashboardOpportunitiesAsync(Guid userId, bool isAdmin);
    Task<(bool Allowed, Opportunity? Opportunity, List<Application> Applications)> GetOpportunityApplicantsAsync(Guid opportunityId, Guid userId, bool isPrivileged);
    Task<(bool Success, string Message)> UpdateApplicantStatusAsync(Guid applicationId, ApplicationStatus newStatus, string? remarks, Guid userId, bool isPrivileged);
    Task<(bool Success, string Message)> BulkUpdateApplicantStatusAsync(Guid opportunityId, List<Guid> applicationIds, ApplicationStatus newStatus, string? remarks, Guid userId, bool isPrivileged);
    Task<OrganizerStatsViewModel> GetOrganizerStatsAsync(Guid userId, bool isAdmin);
    Task<(bool Allowed, string? FileName, byte[]? FileBytes)> ExportCsvAsync(Guid opportunityId, Guid userId, bool isPrivileged);
}
