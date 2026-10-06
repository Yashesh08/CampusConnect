using CampusConnect.Models.ViewModels;
using CampusConnect.Models;
using CampusConnect.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.Controllers;

[Authorize]
public class NetworkController : Controller
{
    private readonly IConnectionRepository _connectionRepo;
    private readonly UserManager<User> _userManager;

    public NetworkController(
        IConnectionRepository connectionRepo,
        UserManager<User> userManager)
    {
        _connectionRepo = connectionRepo;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var accepted = await _connectionRepo.GetConnectionsForUserAsync(user.Id);
        var received = await _connectionRepo.GetPendingRequestsReceivedAsync(user.Id);
        var sent = await _connectionRepo.GetPendingRequestsSentAsync(user.Id);

        var model = new NetworkViewModel
        {
            AcceptedConnections = accepted,
            ReceivedRequests = received,
            SentRequests = sent
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(Guid receiverId, string? returnUrl)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (receiverId == user.Id)
        {
            TempData["ErrorMessage"] = "You cannot connect with yourself.";
            return RedirectToLocal(returnUrl);
        }

        bool success = await _connectionRepo.SendRequestAsync(user.Id, receiverId);
        if (success)
        {
            TempData["SuccessMessage"] = "Connection request sent successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "A connection or pending request already exists.";
        }

        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(Guid connectionId, string? returnUrl)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        bool success = await _connectionRepo.AcceptRequestAsync(connectionId, user.Id);
        if (success)
        {
            TempData["SuccessMessage"] = "Connection request accepted!";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to accept connection request.";
        }

        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Decline(Guid connectionId, string? returnUrl)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        bool success = await _connectionRepo.DeclineRequestAsync(connectionId, user.Id);
        if (success)
        {
            TempData["SuccessMessage"] = "Connection request declined.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to decline connection request.";
        }

        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid connectionId, string? returnUrl)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        bool success = await _connectionRepo.RemoveConnectionAsync(connectionId, user.Id);
        if (success)
        {
            TempData["SuccessMessage"] = "Connection removed.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to remove connection.";
        }

        return RedirectToLocal(returnUrl);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction(nameof(Index));
    }
}
