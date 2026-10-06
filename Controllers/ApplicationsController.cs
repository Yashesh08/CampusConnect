using CampusConnect.Models;
using CampusConnect.Models.Enums;
using CampusConnect.Repositories;
using CampusConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.Controllers;

[Authorize(Roles = "Student")]
public class ApplicationsController : Controller
{
    private readonly IApplicationService _applicationService;
    private readonly IStudentProfileRepository _studentProfileRepository;
    private readonly UserManager<User> _userManager;

    public ApplicationsController(
        IApplicationService applicationService,
        IStudentProfileRepository studentProfileRepository,
        UserManager<User> userManager)
    {
        _applicationService = applicationService;
        _studentProfileRepository = studentProfileRepository;
        _userManager = userManager;
    }

    // POST: /Applications/Apply
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(Guid opportunityId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var studentProfile = await _studentProfileRepository.GetByUserIdAsync(user.Id);
        if (studentProfile == null)
        {
            TempData["ErrorMessage"] = "You must create a student profile before applying.";
            return RedirectToAction("Create", "StudentProfiles");
        }

        var (success, message, _) = await _applicationService.ApplyAsync(opportunityId, studentProfile.ProfileId);
        if (!success)
        {
            TempData["ErrorMessage"] = message;
            return RedirectToAction("Details", "Opportunities", new { id = opportunityId });
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(MyApplications));
    }

    // GET: /Applications/MyApplications
    public async Task<IActionResult> MyApplications(ApplicationStatus? status)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var studentProfile = await _studentProfileRepository.GetByUserIdAsync(user.Id);
        if (studentProfile == null)
        {
            ViewBag.CurrentStatusFilter = status;
            return View(new List<Application>());
        }

        var applications = await _applicationService.GetStudentApplicationsAsync(studentProfile.ProfileId, status);
        ViewBag.CurrentStatusFilter = status;
        return View(applications);
    }

    // POST: /Applications/Withdraw/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var studentProfile = await _studentProfileRepository.GetByUserIdAsync(user.Id);
        if (studentProfile == null) return Forbid();

        var (success, message) = await _applicationService.WithdrawAsync(id, studentProfile.ProfileId);
        if (!success)
        {
            if (message.StartsWith("Forbidden")) return Forbid();
            TempData["ErrorMessage"] = message;
        }
        else
        {
            TempData["SuccessMessage"] = message;
        }

        return RedirectToAction(nameof(MyApplications));
    }
}
