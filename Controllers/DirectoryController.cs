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
public class DirectoryController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IConnectionRepository _connectionRepo;
    private readonly IDepartmentRepository _deptRepo;

    public DirectoryController(
        AppDbContext context,
        UserManager<User> userManager,
        IConnectionRepository connectionRepo,
        IDepartmentRepository deptRepo)
    {
        _context = context;
        _userManager = userManager;
        _connectionRepo = connectionRepo;
        _deptRepo = deptRepo;
    }

    public async Task<IActionResult> Index(
        string? search,
        string? role,
        int? departmentId,
        string? skill)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return Challenge();

        var query = _context.Users
            .Include(u => u.StudentProfile)
                .ThenInclude(sp => sp!.Department)
            .Include(u => u.StudentProfile)
                .ThenInclude(sp => sp!.StudentSkills)
                    .ThenInclude(ss => ss.Skill)
            .Include(u => u.FacultyProfile)
                .ThenInclude(fp => fp!.Department)
            .Where(u => u.Status == UserStatus.Active)
            .AsQueryable();

        // Role filter
        if (!string.IsNullOrWhiteSpace(role))
        {
            if (role.Equals("Student", StringComparison.OrdinalIgnoreCase))
                query = query.Where(u => u.Role == UserRole.Student);
            else if (role.Equals("Faculty", StringComparison.OrdinalIgnoreCase))
                query = query.Where(u => u.Role == UserRole.Faculty || u.Role == UserRole.Hod);
        }

        // Department filter
        if (departmentId.HasValue)
        {
            query = query.Where(u =>
                (u.StudentProfile != null && u.StudentProfile.DepartmentId == departmentId.Value) ||
                (u.FacultyProfile != null && u.FacultyProfile.DepartmentId == departmentId.Value));
        }

        // Search text filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(s)) ||
                (u.StudentProfile != null && (
                    u.StudentProfile.RollNumber.ToLower().Contains(s) ||
                    (u.StudentProfile.Bio != null && u.StudentProfile.Bio.ToLower().Contains(s)) ||
                    u.StudentProfile.StudentSkills.Any(sk => sk.Skill.SkillName.ToLower().Contains(s))
                )) ||
                (u.FacultyProfile != null && (
                    u.FacultyProfile.Designation.ToLower().Contains(s) ||
                    (u.FacultyProfile.CabinNumber != null && u.FacultyProfile.CabinNumber.ToLower().Contains(s))
                )));
        }

        // Skill filter
        if (!string.IsNullOrWhiteSpace(skill))
        {
            var sk = skill.Trim().ToLower();
            query = query.Where(u => u.StudentProfile != null &&
                u.StudentProfile.StudentSkills.Any(s => s.Skill.SkillName.ToLower() == sk));
        }

        var users = await query.ToListAsync();

        // Get connection states with current user
        var otherUserIds = users.Select(u => u.Id).Where(id => id != currentUser.Id).ToList();
        var rawConnections = await _context.Connections
            .Where(c => (c.SenderUserId == currentUser.Id && otherUserIds.Contains(c.ReceiverUserId)) ||
                        (c.ReceiverUserId == currentUser.Id && otherUserIds.Contains(c.SenderUserId)))
            .ToListAsync();

        // Get count of available office hours for faculty members
        var now = DateTime.UtcNow;
        var facultyIds = users.Where(u => u.Role == UserRole.Faculty || u.Role == UserRole.Hod).Select(u => u.Id).ToList();
        var officeHourCounts = await _context.FacultyOfficeHours
            .Where(foh => facultyIds.Contains(foh.FacultyUserId) && !foh.IsBooked && foh.StartTime >= now)
            .GroupBy(foh => foh.FacultyUserId)
            .Select(g => new { FacultyUserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.FacultyUserId, g => g.Count);

        var memberCards = users.Select(u =>
        {
            var card = new DirectoryMemberCard
            {
                UserId = u.Id,
                Email = u.Email ?? "Unknown",
                Role = u.Role
            };

            if (u.Id == currentUser.Id)
            {
                card.ConnectionState = DirectoryConnectionState.IsSelf;
            }
            else
            {
                var conn = rawConnections.FirstOrDefault(c =>
                    (c.SenderUserId == currentUser.Id && c.ReceiverUserId == u.Id) ||
                    (c.ReceiverUserId == currentUser.Id && c.SenderUserId == u.Id));

                if (conn == null)
                {
                    card.ConnectionState = DirectoryConnectionState.NotConnected;
                }
                else if (conn.Status == ConnectionStatus.Accepted)
                {
                    card.ConnectionState = DirectoryConnectionState.Connected;
                    card.ConnectionId = conn.ConnectionId;
                }
                else if (conn.Status == ConnectionStatus.Pending)
                {
                    card.ConnectionState = conn.SenderUserId == currentUser.Id
                        ? DirectoryConnectionState.PendingSent
                        : DirectoryConnectionState.PendingReceived;
                    card.ConnectionId = conn.ConnectionId;
                }
                else
                {
                    card.ConnectionState = DirectoryConnectionState.NotConnected;
                }
            }

            if (u.StudentProfile != null)
            {
                card.DepartmentName = u.StudentProfile.Department?.DepartmentName;
                card.DepartmentId = u.StudentProfile.DepartmentId;
                card.RollNumber = u.StudentProfile.RollNumber;
                card.BatchYear = u.StudentProfile.BatchYear;
                card.Bio = u.StudentProfile.Bio;
                card.Skills = u.StudentProfile.StudentSkills.Select(ss => ss.Skill.SkillName).ToList();
            }
            else if (u.FacultyProfile != null)
            {
                card.DepartmentName = u.FacultyProfile.Department?.DepartmentName;
                card.DepartmentId = u.FacultyProfile.DepartmentId;
                card.Designation = u.FacultyProfile.Designation;
                card.CabinNumber = u.FacultyProfile.CabinNumber;
                card.AvailableOfficeHoursCount = officeHourCounts.TryGetValue(u.Id, out int cnt) ? cnt : 0;
            }

            return card;
        }).ToList();

        var departments = await _deptRepo.GetAllAsync();
        var allSkills = await _context.Skills.Select(s => s.SkillName).Distinct().OrderBy(s => s).ToListAsync();

        var model = new DirectoryIndexViewModel
        {
            Members = memberCards,
            Departments = departments,
            AvailableSkills = allSkills,
            SearchQuery = search,
            SelectedRole = role,
            SelectedDepartmentId = departmentId,
            SelectedSkill = skill
        };

        return View(model);
    }

    public async Task<IActionResult> Profile(Guid id)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return Challenge();

        var targetUser = await _context.Users
            .Include(u => u.StudentProfile)
                .ThenInclude(sp => sp!.Department)
            .Include(u => u.StudentProfile)
                .ThenInclude(sp => sp!.StudentSkills)
                    .ThenInclude(ss => ss.Skill)
            .Include(u => u.FacultyProfile)
                .ThenInclude(fp => fp!.Department)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (targetUser == null) return NotFound();

        var now = DateTime.UtcNow;
        var upcomingOfficeHours = await _context.FacultyOfficeHours
            .Where(foh => foh.FacultyUserId == id && !foh.IsBooked && foh.StartTime >= now)
            .OrderBy(foh => foh.StartTime)
            .Take(5)
            .ToListAsync();

        var conn = await _connectionRepo.GetConnectionAsync(currentUser.Id, id);
        var connState = DirectoryConnectionState.NotConnected;
        if (id == currentUser.Id)
        {
            connState = DirectoryConnectionState.IsSelf;
        }
        else if (conn != null)
        {
            if (conn.Status == ConnectionStatus.Accepted) connState = DirectoryConnectionState.Connected;
            else if (conn.Status == ConnectionStatus.Pending)
                connState = conn.SenderUserId == currentUser.Id ? DirectoryConnectionState.PendingSent : DirectoryConnectionState.PendingReceived;
        }

        var model = new UserPublicProfileViewModel
        {
            User = targetUser,
            StudentProfile = targetUser.StudentProfile,
            FacultyProfile = targetUser.FacultyProfile,
            UpcomingOfficeHours = upcomingOfficeHours,
            ConnectionState = connState,
            ConnectionId = conn?.ConnectionId
        };

        return View(model);
    }
}
