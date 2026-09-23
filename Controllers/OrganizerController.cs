using System.Text;
using CampusConnect.Models;
using CampusConnect.Models.Enums;
using CampusConnect.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.Controllers;

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
        var user = await _userManager.GetUserAsync(User);
        Guid organizerId;

        if (user != null)
        {
            organizerId = user.Id;
        }
        else
        {
            var defaultOrganizer = await _userManager.FindByEmailAsync("organizer@campusconnect.edu");
            organizerId = defaultOrganizer?.Id ?? Guid.Parse("11111111-1111-1111-1111-111111111111");
        }

        var opportunities = await _opportunityRepository.GetByOrganizerIdAsync(organizerId);
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

        await _applicationRepository.UpdateStatusAsync(applicationId, status, remarks);
        TempData["SuccessMessage"] = "Applicant status updated successfully!";
        return RedirectToAction(nameof(Applicants), new { id = application.OpportunityId });
    }

    // GET: /Organizer/ExportCsv/{opportunityId}
    public async Task<IActionResult> ExportCsv(Guid id)
    {
        var opportunity = await _opportunityRepository.GetByIdAsync(id);
        if (opportunity == null)
        {
            return NotFound();
        }

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
