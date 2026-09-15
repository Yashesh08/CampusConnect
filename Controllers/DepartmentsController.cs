using CampusConnect.Models;
using CampusConnect.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.Controllers;

public class DepartmentsController : Controller
{
    private readonly IDepartmentRepository _repository;
    public DepartmentsController(IDepartmentRepository repository) => _repository = repository;

    public async Task<IActionResult> Index() => View(await _repository.GetAllAsync());

    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("DepartmentName,DepartmentCode")] Department department)
    {
        if (!ModelState.IsValid) return View(department);
        await _repository.AddAsync(department);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var dept = await _repository.GetByIdAsync(id);
        return dept == null ? NotFound() : View(dept);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("DepartmentId,DepartmentName,DepartmentCode")] Department department)
    {
        if (id != department.DepartmentId) return NotFound();
        if (!ModelState.IsValid) return View(department);
        await _repository.UpdateAsync(department);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var dept = await _repository.GetByIdAsync(id);
        return dept == null ? NotFound() : View(dept);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _repository.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
