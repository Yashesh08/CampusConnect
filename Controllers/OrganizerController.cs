using CampusConnect.Models;
using CampusConnect.Models.Enums;
using CampusConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.Controllers;

[Authorize(Roles = "Faculty,ClubCoordinator,EventOrganizer,Hod,Admin")]
public class OrganizerController : Controller
{
    private readonly IOrganizerService _organizerService;
    private readonly UserManager<User> _userManager;

    public OrganizerController(
        IOrganizerService organizerService,
        UserManager<User> userManager)
    {
        _organizerService = organizerService;
        _userManager = userManager;
    }

    // GET: /Organizer/Dashboard
    public async Task<IActionResult> Dashboard()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        bool isAdmin = User.IsInRole("Admin");
        var opportunities = await _organizerService.GetOrganizerDashboardOpportunitiesAsync(user.Id, isAdmin);

        return View(opportunities);
    }

    // GET: /Organizer/Applicants/{opportunityId}
    public async Task<IActionResult> Applicants(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        bool isPrivileged = User.IsInRole("Admin") || User.IsInRole("Hod");
        var (allowed, opportunity, applications) = await _organizerService.GetOpportunityApplicantsAsync(id, user.Id, isPrivileged);

        if (!allowed) return Forbid();
        if (opportunity == null) return NotFound();

        ViewBag.Opportunity = opportunity;
        return View(applications);
    }

    // POST: /Organizer/UpdateStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid applicationId, ApplicationStatus status, string? remarks)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        bool isPrivileged = User.IsInRole("Admin") || User.IsInRole("Hod");
        var (success, message) = await _organizerService.UpdateApplicantStatusAsync(applicationId, status, remarks, user.Id, isPrivileged);

        if (!success)
        {
            if (message.StartsWith("Forbidden")) return Forbid();
            TempData["ErrorMessage"] = message;
        }
        else
        {
            TempData["SuccessMessage"] = message;
        }

        return RedirectToAction(nameof(Dashboard));
    }

    // POST: /Organizer/BulkUpdateStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkUpdateStatus(Guid opportunityId, List<Guid> applicationIds, ApplicationStatus status, string? remarks)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        bool isPrivileged = User.IsInRole("Admin") || User.IsInRole("Hod");
        var (success, message) = await _organizerService.BulkUpdateApplicantStatusAsync(opportunityId, applicationIds, status, remarks, user.Id, isPrivileged);

        if (!success)
        {
            if (message.StartsWith("Forbidden")) return Forbid();
            TempData["ErrorMessage"] = message;
        }
        else
        {
            TempData["SuccessMessage"] = message;
        }

        return RedirectToAction(nameof(Applicants), new { id = opportunityId });
    }

    // GET: /Organizer/Stats
    public async Task<IActionResult> Stats()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        bool isAdmin = User.IsInRole("Admin");
        var viewModel = await _organizerService.GetOrganizerStatsAsync(user.Id, isAdmin);

        return View(viewModel);
    }

    // GET: /Organizer/ExportCsv/{opportunityId}
    public async Task<IActionResult> ExportCsv(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        bool isPrivileged = User.IsInRole("Admin") || User.IsInRole("Hod");
        var (allowed, fileName, fileBytes) = await _organizerService.ExportCsvAsync(id, user.Id, isPrivileged);

        if (!allowed) return Forbid();
        if (fileBytes == null || fileName == null) return NotFound();

        return File(fileBytes, "text/csv", fileName);
    }
}
