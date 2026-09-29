using CampusConnect.Models;
using CampusConnect.Models.ViewModels;
using CampusConnect.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CampusConnect.Controllers;

[Authorize(Roles = "Faculty,Hod,Admin")]
public class FacultyProfilesController : Controller
{
    private readonly IFacultyProfileRepository _repository;
    private readonly IDepartmentRepository _deptRepo;
    private readonly UserManager<User> _userManager;

    public FacultyProfilesController(
        IFacultyProfileRepository repository, 
        IDepartmentRepository deptRepo, 
        UserManager<User> userManager)
    {
        _repository = repository;
        _deptRepo = deptRepo;
        _userManager = userManager;
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

        return RedirectToAction(nameof(Dashboard));
    }

    public async Task<IActionResult> Dashboard()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var profile = await _repository.GetByUserIdAsync(user.Id);
        if (profile == null) return RedirectToAction(nameof(Create));

        return View(profile);
    }

    public async Task<IActionResult> Create()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        
        var profile = await _repository.GetByUserIdAsync(user.Id);
        if (profile != null) return RedirectToAction(nameof(Edit)); // Already exists

        var depts = await _deptRepo.GetAllAsync();
        ViewBag.Departments = new SelectList(depts, "DepartmentId", "DepartmentName");
        return View(new FacultyProfileViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FacultyProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (!ModelState.IsValid)
        {
            var depts = await _deptRepo.GetAllAsync();
            ViewBag.Departments = new SelectList(depts, "DepartmentId", "DepartmentName");
            return View(model);
        }

        var profile = new FacultyProfile
        {
            UserId = user.Id,
            DepartmentId = model.DepartmentId,
            Designation = model.Designation,
            CabinNumber = model.CabinNumber
        };

        await _repository.AddAsync(profile);
        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> Edit()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var p = await _repository.GetByUserIdAsync(user.Id);
        if (p == null) return RedirectToAction(nameof(Create));

        var model = new FacultyProfileViewModel
        {
            FacultyProfileId = p.FacultyProfileId,
            DepartmentId = p.DepartmentId,
            Designation = p.Designation,
            CabinNumber = p.CabinNumber
        };

        var depts = await _deptRepo.GetAllAsync();
        ViewBag.Departments = new SelectList(depts, "DepartmentId", "DepartmentName", p.DepartmentId);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(FacultyProfileViewModel model)
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
        if (profile == null || profile.FacultyProfileId != model.FacultyProfileId) return NotFound();

        profile.DepartmentId = model.DepartmentId;
        profile.Designation = model.Designation;
        profile.CabinNumber = model.CabinNumber;

        await _repository.UpdateAsync(profile);
        return RedirectToAction("Index", "Home");
    }
}
