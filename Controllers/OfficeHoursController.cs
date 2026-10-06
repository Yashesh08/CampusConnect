using CampusConnect.Data;
using CampusConnect.Models;
using CampusConnect.Models.Enums;
using CampusConnect.Models.ViewModels;
using CampusConnect.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Controllers;

[Authorize]
public class OfficeHoursController : Controller
{
    private readonly IFacultyOfficeHourRepository _officeHourRepo;
    private readonly IDepartmentRepository _deptRepo;
    private readonly UserManager<User> _userManager;
    private readonly AppDbContext _context;

    public OfficeHoursController(
        IFacultyOfficeHourRepository officeHourRepo,
        IDepartmentRepository deptRepo,
        UserManager<User> userManager,
        AppDbContext context)
    {
        _officeHourRepo = officeHourRepo;
        _deptRepo = deptRepo;
        _userManager = userManager;
        _context = context;
    }

    public async Task<IActionResult> Index(int? departmentId, Guid? facultyId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var availableSlots = await _officeHourRepo.GetUpcomingAvailableSlotsAsync(departmentId, facultyId);

        var myBookedSlots = new List<FacultyOfficeHour>();
        var studentProfile = await _context.StudentProfiles.FirstOrDefaultAsync(sp => sp.UserId == user.Id);
        if (studentProfile != null)
        {
            myBookedSlots = await _officeHourRepo.GetBookedSlotsByStudentAsync(studentProfile.ProfileId);
        }

        var departments = await _deptRepo.GetAllAsync();
        var facultyUsers = await _context.Users
            .Include(u => u.FacultyProfile)
            .Where(u => u.Role == UserRole.Faculty || u.Role == UserRole.Hod)
            .OrderBy(u => u.Email)
            .ToListAsync();

        var model = new OfficeHoursIndexViewModel
        {
            AvailableSlots = availableSlots,
            MyBookedSlots = myBookedSlots,
            Departments = departments,
            FacultyMembers = facultyUsers,
            SelectedDepartmentId = departmentId,
            SelectedFacultyUserId = facultyId,
            IsStudent = User.IsInRole("Student"),
            IsFaculty = User.IsInRole("Faculty") || User.IsInRole("Hod") || User.IsInRole("Admin")
        };

        return View(model);
    }

    [Authorize(Roles = "Faculty,Hod,Admin")]
    public async Task<IActionResult> Manage()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var slots = await _officeHourRepo.GetSlotsByFacultyAsync(user.Id, includePast: true);
        var now = DateTime.UtcNow;

        var model = new ManageOfficeHoursViewModel
        {
            UpcomingSlots = slots.Where(s => s.EndTime >= now).OrderBy(s => s.StartTime).ToList(),
            PastSlots = slots.Where(s => s.EndTime < now).OrderByDescending(s => s.StartTime).ToList(),
            NewSlot = new CreateOfficeHourSlotViewModel
            {
                Date = DateTime.UtcNow.AddDays(1).Date,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 0, 0)
            }
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Faculty,Hod,Admin")]
    public async Task<IActionResult> CreateSlot(CreateOfficeHourSlotViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Please provide valid date and time values.";
            return RedirectToAction(nameof(Manage));
        }

        var startDateTime = model.Date.Date.Add(model.StartTime);
        var endDateTime = model.Date.Date.Add(model.EndTime);

        var slot = new FacultyOfficeHour
        {
            SlotId = Guid.NewGuid(),
            FacultyUserId = user.Id,
            StartTime = DateTime.SpecifyKind(startDateTime, DateTimeKind.Utc),
            EndTime = DateTime.SpecifyKind(endDateTime, DateTimeKind.Utc),
            IsBooked = false
        };

        var result = await _officeHourRepo.AddSlotAsync(slot);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Manage));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> BookSlot(Guid slotId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var studentProfile = await _context.StudentProfiles.FirstOrDefaultAsync(sp => sp.UserId == user.Id);
        if (studentProfile == null)
        {
            TempData["ErrorMessage"] = "Please complete your student profile before booking office hours.";
            return RedirectToAction("Create", "StudentProfiles");
        }

        var result = await _officeHourRepo.BookSlotAsync(slotId, studentProfile.ProfileId);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelBooking(Guid slotId, string? returnUrl)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        bool isFaculty = User.IsInRole("Faculty") || User.IsInRole("Hod") || User.IsInRole("Admin");
        var result = await _officeHourRepo.CancelBookingAsync(slotId, user.Id, isFaculty);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(isFaculty ? nameof(Manage) : nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Faculty,Hod,Admin")]
    public async Task<IActionResult> DeleteSlot(Guid slotId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var result = await _officeHourRepo.DeleteSlotAsync(slotId, user.Id);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Manage));
    }
}
