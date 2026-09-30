using CampusConnect.Models;
using CampusConnect.Models.Enums;
using CampusConnect.Models.ViewModels;
using CampusConnect.Repositories;
using CampusConnect.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;

namespace CampusConnect.Controllers;

[Authorize]
public class OpportunitiesController : Controller
{
    private readonly IOpportunityRepository _opportunityRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly IOpportunityCalendarFeed _calendarFeed;
    private readonly UserManager<User> _userManager;

    public OpportunitiesController(
        IOpportunityRepository opportunityRepository,
        IApplicationRepository applicationRepository,
        IDepartmentRepository departmentRepository,
        ISkillRepository skillRepository,
        IOpportunityCalendarFeed calendarFeed,
        UserManager<User> userManager)
    {
        _opportunityRepository = opportunityRepository;
        _applicationRepository = applicationRepository;
        _departmentRepository = departmentRepository;
        _skillRepository = skillRepository;
        _calendarFeed = calendarFeed;
        _userManager = userManager;
    }

    // GET: /Opportunities (Discovery / Search / Filter)
    public async Task<IActionResult> Index(string? search, OpportunityCategory? category, WorkMode? mode, int? departmentId)
    {
        var opportunities = await _opportunityRepository.GetAllAsync(category, mode, ApprovalStatus.Approved, search);
        
        if (departmentId.HasValue && departmentId.Value > 0)
        {
            opportunities = opportunities.Where(o => o.TargetDepartmentId == departmentId.Value || o.TargetDepartmentId == null).ToList();
        }

        var departments = await _departmentRepository.GetAllAsync();

        var viewModel = new OpportunityFilterViewModel
        {
            SearchQuery = search,
            Category = category,
            WorkMode = mode,
            DepartmentId = departmentId,
            Opportunities = opportunities,
            Departments = departments
        };

        return View(viewModel);
    }

    // GET: /Opportunities/Details/{id}
    public async Task<IActionResult> Details(Guid id)
    {
        var opportunity = await _opportunityRepository.GetByIdAsync(id);
        if (opportunity == null)
        {
            return NotFound();
        }

        var currentUser = await _userManager.GetUserAsync(User);
        bool alreadyApplied = false;
        if (currentUser != null && currentUser.StudentProfile != null)
        {
            alreadyApplied = await _applicationRepository.HasAlreadyAppliedAsync(id, currentUser.StudentProfile.ProfileId);
        }

        ViewBag.AlreadyApplied = alreadyApplied;
        return View(opportunity);
    }

    // GET: /Opportunities/Create (Week 2 - Form)
    [Authorize(Roles = "Faculty,ClubCoordinator,EventOrganizer,Hod,Admin")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Departments = await _departmentRepository.GetAllAsync();
        ViewBag.Skills = await _skillRepository.GetAllAsync();
        return View(new CreateOpportunityViewModel());
    }

    // POST: /Opportunities/Create (Week 2 - Submit)
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
        Guid organizerId;
        
        if (user != null)
        {
            organizerId = user.Id;
        }
        else
        {
            var defaultUser = (await _userManager.GetUsersInRoleAsync("Faculty")).FirstOrDefault() 
                              ?? (await _userManager.GetUsersInRoleAsync("Admin")).FirstOrDefault();
            organizerId = defaultUser?.Id ?? Guid.Parse("11111111-1111-1111-1111-111111111111");
        }

        var opportunity = new Opportunity
        {
            OpportunityId = Guid.NewGuid(),
            OrganizerId = organizerId,
            Title = model.Title,
            Description = model.Description,
            Category = model.Category,
            TargetDepartmentId = model.TargetDepartmentId,
            WorkMode = model.WorkMode,
            StipendSalary = model.StipendSalary,
            RegistrationDeadline = model.RegistrationDeadline,
            EventDate = model.EventDate,
            Capacity = model.Capacity,
            ApprovalStatus = ApprovalStatus.PendingReview
        };

        await _opportunityRepository.AddAsync(opportunity, model.SelectedSkillIds);

        TempData["SuccessMessage"] = "Opportunity created successfully! It is currently pending Admin/HOD approval.";

        // Redirect based on role: Hod/Admin can view approvals, others go to their dashboard
        if (User.IsInRole("Hod") || User.IsInRole("Admin"))
        {
            return RedirectToAction(nameof(PendingApprovals));
        }
        return RedirectToAction("Dashboard", "Organizer");
    }

    // GET: /Opportunities/PendingApprovals (Week 2 - Admin / HOD Approval Workflow)
    [Authorize(Roles = "Hod,Admin")]
    public async Task<IActionResult> PendingApprovals()
    {
        var pendingOpps = await _opportunityRepository.GetPendingApprovalsAsync();
        return View(pendingOpps);
    }

    // POST: /Opportunities/Approve/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Hod,Admin")]
    public async Task<IActionResult> Approve(Guid id)
    {
        
        var opp = await _opportunityRepository.GetByIdAsync(id);
        if (User.IsInRole("Hod") && !User.IsInRole("Admin")) {
            var user = await _userManager.GetUserAsync(User);
            var facRepo = HttpContext.RequestServices.GetService<CampusConnect.Repositories.IFacultyProfileRepository>();
            var facProfile = facRepo != null ? await facRepo.GetByUserIdAsync(user.Id) : null;
            if (facProfile == null || (opp?.TargetDepartmentId != null && opp.TargetDepartmentId != facProfile.DepartmentId)) return Forbid();
        }
        await _opportunityRepository.UpdateStatusAsync(id, ApprovalStatus.Approved);

        TempData["SuccessMessage"] = "Opportunity approved and published successfully!";
        return RedirectToAction(nameof(PendingApprovals));
    }

    // POST: /Opportunities/Reject/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Hod,Admin")]
    public async Task<IActionResult> Reject(Guid id)
    {
        
        var opp = await _opportunityRepository.GetByIdAsync(id);
        if (User.IsInRole("Hod") && !User.IsInRole("Admin")) {
            var user = await _userManager.GetUserAsync(User);
            var facRepo = HttpContext.RequestServices.GetService<CampusConnect.Repositories.IFacultyProfileRepository>();
            var facProfile = facRepo != null ? await facRepo.GetByUserIdAsync(user.Id) : null;
            if (facProfile == null || (opp?.TargetDepartmentId != null && opp.TargetDepartmentId != facProfile.DepartmentId)) return Forbid();
        }
        await _opportunityRepository.UpdateStatusAsync(id, ApprovalStatus.Rejected);

        TempData["SuccessMessage"] = "Opportunity has been rejected.";
        return RedirectToAction(nameof(PendingApprovals));
    }

    // GET: /Opportunities/Calendar (Visual Calendar View)
    public async Task<IActionResult> Calendar()
    {
        var events = await _calendarFeed.GetCalendarEventsAsync();
        return View(events);
    }

    // GET: /Opportunities/CalendarFeed (JSON feed for Week 6 integration)
    [HttpGet]
    public async Task<IActionResult> CalendarFeed(DateTime? start, DateTime? end)
    {
        var events = await _calendarFeed.GetCalendarEventsAsync(start, end);
        return Json(events);
    }
}
