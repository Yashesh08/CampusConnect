using CampusConnect.Models;
using CampusConnect.Models.Enums;
using CampusConnect.Models.ViewModels;
using CampusConnect.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.Controllers;

[Authorize]
public class GrievancesController : Controller
{
    private readonly IGrievanceRepository _grievanceRepo;
    private readonly IDepartmentRepository _departmentRepo;
    private readonly UserManager<User> _userManager;

    // SLA business-day lookup per priority
    private static readonly Dictionary<GrievancePriority, int> SlaDays = new()
    {
        { GrievancePriority.Critical, 1 },
        { GrievancePriority.High,     3 },
        { GrievancePriority.Medium,   5 },
        { GrievancePriority.Low,      10 }
    };

    public GrievancesController(
        IGrievanceRepository grievanceRepo,
        IDepartmentRepository departmentRepo,
        UserManager<User> userManager)
    {
        _grievanceRepo = grievanceRepo;
        _departmentRepo = departmentRepo;
        _userManager = userManager;
    }

    // ─────────────────────────────────────────────────────────────────
    // GET: /Grievances
    // ─────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Index(
        GrievanceStatus? status,
        GrievanceCategory? category,
        int? departmentId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        bool isPrivileged = User.IsInRole("Hod") || User.IsInRole("Admin");
        bool isOfficer = User.IsInRole("Faculty") || isPrivileged;

        List<Grievance> grievances;

        if (isPrivileged)
        {
            grievances = await _grievanceRepo.GetAllAsync(status, category, departmentId);
        }
        else if (isOfficer)
        {
            // Officers see grievances assigned to them or their department
            grievances = await _grievanceRepo.GetAssignedToUserAsync(user.Id);
        }
        else
        {
            // Students see their own submissions
            grievances = await _grievanceRepo.GetByComplainantIdAsync(user.Id);
        }

        ViewBag.Departments = await _departmentRepo.GetAllAsync();
        ViewBag.FilterStatus = status;
        ViewBag.FilterCategory = category;
        ViewBag.FilterDeptId = departmentId;
        ViewBag.IsPrivileged = isPrivileged;
        ViewBag.IsOfficer = isOfficer;

