using System.Text;
using CampusConnect.Controllers;
using CampusConnect.Models;
using CampusConnect.Models.Enums;
using CampusConnect.Models.ViewModels;
using CampusConnect.Repositories;

namespace CampusConnect.Services;

public class OrganizerService : IOrganizerService
{
    private readonly IOpportunityRepository _opportunityRepo;
    private readonly IApplicationRepository _applicationRepo;
    private readonly IApplicationService _applicationService;

    public OrganizerService(
        IOpportunityRepository opportunityRepo,
        IApplicationRepository applicationRepo,
        IApplicationService applicationService)
    {
        _opportunityRepo = opportunityRepo;
        _applicationRepo = applicationRepo;
        _applicationService = applicationService;
    }

    public async Task<List<Opportunity>> GetOrganizerDashboardOpportunitiesAsync(Guid userId, bool isAdmin)
    {
        if (isAdmin)
        {
            return await _opportunityRepo.GetAllAsync(status: null);
        }
        return await _opportunityRepo.GetByOrganizerIdAsync(userId);
    }

    public async Task<(bool Allowed, Opportunity? Opportunity, List<Application> Applications)> GetOpportunityApplicantsAsync(Guid opportunityId, Guid userId, bool isPrivileged)
    {
        var opportunity = await _opportunityRepo.GetByIdAsync(opportunityId);
        if (opportunity == null)
        {
            return (true, null, new List<Application>());
        }

        if (opportunity.OrganizerId != userId && !isPrivileged)
        {
            return (false, opportunity, new List<Application>());
        }

        var apps = await _applicationRepo.GetByOpportunityIdAsync(opportunityId);
        return (true, opportunity, apps);
    }

    public async Task<(bool Success, string Message)> UpdateApplicantStatusAsync(Guid applicationId, ApplicationStatus newStatus, string? remarks, Guid userId, bool isPrivileged)
    {
        var application = await _applicationRepo.GetByIdAsync(applicationId);
        if (application == null)
        {
            return (false, "Application not found.");
        }

        var opportunity = await _opportunityRepo.GetByIdAsync(application.OpportunityId);
        if (opportunity == null)
        {
            return (false, "Opportunity not found.");
        }

        if (opportunity.OrganizerId != userId && !isPrivileged)
        {
            return (false, "Forbidden: You are not authorized to manage applicants for this opportunity.");
        }

        // State machine status transition check (Priority 4)
        if (!_applicationService.IsValidStatusTransition(application.Status, newStatus))
        {
            return (false, $"Invalid status transition from {application.Status} to {newStatus}.");
        }

        await _applicationRepo.UpdateStatusAsync(applicationId, newStatus, remarks);
        return (true, "Applicant status updated successfully!");
    }

    public async Task<(bool Success, string Message)> BulkUpdateApplicantStatusAsync(Guid opportunityId, List<Guid> applicationIds, ApplicationStatus newStatus, string? remarks, Guid userId, bool isPrivileged)
    {
        if (applicationIds == null || !applicationIds.Any())
        {
            return (false, "No applicants selected for bulk update.");
        }

        var opportunity = await _opportunityRepo.GetByIdAsync(opportunityId);
        if (opportunity == null)
        {
            return (false, "Opportunity not found.");
        }

        if (opportunity.OrganizerId != userId && !isPrivileged)
        {
            return (false, "Forbidden: You are not authorized to manage applicants for this opportunity.");
        }

        int updatedCount = 0;
        foreach (var appId in applicationIds)
        {
            var app = await _applicationRepo.GetByIdAsync(appId);
            if (app != null && _applicationService.IsValidStatusTransition(app.Status, newStatus))
            {
                await _applicationRepo.UpdateStatusAsync(appId, newStatus, remarks);
                updatedCount++;
            }
        }

        return (true, $"Bulk updated {updatedCount} applicant(s) to status '{newStatus}'.");
    }

    public async Task<OrganizerStatsViewModel> GetOrganizerStatsAsync(Guid userId, bool isAdmin)
    {
        var opportunities = await GetOrganizerDashboardOpportunitiesAsync(userId, isAdmin);

        var allApplications = new List<Application>();
        foreach (var opp in opportunities)
        {
            var apps = await _applicationRepo.GetByOpportunityIdAsync(opp.OpportunityId);
            allApplications.AddRange(apps);
        }

        var totalApps = allApplications.Count;
        var selectedApps = allApplications.Count(a => a.Status == ApplicationStatus.Selected);
        var shortlistedApps = allApplications.Count(a => a.Status == ApplicationStatus.Shortlisted);
        var rejectedApps = allApplications.Count(a => a.Status == ApplicationStatus.Rejected);

        return new OrganizerStatsViewModel
        {
            TotalOpportunities = opportunities.Count,
            ActiveOpportunities = opportunities.Count(o => o.ApprovalStatus == ApprovalStatus.Approved),
            PendingOpportunities = opportunities.Count(o => o.ApprovalStatus == ApprovalStatus.PendingReview),
            TotalApplications = totalApps,
            ShortlistedApplications = shortlistedApps,
            SelectedApplications = selectedApps,
            RejectedApplications = rejectedApps,
            AcceptanceRate = totalApps > 0 ? Math.Round((double)selectedApps / totalApps * 100, 1) : 0,
            ClosingSoon = opportunities
                .Where(o => o.RegistrationDeadline >= DateTime.UtcNow && o.RegistrationDeadline <= DateTime.UtcNow.AddDays(7))
                .OrderBy(o => o.RegistrationDeadline)
                .ToList(),
            CategoryBreakdown = opportunities
                .GroupBy(o => o.Category)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async Task<(bool Allowed, string? FileName, byte[]? FileBytes)> ExportCsvAsync(Guid opportunityId, Guid userId, bool isPrivileged)
    {
        var (allowed, opportunity, applications) = await GetOpportunityApplicantsAsync(opportunityId, userId, isPrivileged);
        if (!allowed)
        {
            return (false, null, null);
        }
        if (opportunity == null)
        {
            return (true, null, null);
        }

        var builder = new StringBuilder();
        builder.AppendLine("ApplicationId,StudentEmail,BatchYear,Status,AppliedAt,OrganizerRemarks");

        foreach (var app in applications)
        {
            var studentName = app.Student?.User?.Email ?? app.Student?.RollNumber ?? "Student";
            var batch = app.Student?.BatchYear.ToString() ?? "N/A";
            var status = app.Status.ToString();
            var appliedAt = app.AppliedAt.ToString("yyyy-MM-dd HH:mm:ss");
            var remarks = app.OrganizerRemarks?.Replace(",", " ") ?? "";

            builder.AppendLine($"{app.ApplicationId},\"{studentName}\",\"{batch}\",\"{status}\",\"{appliedAt}\",\"{remarks}\"");
        }

        var fileBytes = Encoding.UTF8.GetBytes(builder.ToString());
        var fileName = $"Applicants_{opportunity.Title.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.csv";

        return (true, fileName, fileBytes);
    }
}
