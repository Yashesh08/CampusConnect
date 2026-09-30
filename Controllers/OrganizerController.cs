using System.Text;
using CampusConnect.Models;
using CampusConnect.Models.Enums;
using CampusConnect.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.Controllers;

[Authorize(Roles = "Faculty,ClubCoordinator,EventOrganizer,Hod,Admin")]
public class OrganizerController : Controller
{
    private readonly IOpportunityRepository _opportunityRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly UserManager<User> _userManager;

    public OrganizerController(
        IOpportunityRepository opportunityRepository,
        IApplicationRepository applicationRepository,
        UserManager<User> userManager)
    {
        _opportunityRepository = opportunityRepository;
        _applicationRepository = applicationRepository;
        _userManager = userManager;
    }

    // GET: /Organizer/Dashboard
    public async Task<IActionResult> Dashboard()
    {
        List<Opportunity> opportunities;

        if (User.IsInRole("Admin"))
        {
            opportunities = await _opportunityRepository.GetAllAsync(status: null);
        }
        else
        {
            var user = await _userManager.GetUserAsync(User);
            Guid organizerId = user?.Id ?? Guid.Parse("11111111-1111-1111-1111-111111111111");
            opportunities = await _opportunityRepository.GetByOrganizerIdAsync(organizerId);
        }

        return View(opportunities);
    }

    // GET: /Organizer/Applicants/{opportunityId}
    public async Task<IActionResult> Applicants(Guid id)
    {
        var opportunity = await _opportunityRepository.GetByIdAsync(id);
        if (opportunity == null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        if (opportunity?.OrganizerId != user?.Id && !User.IsInRole("Admin") && !User.IsInRole("Hod")) return Forbid();
        var applications = await _applicationRepository.GetByOpportunityIdAsync(id);

        ViewBag.Opportunity = opportunity;
        return View(applications);
    }

    // POST: /Organizer/UpdateStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid applicationId, ApplicationStatus status, string? remarks)
    {
        var application = await _applicationRepository.GetByIdAsync(applicationId);
        if (application == null)
        {
            return NotFound();
        }

        var opportunity = await _opportunityRepository.GetByIdAsync(application.OpportunityId);
        var user = await _userManager.GetUserAsync(User);
        if (opportunity?.OrganizerId != user?.Id && !User.IsInRole("Admin") && !User.IsInRole("Hod")) return Forbid();
        await _applicationRepository.UpdateStatusAsync(applicationId, status, remarks);

        TempData["SuccessMessage"] = "Applicant status updated successfully!";
        return RedirectToAction(nameof(Applicants), new { id = application.OpportunityId });
    }

    // POST: /Organizer/BulkUpdateStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkUpdateStatus(Guid opportunityId, List<Guid> applicationIds, ApplicationStatus status, string? remarks)
    {
        if (applicationIds == null || !applicationIds.Any())
        {
            TempData["ErrorMessage"] = "No applicants selected for bulk update.";
            return RedirectToAction(nameof(Applicants), new { id = opportunityId });
        }

        var opportunity = await _opportunityRepository.GetByIdAsync(opportunityId);
        var user = await _userManager.GetUserAsync(User);
        if (opportunity?.OrganizerId != user?.Id && !User.IsInRole("Admin") && !User.IsInRole("Hod")) return Forbid();

        foreach (var appId in applicationIds)
        {
            await _applicationRepository.UpdateStatusAsync(appId, status, remarks);
        }

        TempData["SuccessMessage"] = $"Bulk updated {applicationIds.Count} applicant(s) to status '{status}'.";
        return RedirectToAction(nameof(Applicants), new { id = opportunityId });
    }

    // GET: /Organizer/Stats
    public async Task<IActionResult> Stats()
    {
        List<Opportunity> opportunities;

        if (User.IsInRole("Admin"))
        {
            opportunities = await _opportunityRepository.GetAllAsync(status: null);
        }
        else
        {
            var user = await _userManager.GetUserAsync(User);
            Guid organizerId = user?.Id ?? Guid.Parse("11111111-1111-1111-1111-111111111111");
            opportunities = await _opportunityRepository.GetByOrganizerIdAsync(organizerId);
        }

        var allApplications = new List<Application>();

        foreach (var opp in opportunities)
        {
            var apps = await _applicationRepository.GetByOpportunityIdAsync(opp.OpportunityId);
            allApplications.AddRange(apps);
        }

        var totalApps = allApplications.Count;
        var selectedApps = allApplications.Count(a => a.Status == ApplicationStatus.Selected);
        var shortlistedApps = allApplications.Count(a => a.Status == ApplicationStatus.Shortlisted);
        var rejectedApps = allApplications.Count(a => a.Status == ApplicationStatus.Rejected);

        var viewModel = new OrganizerStatsViewModel
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

        return View(viewModel);
    }

    // GET: /Organizer/ExportCsv/{opportunityId}
    public async Task<IActionResult> ExportCsv(Guid id)
    {
        var opportunity = await _opportunityRepository.GetByIdAsync(id);
        if (opportunity == null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        if (opportunity?.OrganizerId != user?.Id && !User.IsInRole("Admin") && !User.IsInRole("Hod")) return Forbid();
        var applications = await _applicationRepository.GetByOpportunityIdAsync(id);


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

        return File(fileBytes, "text/csv", fileName);
    }
}

public class OrganizerStatsViewModel
{
    public int TotalOpportunities { get; set; }
    public int ActiveOpportunities { get; set; }
    public int PendingOpportunities { get; set; }
    public int TotalApplications { get; set; }
    public int ShortlistedApplications { get; set; }
    public int SelectedApplications { get; set; }
    public int RejectedApplications { get; set; }
    public double AcceptanceRate { get; set; }
    public List<Opportunity> ClosingSoon { get; set; } = new();
    public Dictionary<OpportunityCategory, int> CategoryBreakdown { get; set; } = new();
}
