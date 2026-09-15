using CampusConnect.Models;
using CampusConnect.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CampusConnect.Controllers;

public class StudentProfilesController : Controller
{
    private readonly IStudentProfileRepository _repository;
    private readonly IDepartmentRepository _deptRepo;

    public StudentProfilesController(IStudentProfileRepository repository, IDepartmentRepository deptRepo)
    {
        _repository = repository;
        _deptRepo = deptRepo;
    }

    public async Task<IActionResult> Index() => View(await _repository.GetAllAsync());

    public async Task<IActionResult> Create()
    {
        var depts = await _deptRepo.GetAllAsync();
        ViewBag.Departments = new SelectList(depts, "DepartmentId", "DepartmentName");
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("RollNumber,DepartmentId,BatchYear,Bio,GitHubUrl,LinkedInUrl")] StudentProfile profile)
    {
        if (!ModelState.IsValid)
        {
            var depts = await _deptRepo.GetAllAsync();
            ViewBag.Departments = new SelectList(depts, "DepartmentId", "DepartmentName");
            return View(profile);
        }

        profile.UserId = Guid.NewGuid();
        profile.ProfileCompletionScore = 0;
        await _repository.AddAsync(profile);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var p = await _repository.GetByIdAsync(id);
        if (p == null) return NotFound();
        var depts = await _deptRepo.GetAllAsync();
        ViewBag.Departments = new SelectList(depts, "DepartmentId", "DepartmentName", p.DepartmentId);
        return View(p);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("ProfileId,RollNumber,DepartmentId,BatchYear,Bio,GitHubUrl,LinkedInUrl")] StudentProfile profile)
    {
        if (id != profile.ProfileId) return NotFound();
        if (!ModelState.IsValid)
        {
            var depts = await _deptRepo.GetAllAsync();
            ViewBag.Departments = new SelectList(depts, "DepartmentId", "DepartmentName", profile.DepartmentId);
            return View(profile);
        }
        await _repository.UpdateAsync(profile);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var p = await _repository.GetByIdAsync(id);
        return p == null ? NotFound() : View(p);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _repository.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
