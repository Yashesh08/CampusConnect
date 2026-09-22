using CampusConnect.Models;
using CampusConnect.Models.ViewModels;
using CampusConnect.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CampusConnect.Controllers;

[Authorize]
public class StudentProfilesController : Controller
{
    private readonly IStudentProfileRepository _repository;
    private readonly IDepartmentRepository _deptRepo;
    private readonly ISkillRepository _skillRepo;
    private readonly UserManager<User> _userManager;
    private readonly IWebHostEnvironment _env;

    public StudentProfilesController(
        IStudentProfileRepository repository, 
        IDepartmentRepository deptRepo, 
        ISkillRepository skillRepo,
        UserManager<User> userManager,
        IWebHostEnvironment env)
    {
        _repository = repository;
        _deptRepo = deptRepo;
        _skillRepo = skillRepo;
        _userManager = userManager;
        _env = env;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var profile = await _repository.GetByUserIdAsync(user.Id);
        if (profile == null)
        {
            return RedirectToAction(nameof(Create));
        }

        return RedirectToAction(nameof(Edit));
    }

    public async Task<IActionResult> Create()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        
        var profile = await _repository.GetByUserIdAsync(user.Id);
        if (profile != null) return RedirectToAction(nameof(Edit)); // Already exists

        var depts = await _deptRepo.GetAllAsync();
        ViewBag.Departments = new SelectList(depts, "DepartmentId", "DepartmentName");
        return View(new StudentProfileViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudentProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (!ModelState.IsValid)
        {
            var depts = await _deptRepo.GetAllAsync();
            ViewBag.Departments = new SelectList(depts, "DepartmentId", "DepartmentName");
            return View(model);
        }

        string? resumeUrl = null;
        if (model.ResumeFile != null)
        {
            resumeUrl = await HandleFileUpload(model.ResumeFile);
        }

        var profile = new StudentProfile
        {
            UserId = user.Id,
            RollNumber = model.RollNumber,
            DepartmentId = model.DepartmentId,
            BatchYear = model.BatchYear,
            Bio = model.Bio,
            GitHubUrl = model.GitHubUrl,
            LinkedInUrl = model.LinkedInUrl,
            ResumeUrl = resumeUrl,
        };
        
        profile.ProfileCompletionScore = CalculateCompletionScore(profile);

        await _repository.AddAsync(profile);
        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> Edit()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var p = await _repository.GetByUserIdAsync(user.Id);
        if (p == null) return RedirectToAction(nameof(Create));

        var model = new StudentProfileViewModel
        {
            ProfileId = p.ProfileId,
            RollNumber = p.RollNumber,
            DepartmentId = p.DepartmentId,
            BatchYear = p.BatchYear,
            Bio = p.Bio,
            GitHubUrl = p.GitHubUrl,
            LinkedInUrl = p.LinkedInUrl,
            ExistingResumeUrl = p.ResumeUrl
        };

        var depts = await _deptRepo.GetAllAsync();
        ViewBag.Departments = new SelectList(depts, "DepartmentId", "DepartmentName", p.DepartmentId);
        ViewBag.CompletionScore = p.ProfileCompletionScore;
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(StudentProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (!ModelState.IsValid)
        {
            var depts = await _deptRepo.GetAllAsync();
            ViewBag.Departments = new SelectList(depts, "DepartmentId", "DepartmentName", model.DepartmentId);
            return View(model);
        }

        var profile = await _repository.GetByUserIdAsync(user.Id);
        if (profile == null || profile.ProfileId != model.ProfileId) return NotFound();

        profile.RollNumber = model.RollNumber;
        profile.DepartmentId = model.DepartmentId;
        profile.BatchYear = model.BatchYear;
        profile.Bio = model.Bio;
        profile.GitHubUrl = model.GitHubUrl;
        profile.LinkedInUrl = model.LinkedInUrl;

        if (model.ResumeFile != null)
        {
            profile.ResumeUrl = await HandleFileUpload(model.ResumeFile);
        }

        profile.ProfileCompletionScore = CalculateCompletionScore(profile);

        await _repository.UpdateAsync(profile);
        return RedirectToAction("Index", "Home");
    }

    private async Task<string?> HandleFileUpload(IFormFile file)
    {
        if (file.Length > 0)
        {
            string uploadsFolder = Path.Combine(_env.WebRootPath, "resumes");
            Directory.CreateDirectory(uploadsFolder);
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            return "/resumes/" + uniqueFileName;
        }
        return null;
    }

    private int CalculateCompletionScore(StudentProfile profile)
    {
        int score = 0;
        if (!string.IsNullOrWhiteSpace(profile.RollNumber)) score += 20;
        if (profile.DepartmentId > 0) score += 20;
        if (!string.IsNullOrWhiteSpace(profile.Bio)) score += 20;
        if (!string.IsNullOrWhiteSpace(profile.GitHubUrl) || !string.IsNullOrWhiteSpace(profile.LinkedInUrl)) score += 20;
        if (!string.IsNullOrWhiteSpace(profile.ResumeUrl)) score += 20;
        return score;
    }

    public async Task<IActionResult> ManageSkills()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var profile = await _repository.GetByUserIdAsync(user.Id);
        if (profile == null) return RedirectToAction(nameof(Create));

        var allSkills = await _skillRepo.GetAllAsync();
        ViewBag.AvailableSkills = new SelectList(allSkills, "SkillId", "SkillName");
        
        return View(profile);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSkill(int skillId, CampusConnect.Models.Enums.ProficiencyLevel proficiencyLevel)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var profile = await _repository.GetByUserIdAsync(user.Id);
        if (profile == null) return NotFound();

        if (skillId > 0 && !profile.StudentSkills.Any(ss => ss.SkillId == skillId))
        {
            var ss = new StudentSkill 
            { 
                StudentId = profile.ProfileId, 
                SkillId = skillId, 
                ProficiencyLevel = proficiencyLevel 
            };
            await _repository.AddSkillAsync(ss);
        }

        return RedirectToAction(nameof(ManageSkills));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveSkill(int studentSkillId)
    {
        await _repository.RemoveSkillAsync(studentSkillId);
        return RedirectToAction(nameof(ManageSkills));
    }
}
