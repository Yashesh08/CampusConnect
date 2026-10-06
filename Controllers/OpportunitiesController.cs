using CampusConnect.Models;
using CampusConnect.Models.Enums;
using CampusConnect.Models.ViewModels;
using CampusConnect.Repositories;
using CampusConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.Controllers;

[Authorize]
public class OpportunitiesController : Controller
{
    private readonly IOpportunityService _opportunityService;
    private readonly ISavedOpportunityService _savedOpportunityService;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly IStudentProfileRepository _studentProfileRepository;
    private readonly IFacultyProfileRepository _facultyProfileRepository;
    private readonly IOpportunityCalendarFeed _calendarFeed;
    private readonly UserManager<User> _userManager;

    public OpportunitiesController(
        IOpportunityService opportunityService,
        ISavedOpportunityService savedOpportunityService,
        IApplicationRepository applicationRepository,
        IDepartmentRepository departmentRepository,
        ISkillRepository skillRepository,
        IStudentProfileRepository studentProfileRepository,
        IFacultyProfileRepository facultyProfileRepository,
        IOpportunityCalendarFeed calendarFeed,
        UserManager<User> userManager)
    {
        _opportunityService = opportunityService;
        _savedOpportunityService = savedOpportunityService;
        _applicationRepository = applicationRepository;
        _departmentRepository = departmentRepository;
        _skillRepository = skillRepository;
        _studentProfileRepository = studentProfileRepository;
        _facultyProfileRepository = facultyProfileRepository;
        _calendarFeed = calendarFeed;
        _userManager = userManager;
    }

    // GET: /Opportunities (Discovery / Search / Filter / Sort / Pagination)
    public async Task<IActionResult> Index(
        string? search,
        OpportunityCategory? category,
        WorkMode? mode,
        int? departmentId,
        string? sortBy,
        int page = 1,
        int pageSize = 6)
    {
        var user = await _userManager.GetUserAsync(User);
        Guid? studentId = null;
        if (user != null && User.IsInRole("Student"))
        {
            var profile = await _studentProfileRepository.GetByUserIdAsync(user.Id);
            if (profile != null) studentId = profile.ProfileId;
        }

        var viewModel = await _opportunityService.GetFilteredOpportunitiesAsync(
            search, category, mode, departmentId, sortBy, page, pageSize, studentId);

        return View(viewModel);
    }

    // GET: /Opportunities/Details/{id}
    public async Task<IActionResult> Details(Guid id)
    {
        var opportunity = await _opportunityService.GetOpportunityByIdAsync(id);
        if (opportunity == null)
        {
            return NotFound();
        }

        var currentUser = await _userManager.GetUserAsync(User);

        // Visibility check for unapproved opportunities
        if (opportunity.ApprovalStatus != ApprovalStatus.Approved)
        {
            bool isPrivileged = User.IsInRole("Admin") || User.IsInRole("Hod");
            bool isOrganizer = currentUser != null && opportunity.OrganizerId == currentUser.Id;

            if (!isPrivileged && !isOrganizer)
            {
                return Forbid();
            }
        }

        bool alreadyApplied = false;
        bool isSaved = false;

        if (currentUser != null)
        {
            var studentProfile = await _studentProfileRepository.GetByUserIdAsync(currentUser.Id);
            if (studentProfile != null)
            {
                alreadyApplied = await _applicationRepository.HasAlreadyAppliedAsync(id, studentProfile.ProfileId);
                isSaved = await _savedOpportunityService.IsSavedAsync(studentProfile.ProfileId, id);
            }
        }

        ViewBag.AlreadyApplied = alreadyApplied;
        ViewBag.IsSaved = isSaved;

        return View(opportunity);
    }

    // GET: /Opportunities/Create
    [Authorize(Roles = "Faculty,ClubCoordinator,EventOrganizer,Hod,Admin")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Departments = await _departmentRepository.GetAllAsync();
        ViewBag.Skills = await _skillRepository.GetAllAsync();
        return View(new CreateOpportunityViewModel());
    }

    // POST: /Opportunities/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Faculty,ClubCoordinator,EventOrganizer,Hod,Admin")]
    public async Task<IActionResult> Create(CreateOpportunityViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Departments = await _departmentRepository.GetAllAsync();
            ViewBag.Skills = await _skillRepository.GetAllAsync();
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        await _opportunityService.CreateOpportunityAsync(model, user.Id);

        TempData["SuccessMessage"] = "Opportunity created successfully! It is currently pending Admin/HOD approval.";

        if (User.IsInRole("Hod") || User.IsInRole("Admin"))
        {
            return RedirectToAction(nameof(PendingApprovals));
        }
        return RedirectToAction("Dashboard", "Organizer");
    }

