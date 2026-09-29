using CampusConnect.Models;
using CampusConnect.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.Controllers;

[Authorize(Roles = "Hod,Admin")]
public class HodController : Controller
{
    private readonly IOpportunityRepository _opportunityRepo;
    private readonly UserManager<User> _userManager;

    public HodController(
        IOpportunityRepository opportunityRepo,
        UserManager<User> userManager)
    {
        _opportunityRepo = opportunityRepo;
        _userManager = userManager;
    }

    public async Task<IActionResult> Dashboard()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var pendingOpps = await _opportunityRepo.GetPendingApprovalsAsync();
        ViewBag.PendingOpportunitiesCount = pendingOpps.Count;
        
        return View(user);
    }
}
