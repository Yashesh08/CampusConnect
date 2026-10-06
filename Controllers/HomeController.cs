using CampusConnect.Models;
using CampusConnect.Repositories;
using CampusConnect.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.Controllers;

public class HomeController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly IStudentProfileRepository _studentRepo;
    private readonly ISkillRepository _skillRepo;
    private readonly ISkillMatchingEngine _matchingEngine;
    private readonly ISharedOpportunityFeed _opportunityFeed;

    public HomeController(
        UserManager<User> userManager,
        IStudentProfileRepository studentRepo,
        ISkillRepository skillRepo,
        ISkillMatchingEngine matchingEngine,
        ISharedOpportunityFeed opportunityFeed)
    {
        _userManager = userManager;
        _studentRepo = studentRepo;
        _skillRepo = skillRepo;
        _matchingEngine = matchingEngine;
        _opportunityFeed = opportunityFeed;
    }

    public IActionResult Index() => View();

    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> TestEngine(Guid? studentProfileId, Guid? opportunityId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        StudentProfile? studentProfile = null;
        var allStudents = await _studentRepo.GetAllAsync();

        if (user.Role == CampusConnect.Models.Enums.UserRole.Student)
        {
            studentProfile = await _studentRepo.GetByUserIdAsync(user.Id);
            if (studentProfile == null)
            {
                TempData["ErrorMessage"] = "Please complete your student profile first before running the skill simulator.";
                return RedirectToAction("Create", "StudentProfiles");
            }
        }
        else
        {
            // Non-students (Faculty, Admin, HOD, Organizers) simulate using a student profile
            if (studentProfileId.HasValue)
            {
                studentProfile = await _studentRepo.GetByIdAsync(studentProfileId.Value);
            }

            if (studentProfile == null)
            {
                studentProfile = allStudents.FirstOrDefault();
            }

            if (studentProfile == null)
            {
                studentProfile = new StudentProfile
                {
                    RollNumber = "DEMO-CS2026",
                    Bio = "Demo Candidate Profile for Simulation",
                    StudentSkills = new List<StudentSkill>()
                };
            }
        }

        // Fetch skills & opportunities
        var allSkills = await _skillRepo.GetAllAsync();
        var opportunities = await _opportunityFeed.GetUpcomingOpportunitiesAsync();
        Opportunity? selectedOpportunity = null;

        if (opportunityId.HasValue)
        {
            selectedOpportunity = opportunities.FirstOrDefault(o => o.OpportunityId == opportunityId.Value);
        }

        if (selectedOpportunity == null)
        {
            selectedOpportunity = opportunities.FirstOrDefault(o => o.RequiredSkills.Any()) ?? new Opportunity
            {
                Title = "Senior Software Engineering Internship",
                Description = "A core engineering internship requiring modern full-stack development skills and algorithmic proficiency.",
                Category = CampusConnect.Models.Enums.OpportunityCategory.Internship,
                RequiredSkills = allSkills.Take(3).Select(s => new OpportunitySkill 
                { 
                    SkillId = s.SkillId, 
                    Skill = s 
                }).ToList()
            };
        }

        // Run the matching engine
        var matchResult = _matchingEngine.CalculateMatch(studentProfile, selectedOpportunity);

        ViewBag.Opportunity = selectedOpportunity;
        ViewBag.MatchResult = matchResult;
        ViewBag.StudentProfile = studentProfile;
        ViewBag.AvailableOpportunities = opportunities;
        ViewBag.AvailableStudents = allStudents;
        ViewBag.IsSimulatorForNonStudent = user.Role != CampusConnect.Models.Enums.UserRole.Student;

        return View();
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> DashboardRouter()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        return user.Role switch
        {
            CampusConnect.Models.Enums.UserRole.Student => RedirectToAction("Dashboard", "StudentProfiles"),
            CampusConnect.Models.Enums.UserRole.Faculty => RedirectToAction("Dashboard", "FacultyProfiles"),
            CampusConnect.Models.Enums.UserRole.Hod => RedirectToAction("Dashboard", "FacultyProfiles"),
            CampusConnect.Models.Enums.UserRole.Admin => RedirectToAction("Dashboard", "FacultyProfiles"),
            CampusConnect.Models.Enums.UserRole.EventOrganizer => RedirectToAction("Dashboard", "Organizer"),
            CampusConnect.Models.Enums.UserRole.ClubCoordinator => RedirectToAction("Dashboard", "Organizer"),
            _ => RedirectToAction("Index", "Home")
        };
    }
}