    // GET: /Opportunities/Edit/{id}
    [Authorize(Roles = "Faculty,ClubCoordinator,EventOrganizer,Hod,Admin")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var opp = await _opportunityService.GetOpportunityByIdAsync(id);
        if (opp == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        bool isPrivileged = User.IsInRole("Admin") || User.IsInRole("Hod");
        if (opp.OrganizerId != user.Id && !isPrivileged)
        {
            return Forbid();
        }

        var editModel = await _opportunityService.GetEditViewModelAsync(id);
        ViewBag.Departments = await _departmentRepository.GetAllAsync();
        ViewBag.Skills = await _skillRepository.GetAllAsync();
        return View(editModel);
    }

    // POST: /Opportunities/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Faculty,ClubCoordinator,EventOrganizer,Hod,Admin")]
    public async Task<IActionResult> Edit(Guid id, EditOpportunityViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Departments = await _departmentRepository.GetAllAsync();
            ViewBag.Skills = await _skillRepository.GetAllAsync();
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        bool isPrivileged = User.IsInRole("Admin") || User.IsInRole("Hod");
        bool success = await _opportunityService.UpdateOpportunityAsync(id, model, user.Id, isPrivileged);
        if (!success)
        {
            return Forbid();
        }

        TempData["SuccessMessage"] = "Opportunity updated successfully!";
        return RedirectToAction("Dashboard", "Organizer");
    }

    // POST: /Opportunities/Delete/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Faculty,ClubCoordinator,EventOrganizer,Hod,Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        bool isPrivileged = User.IsInRole("Admin") || User.IsInRole("Hod");
        bool success = await _opportunityService.DeleteOpportunityAsync(id, user.Id, isPrivileged);
        if (!success)
        {
            return Forbid();
        }

        TempData["SuccessMessage"] = "Opportunity deleted successfully.";
        return RedirectToAction("Dashboard", "Organizer");
    }

    // POST: /Opportunities/Save/{id} (Bookmark)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Save(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var studentProfile = await _studentProfileRepository.GetByUserIdAsync(user.Id);
        if (studentProfile == null)
        {
            TempData["ErrorMessage"] = "You must create a student profile to save opportunities.";
            return RedirectToAction("Create", "StudentProfiles");
        }

        var (success, message) = await _savedOpportunityService.SaveOpportunityAsync(studentProfile.ProfileId, id);
        if (success) TempData["SuccessMessage"] = message;
        else TempData["ErrorMessage"] = message;

        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /Opportunities/Unsave/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Unsave(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var studentProfile = await _studentProfileRepository.GetByUserIdAsync(user.Id);
        if (studentProfile == null) return Forbid();

        var (success, message) = await _savedOpportunityService.UnsaveOpportunityAsync(studentProfile.ProfileId, id);
        TempData["SuccessMessage"] = message;

        return RedirectToAction(nameof(Details), new { id });
    }

    // GET: /Opportunities/Saved (View student's saved bookmarks)
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Saved()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var studentProfile = await _studentProfileRepository.GetByUserIdAsync(user.Id);
        if (studentProfile == null)
        {
            return View(new List<SavedOpportunity>());
        }

        var savedOpportunities = await _savedOpportunityService.GetSavedOpportunitiesAsync(studentProfile.ProfileId);
        return View(savedOpportunities);
    }

    // GET: /Opportunities/PendingApprovals
    [Authorize(Roles = "Hod,Admin")]
    public async Task<IActionResult> PendingApprovals()
    {
        var pendingOpps = await _opportunityService.GetPendingApprovalsAsync();
        return View(pendingOpps);
    }

    // POST: /Opportunities/Approve/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Hod,Admin")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        bool isAdmin = User.IsInRole("Admin");
        bool isHod = User.IsInRole("Hod");
        int? deptId = null;

        if (isHod && !isAdmin)
        {
            var facProfile = await _facultyProfileRepository.GetByUserIdAsync(user.Id);
            if (facProfile != null) deptId = facProfile.DepartmentId;
        }

        var (success, message) = await _opportunityService.ApproveOpportunityAsync(id, user.Id, isAdmin, isHod, deptId);
        if (!success)
        {
            if (message.StartsWith("Forbidden")) return Forbid();
            TempData["ErrorMessage"] = message;
        }
        else
        {
            TempData["SuccessMessage"] = message;
        }

        return RedirectToAction(nameof(PendingApprovals));
    }

    // POST: /Opportunities/Reject/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Hod,Admin")]
    public async Task<IActionResult> Reject(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        bool isAdmin = User.IsInRole("Admin");
        bool isHod = User.IsInRole("Hod");
        int? deptId = null;

        if (isHod && !isAdmin)
        {
            var facProfile = await _facultyProfileRepository.GetByUserIdAsync(user.Id);
            if (facProfile != null) deptId = facProfile.DepartmentId;
        }

        var (success, message) = await _opportunityService.RejectOpportunityAsync(id, user.Id, isAdmin, isHod, deptId);
        if (!success)
        {
            if (message.StartsWith("Forbidden")) return Forbid();
            TempData["ErrorMessage"] = message;
        }
        else
        {
            TempData["SuccessMessage"] = message;
        }

        return RedirectToAction(nameof(PendingApprovals));
    }

    // GET: /Opportunities/Calendar
    public async Task<IActionResult> Calendar()
    {
        var events = await _calendarFeed.GetCalendarEventsAsync();
        return View(events);
    }

    // GET: /Opportunities/CalendarFeed
    [HttpGet]
    public async Task<IActionResult> CalendarFeed(DateTime? start, DateTime? end)
    {
        var events = await _calendarFeed.GetCalendarEventsAsync(start, end);
        return Json(events);
    }
}
