using CampusConnect.Models;
using CampusConnect.Models.Enums;
using CampusConnect.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;

namespace CampusConnect.Controllers;

[Authorize(Roles = "Student")]
public class ApplicationsController : Controller
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IOpportunityRepository _opportunityRepository;
    private readonly IStudentProfileRepository _studentProfileRepository;
    private readonly UserManager<User> _userManager;

    public ApplicationsController(
        IApplicationRepository applicationRepository,
        IOpportunityRepository opportunityRepository,
        IStudentProfileRepository studentProfileRepository,
        UserManager<User> userManager)
    {
        _applicationRepository = applicationRepository;
        _opportunityRepository = opportunityRepository;
        _studentProfileRepository = studentProfileRepository;
        _userManager = userManager;
    }

    // POST: /Applications/Apply
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(Guid opportunityId)
    {
        var opportunity = await _opportunityRepository.GetByIdAsync(opportunityId);
        if (opportunity == null)
        {
            return NotFound("Opportunity not found.");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var studentProfile = await _studentProfileRepository.GetByUserIdAsync(user.Id);

        // SECURITY FIX (V002): Do NOT fall back to another student's profile
        if (studentProfile == null)
        {
            TempData["ErrorMessage"] = "You must create a student profile before applying.";
            return RedirectToAction("Create", "StudentProfiles");
        }

        // Check if deadline has passed
        if (opportunity.RegistrationDeadline < DateTime.UtcNow)
        {
            TempData["ErrorMessage"] = "Registration deadline for this opportunity has already passed.";
            return RedirectToAction("Details", "Opportunities", new { id = opportunityId });
        }

        // Duplicate check
        bool alreadyApplied = await _applicationRepository.HasAlreadyAppliedAsync(opportunityId, studentProfile.ProfileId);
        if (alreadyApplied)
        {
            TempData["ErrorMessage"] = "You have already applied for this opportunity.";
            return RedirectToAction("Details", "Opportunities", new { id = opportunityId });
        }

        var application = new Application
        {
            ApplicationId = Guid.NewGuid(),
            OpportunityId = opportunityId,
            StudentId = studentProfile.ProfileId,
            Status = ApplicationStatus.Applied,
            AppliedAt = DateTime.UtcNow
        };

        await _applicationRepository.AddAsync(application);

        TempData["SuccessMessage"] = "Application submitted successfully!";
        return RedirectToAction(nameof(MyApplications));
    }

    // GET: /Applications/MyApplications
    public async Task<IActionResult> MyApplications(ApplicationStatus? status)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var studentProfile = await _studentProfileRepository.GetByUserIdAsync(user.Id);

        // SECURITY FIX (V003): Do NOT fall back to another student's profile
        if (studentProfile == null)
        {
            ViewBag.CurrentStatusFilter = status;
            return View(new List<Application>());
        }

        var applications = await _applicationRepository.GetByStudentIdAsync(studentProfile.ProfileId);
        if (status.HasValue)
        {
            applications = applications.Where(a => a.Status == status.Value).ToList();
        }

        ViewBag.CurrentStatusFilter = status;
        return View(applications);
    }

    // POST: /Applications/Withdraw/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(Guid id)
    {
        
        var user = await _userManager.GetUserAsync(User);
        var studentProfile = user != null ? await _studentProfileRepository.GetByUserIdAsync(user.Id) : null;
        if (studentProfile == null) return Forbid();
        var application = await _applicationRepository.GetByIdAsync(id);
        if (application == null) return NotFound();
        if (application.StudentId != studentProfile.ProfileId) return Forbid();
        await _applicationRepository.DeleteAsync(id);

        TempData["SuccessMessage"] = "Application withdrawn successfully.";
        return RedirectToAction(nameof(MyApplications));
    }
}
