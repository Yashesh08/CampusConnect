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

    public HomeController(
        UserManager<User> userManager,
        IStudentProfileRepository studentRepo,
        ISkillRepository skillRepo,
        ISkillMatchingEngine matchingEngine)
    {
        _userManager = userManager;
        _studentRepo = studentRepo;
        _skillRepo = skillRepo;
        _matchingEngine = matchingEngine;
    }

    public IActionResult Index() => View();

    public async Task<IActionResult> TestEngine()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var studentProfile = await _studentRepo.GetByUserIdAsync(user.Id);
        if (studentProfile == null)
        {
            return RedirectToAction("Create", "StudentProfiles");
        }

        // Fetch all skills to create a dummy opportunity
        var allSkills = await _skillRepo.GetAllAsync();
        
        // Create a dummy opportunity that requires the first 3 skills in the DB
        var dummyOpportunity = new Opportunity
        {
            Title = "Senior Software Engineering Internship",
            Description = "A great opportunity requiring specific skills.",
            Category = CampusConnect.Models.Enums.OpportunityCategory.Internship,
            RequiredSkills = allSkills.Take(3).Select(s => new OpportunitySkill 
            { 
                SkillId = s.SkillId, 
                Skill = s 
            }).ToList()
        };

        // Run the matching engine
        var matchResult = _matchingEngine.CalculateMatch(studentProfile, dummyOpportunity);

        ViewBag.Opportunity = dummyOpportunity;
        ViewBag.MatchResult = matchResult;

        return View();
    }
}
