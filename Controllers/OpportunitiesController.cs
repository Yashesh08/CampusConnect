using CampusConnect.Models;
using CampusConnect.Models.Enums;
using CampusConnect.Models.ViewModels;
using CampusConnect.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.Controllers;

public class OpportunitiesController : Controller
{
    private readonly IOpportunityRepository _opportunityRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly UserManager<User> _userManager;

    public OpportunitiesController(
        IOpportunityRepository opportunityRepository,
        IDepartmentRepository departmentRepository,
        ISkillRepository skillRepository,
        UserManager<User> userManager)
    {
        _opportunityRepository = opportunityRepository;
        _departmentRepository = departmentRepository;
        _skillRepository = skillRepository;
        _userManager = userManager;
    }

    // GET: /Opportunities/Create (Week 2 - Opportunity Creation Form)
    public async Task<IActionResult> Create()
    {
        ViewBag.Departments = await _departmentRepository.GetAllAsync();
        ViewBag.Skills = await _skillRepository.GetAllAsync();
        return View(new CreateOpportunityViewModel());
    }

    // POST: /Opportunities/Create (Week 2 - Opportunity Creation Submission)
    [HttpPost]
    [ValidateAntiForgeryToken]
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
        return RedirectToAction(nameof(PendingApprovals));
    }

    // GET: /Opportunities/PendingApprovals (Week 2 - Admin / HOD Approval Workflow)
    public async Task<IActionResult> PendingApprovals()
    {
        var pendingOpps = await _opportunityRepository.GetPendingApprovalsAsync();
        return View(pendingOpps);
    }

    // POST: /Opportunities/Approve/{id} (Week 2 - Admin / HOD Approval Action)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id)
    {
        await _opportunityRepository.UpdateStatusAsync(id, ApprovalStatus.Approved);
        TempData["SuccessMessage"] = "Opportunity approved and published successfully!";
        return RedirectToAction(nameof(PendingApprovals));
    }

    // POST: /Opportunities/Reject/{id} (Week 2 - Admin / HOD Rejection Action)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id)
    {
        await _opportunityRepository.UpdateStatusAsync(id, ApprovalStatus.Rejected);
        TempData["SuccessMessage"] = "Opportunity has been rejected.";
        return RedirectToAction(nameof(PendingApprovals));
    }
}