        return View(grievances);
    }

    // ─────────────────────────────────────────────────────────────────
    // GET: /Grievances/Tracker  — Student Ticket Tracking View
    // ─────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Tracker(string? ticketId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var myGrievances = await _grievanceRepo.GetByComplainantIdAsync(user.Id);

        Grievance? selected = null;
        bool searchAttempted = !string.IsNullOrWhiteSpace(ticketId);

        if (!string.IsNullOrWhiteSpace(ticketId))
        {
            var cleanId = ticketId.Trim().Replace("#", "");
            if (Guid.TryParse(cleanId, out var parsedGuid))
            {
                selected = await _grievanceRepo.GetByIdAsync(parsedGuid);
            }
            else
            {
                // Match by first 8 chars prefix
                var allGrievances = await _grievanceRepo.GetAllAsync();
                selected = allGrievances.FirstOrDefault(g =>
                    g.GrievanceId.ToString().Replace("-", "").StartsWith(cleanId, StringComparison.OrdinalIgnoreCase) ||
                    g.GrievanceId.ToString().StartsWith(cleanId, StringComparison.OrdinalIgnoreCase));

                if (selected != null)
                {
                    selected = await _grievanceRepo.GetByIdAsync(selected.GrievanceId);
                }
            }

            // SECURITY FIX (V007): Ensure user is authorized to view this ticket!
            // Non-privileged users (e.g. students) must only view their own grievance.
            bool isPrivileged = User.IsInRole("Hod") || User.IsInRole("Admin");
            bool isAssignedOfficer = selected != null && selected.AssignedToUserId == user.Id;

            if (selected != null && !isPrivileged && !isAssignedOfficer && selected.ComplainantUserId != user.Id)
            {
                // Do not leak other users' or anonymous grievances to unauthorized students
                selected = null;
            }
        }
        else if (myGrievances.Any())
        {
            selected = myGrievances.First();
            if (selected.Logs == null || !selected.Logs.Any())
            {
                selected = await _grievanceRepo.GetByIdAsync(selected.GrievanceId);
            }
        }

        var model = new GrievanceTrackerViewModel
        {
            SelectedGrievance = selected,
            MyGrievances = myGrievances,
            SearchQuery = ticketId,
            SearchAttempted = searchAttempted
        };

        return View(model);
    }

    // ─────────────────────────────────────────────────────────────────
    // GET: /Grievances/OfficerDashboard  — Officer View (Faculty / HOD / Admin)
    // ─────────────────────────────────────────────────────────────────
    [Authorize(Roles = "Faculty,Hod,Admin")]
    public async Task<IActionResult> OfficerDashboard(GrievanceStatus? status)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var assigned = await _grievanceRepo.GetAssignedToUserAsync(user.Id);

        // All unassigned grievances across the board or filtered
        var allGrievances = await _grievanceRepo.GetAllAsync();
        var unassigned = allGrievances.Where(g => g.AssignedToUserId == null && g.Status != GrievanceStatus.Resolved).ToList();

        if (status.HasValue)
        {
            assigned = assigned.Where(g => g.Status == status.Value).ToList();
        }

        var model = new GrievanceOfficerDashboardViewModel
        {
            AssignedGrievances = assigned,
            UnassignedDepartmentGrievances = unassigned,
            FilterStatus = status
        };

        return View(model);
    }

    // ─────────────────────────────────────────────────────────────────
    // GET: /Grievances/Escalations  — HOD / Admin Escalation View
    // ─────────────────────────────────────────────────────────────────
    [Authorize(Roles = "Hod,Admin")]
    public async Task<IActionResult> Escalations()
    {
        var escalated = await _grievanceRepo.GetEscalatedAsync();
        var overdue = await _grievanceRepo.GetOverdueAsync();

        var allGrievances = await _grievanceRepo.GetAllAsync();
        var critical = allGrievances
            .Where(g => g.Priority == GrievancePriority.Critical && g.Status != GrievanceStatus.Resolved)
            .ToList();

        // Get available officers for reassignment
        var faculty = await _userManager.GetUsersInRoleAsync("Faculty");
        var hods = await _userManager.GetUsersInRoleAsync("Hod");
        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        var officers = faculty.Concat(hods).Concat(admins).DistinctBy(u => u.Id).OrderBy(u => u.Email).ToList();

        var model = new GrievanceEscalationViewModel
        {
            EscalatedGrievances = escalated,
            OverdueGrievances = overdue,
            CriticalGrievances = critical,
            AvailableOfficers = officers
        };

        return View(model);
    }

    // ─────────────────────────────────────────────────────────────────
    // GET: /Grievances/Details/{id}
    // ─────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Details(Guid id)
    {
        var grievance = await _grievanceRepo.GetByIdAsync(id);
        if (grievance == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        bool isPrivileged = User.IsInRole("Hod") || User.IsInRole("Admin");
        bool isOfficer = User.IsInRole("Faculty") || isPrivileged;
        bool isAssignedOfficer = grievance.AssignedToUserId == user.Id;

        // SECURITY FIX (V007b): Non-privileged users can only view their own submissions.
        // Anonymous grievances can only be viewed by privileged users or the assigned officer.
        if (!isPrivileged && !isAssignedOfficer && (grievance.ComplainantUserId != user.Id || grievance.IsAnonymous))
        {
            return Forbid();
        }

        // Available officers for assignment dropdown
        if (isPrivileged)
        {
            var faculty = await _userManager.GetUsersInRoleAsync("Faculty");
            var hods = await _userManager.GetUsersInRoleAsync("Hod");
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            ViewBag.AvailableOfficers = faculty.Concat(hods).Concat(admins).DistinctBy(u => u.Id).OrderBy(u => u.Email).ToList();
        }

        ViewBag.IsPrivileged = isPrivileged;
        ViewBag.IsOfficer = isOfficer;
        ViewBag.IsAssignedOfficer = isAssignedOfficer;
        ViewBag.CurrentUserId = user.Id;

        return View(grievance);
    }

    // ─────────────────────────────────────────────────────────────────
    // GET: /Grievances/Create
    // ─────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Create()
    {
        ViewBag.Departments = await _departmentRepo.GetAllAsync();
        return View(new CreateGrievanceViewModel());
    }

    // ─────────────────────────────────────────────────────────────────
    // POST: /Grievances/Create
    // ─────────────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateGrievanceViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Departments = await _departmentRepo.GetAllAsync();
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        int slaDays = SlaDays[model.Priority];
        DateTime slaDueAt = AddBusinessDays(DateTime.UtcNow, slaDays);

        var grievance = new Grievance
        {
            GrievanceId       = Guid.NewGuid(),
            ComplainantUserId = model.IsAnonymous ? null : user.Id,
            IsAnonymous       = model.IsAnonymous,
            Category          = model.Category,
            DepartmentId      = model.DepartmentId,
            Description       = model.Description,
            Priority          = model.Priority,
            Status            = GrievanceStatus.Submitted,
            SlaDueAt          = slaDueAt,
            CreatedAt         = DateTime.UtcNow
        };

        await _grievanceRepo.AddAsync(grievance, user.Id);

        TempData["SuccessMessage"] =
            $"Grievance #{grievance.GrievanceId.ToString()[..8].ToUpper()} submitted successfully! " +
            $"SLA target: {slaDueAt:MMM dd, yyyy} ({slaDays} business day{(slaDays == 1 ? "" : "s")}).";

        return RedirectToAction(nameof(Tracker), new { ticketId = grievance.GrievanceId.ToString()[..8].ToUpper() });
    }

    // ─────────────────────────────────────────────────────────────────
    // POST: /Grievances/UpdateStatus/{id}
    // ─────────────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid id, GrievanceStatus newStatus, string? note, string? returnUrl)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var grievance = await _grievanceRepo.GetByIdAsync(id);
        if (grievance == null) return NotFound();

        bool isPrivileged = User.IsInRole("Hod") || User.IsInRole("Admin");
        bool isAssignedOfficer = grievance.AssignedToUserId == user.Id;

        if (!isPrivileged && !isAssignedOfficer)
        {
            return Forbid();
        }

        // Validate workflow transition
        if (!IsValidTransition(grievance.Status, newStatus, isPrivileged))
        {
            TempData["ErrorMessage"] = $"Invalid status transition from {grievance.Status} to {newStatus}.";
            return RedirectBack(returnUrl, id);
        }

        await _grievanceRepo.UpdateStatusAsync(id, newStatus, user.Id, note);

        TempData["SuccessMessage"] = $"Grievance #{grievance.GrievanceId.ToString()[..8].ToUpper()} updated to \"{newStatus}\".";
        return RedirectBack(returnUrl, id);
    }

    // ─────────────────────────────────────────────────────────────────
    // POST: /Grievances/Assign/{id}
    // ─────────────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(Guid id, Guid assignedToUserId, string? note, string? returnUrl)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var grievance = await _grievanceRepo.GetByIdAsync(id);
        if (grievance == null) return NotFound();

        bool isPrivileged = User.IsInRole("Hod") || User.IsInRole("Admin");
        bool isSelfAssign = (User.IsInRole("Faculty") && assignedToUserId == user.Id);

        if (!isPrivileged && !isSelfAssign)
        {
            return Forbid();
        }

        var assignedUser = await _userManager.FindByIdAsync(assignedToUserId.ToString());
        string officerName = assignedUser?.Email ?? "Officer";

        string logNote = string.IsNullOrWhiteSpace(note)
            ? $"Grievance assigned to {officerName} for resolution."
            : $"{note} (Assigned to: {officerName})";

        await _grievanceRepo.AssignAsync(id, assignedToUserId, user.Id, logNote);

        TempData["SuccessMessage"] = $"Grievance #{grievance.GrievanceId.ToString()[..8].ToUpper()} assigned to {officerName}.";
        return RedirectBack(returnUrl, id);
    }

    // ─────────────────────────────────────────────────────────────────
    // Helper: Valid transition rules
    // ─────────────────────────────────────────────────────────────────
    private static bool IsValidTransition(GrievanceStatus current, GrievanceStatus target, bool isPrivileged)
    {
        if (current == target) return true;
        if (isPrivileged) return true; // HOD/Admin have full override capability

        return (current, target) switch
        {
            (GrievanceStatus.Submitted,    GrievanceStatus.Acknowledged) => true,
            (GrievanceStatus.Submitted,    GrievanceStatus.Escalated)    => true,
            (GrievanceStatus.Acknowledged, GrievanceStatus.InProgress)   => true,
            (GrievanceStatus.Acknowledged, GrievanceStatus.Escalated)    => true,
            (GrievanceStatus.InProgress,   GrievanceStatus.Resolved)     => true,
            (GrievanceStatus.InProgress,   GrievanceStatus.Escalated)    => true,
            (GrievanceStatus.Escalated,    GrievanceStatus.InProgress)   => true,
            (GrievanceStatus.Escalated,    GrievanceStatus.Resolved)     => true,
            (GrievanceStatus.Resolved,     GrievanceStatus.InProgress)   => true,
            _ => false
        };
    }

    // ─────────────────────────────────────────────────────────────────
    // Helper: Redirect back or to Details
    // ─────────────────────────────────────────────────────────────────
    private IActionResult RedirectBack(string? returnUrl, Guid id)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    // ─────────────────────────────────────────────────────────────────
    // Helper: Add N business days (skip Sat/Sun)
    // ─────────────────────────────────────────────────────────────────
    private static DateTime AddBusinessDays(DateTime start, int businessDays)
    {
        var date = start;
        int added = 0;
        while (added < businessDays)
        {
            date = date.AddDays(1);
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                added++;
        }
        return date;
    }
}
