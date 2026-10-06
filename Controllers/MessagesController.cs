using CampusConnect.Models;
using CampusConnect.Models.ViewModels;
using CampusConnect.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.Controllers;

[Authorize]
public class MessagesController : Controller
{
    private readonly IMessageRepository _messageRepo;
    private readonly UserManager<User> _userManager;

    public MessagesController(
        IMessageRepository messageRepo,
        UserManager<User> userManager)
    {
        _messageRepo = messageRepo;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(Guid? userId)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return Challenge();

        var conversations = await _messageRepo.GetRecentConversationsForUserAsync(currentUser.Id);

        User? activeChatUser = null;
        var activeChatMessages = new List<Message>();

        // If target user specified
        if (userId.HasValue && userId.Value != currentUser.Id)
        {
            activeChatUser = await _userManager.FindByIdAsync(userId.Value.ToString());
            if (activeChatUser != null)
            {
                activeChatMessages = await _messageRepo.GetConversationAsync(currentUser.Id, activeChatUser.Id);
                await _messageRepo.MarkAsReadAsync(activeChatUser.Id, currentUser.Id);
            }
        }
        else if (conversations.Any())
        {
            // Default to the most recent conversation
            activeChatUser = conversations.First().OtherUser;
            activeChatMessages = await _messageRepo.GetConversationAsync(currentUser.Id, activeChatUser.Id);
            await _messageRepo.MarkAsReadAsync(activeChatUser.Id, currentUser.Id);
        }

        var model = new MessagesIndexViewModel
        {
            Conversations = conversations,
            ActiveChatUser = activeChatUser,
            ActiveChatMessages = activeChatMessages
        };

        return View(model);
    }

    public IActionResult Chat(Guid userId)
    {
        return RedirectToAction(nameof(Index), new { userId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(Guid receiverId, string content)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return Challenge();

        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["ErrorMessage"] = "Message cannot be empty.";
            return RedirectToAction(nameof(Index), new { userId = receiverId });
        }

        await _messageRepo.SendMessageAsync(currentUser.Id, receiverId, content);
        return RedirectToAction(nameof(Index), new { userId = receiverId });
    }
}
