using CampusConnect.Data;
using CampusConnect.Models;
using CampusConnect.Models.ViewModels;
using CampusConnect.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Controllers;

[Authorize]
public class AnnouncementsController : Controller
{
    private readonly IAnnouncementRepository _announcementRepo;
    private readonly IDepartmentRepository _deptRepo;
    private readonly UserManager<User> _userManager;
    private readonly AppDbContext _context;

    public AnnouncementsController(
        IAnnouncementRepository announcementRepo,
        IDepartmentRepository deptRepo,
        UserManager<User> userManager,
        AppDbContext context)
    {
        _announcementRepo = announcementRepo;
        _deptRepo = deptRepo;
        _userManager = userManager;
        _context = context;
    }

    public async Task<IActionResult> Index(string? scope)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        int? userDeptId = null;
        var studentProfile = await _context.StudentProfiles.FirstOrDefaultAsync(sp => sp.UserId == user.Id);
        if (studentProfile != null) userDeptId = studentProfile.DepartmentId;

        var facultyProfile = await _context.FacultyProfiles.FirstOrDefaultAsync(fp => fp.UserId == user.Id);
        if (facultyProfile != null) userDeptId = facultyProfile.DepartmentId;

        List<Announcement> announcements;
        if (scope == "college")
        {
            announcements = await _announcementRepo.GetAnnouncementsAsync(collegeWideOnly: true);
        }
        else if (scope == "dept" && userDeptId.HasValue)
        {
            var all = await _announcementRepo.GetAnnouncementsAsync(userDeptId.Value);
            announcements = all.Where(a => a.DepartmentId == userDeptId.Value).ToList();
        }
        else
        {
            // Default: All announcements relevant to user (College-wide + user's department)
            announcements = await _announcementRepo.GetAnnouncementsAsync(userDeptId);
        }

        var departments = await _deptRepo.GetAllAsync();
        bool canPost = User.IsInRole("Faculty") || User.IsInRole("Hod") || User.IsInRole("Admin");

        var model = new AnnouncementFeedViewModel
        {
            Announcements = announcements,
            Departments = departments,
            SelectedScope = scope ?? "all",
            UserDepartmentId = userDeptId,
            CanPost = canPost
        };

        return View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var announcement = await _announcementRepo.GetByIdAsync(id);
        if (announcement == null) return NotFound();

        return View(announcement);
    }

    [Authorize(Roles = "Faculty,Hod,Admin")]
    public async Task<IActionResult> Create()
    {
        var depts = await _deptRepo.GetAllAsync();
        ViewBag.Departments = new SelectList(depts, "DepartmentId", "DepartmentName");
        return View(new CreateAnnouncementViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Faculty,Hod,Admin")]
    public async Task<IActionResult> Create(CreateAnnouncementViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (!ModelState.IsValid)
        {
            var depts = await _deptRepo.GetAllAsync();
            ViewBag.Departments = new SelectList(depts, "DepartmentId", "DepartmentName");
            return View(model);
        }

        var announcement = new Announcement
        {
            AnnouncementId = Guid.NewGuid(),
            AuthorUserId = user.Id,
            DepartmentId = model.IsCollegeWide ? null : model.DepartmentId,
            Title = model.Title.Trim(),
            Content = model.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _announcementRepo.AddAsync(announcement);
        TempData["SuccessMessage"] = "Announcement published successfully!";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Faculty,Hod,Admin")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var announcement = await _announcementRepo.GetByIdAsync(id);
        if (announcement == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        bool canEdit = announcement.AuthorUserId == user.Id || User.IsInRole("Hod") || User.IsInRole("Admin");
        if (!canEdit) return Forbid();

        var depts = await _deptRepo.GetAllAsync();
        ViewBag.Departments = new SelectList(depts, "DepartmentId", "DepartmentName", announcement.DepartmentId);

        var model = new CreateAnnouncementViewModel
        {
            Title = announcement.Title,
            Content = announcement.Content,
            IsCollegeWide = announcement.DepartmentId == null,
            DepartmentId = announcement.DepartmentId
        };

        ViewBag.AnnouncementId = id;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Faculty,Hod,Admin")]
    public async Task<IActionResult> Edit(Guid id, CreateAnnouncementViewModel model)
    {
        var announcement = await _announcementRepo.GetByIdAsync(id);
        if (announcement == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        bool canEdit = announcement.AuthorUserId == user.Id || User.IsInRole("Hod") || User.IsInRole("Admin");
        if (!canEdit) return Forbid();

        if (!ModelState.IsValid)
        {
            var depts = await _deptRepo.GetAllAsync();
            ViewBag.Departments = new SelectList(depts, "DepartmentId", "DepartmentName", model.DepartmentId);
            ViewBag.AnnouncementId = id;
            return View(model);
        }

        announcement.Title = model.Title.Trim();
        announcement.Content = model.Content.Trim();
        announcement.DepartmentId = model.IsCollegeWide ? null : model.DepartmentId;

        await _announcementRepo.UpdateAsync(announcement);
        TempData["SuccessMessage"] = "Announcement updated successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Faculty,Hod,Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var announcement = await _announcementRepo.GetByIdAsync(id);
        if (announcement == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        bool canDelete = announcement.AuthorUserId == user.Id || User.IsInRole("Hod") || User.IsInRole("Admin");
        if (!canDelete) return Forbid();

        await _announcementRepo.DeleteAsync(id);
        TempData["SuccessMessage"] = "Announcement deleted.";
        return RedirectToAction(nameof(Index));
    }
}
