using CampusConnect.Models;
using CampusConnect.Repositories;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;

namespace CampusConnect.Controllers;

[Authorize]
public class SkillsController : Controller
{
    private readonly ISkillRepository _repository;
    public SkillsController(ISkillRepository repository) => _repository = repository;

    public async Task<IActionResult> Index() => View(await _repository.GetAllAsync());

    [Authorize(Roles = "Admin")]
    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([Bind("SkillName,Category")] Skill skill)
    {
        if (!ModelState.IsValid) return View(skill);
        await _repository.AddAsync(skill);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var s = await _repository.GetByIdAsync(id);
        return s == null ? NotFound() : View(s);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, [Bind("SkillId,SkillName,Category")] Skill skill)
    {
        if (id != skill.SkillId) return NotFound();
        if (!ModelState.IsValid) return View(skill);
        await _repository.UpdateAsync(skill);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var s = await _repository.GetByIdAsync(id);
        return s == null ? NotFound() : View(s);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _repository.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
