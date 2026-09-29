using CampusConnect.Data;
using CampusConnect.Models;
using CampusConnect.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Repositories;

public class GrievanceRepository : IGrievanceRepository
{
    private readonly AppDbContext _context;

    public GrievanceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Grievance?> GetByIdAsync(Guid id)
    {
        var item = await _context.Grievances
            .Include(g => g.ComplainantUser)
            .Include(g => g.AssignedToUser)
            .Include(g => g.Department)
            .Include(g => g.Logs)
                .ThenInclude(l => l.UpdatedByUser)
            .FirstOrDefaultAsync(g => g.GrievanceId == id);

        if (item != null)
        {
            item.Logs = item.Logs.OrderBy(l => l.Timestamp).ToList();
        }

        return item;
    }

    public async Task<List<Grievance>> GetAllAsync(
        GrievanceStatus? status = null,
        GrievanceCategory? category = null,
        int? departmentId = null)
    {
        var query = _context.Grievances
            .Include(g => g.ComplainantUser)
            .Include(g => g.AssignedToUser)
            .Include(g => g.Department)
            .Include(g => g.Logs)
                .ThenInclude(l => l.UpdatedByUser)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(g => g.Status == status.Value);

        if (category.HasValue)
            query = query.Where(g => g.Category == category.Value);

        if (departmentId.HasValue)
            query = query.Where(g => g.DepartmentId == departmentId.Value);

        return await query.OrderByDescending(g => g.CreatedAt).ToListAsync();
    }

    public async Task<List<Grievance>> GetByComplainantIdAsync(Guid userId)
    {
        return await _context.Grievances
            .Include(g => g.Department)
            .Include(g => g.AssignedToUser)
            .Include(g => g.Logs)
                .ThenInclude(l => l.UpdatedByUser)
            .Where(g => g.ComplainantUserId == userId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Grievance>> GetAssignedToUserAsync(Guid userId)
    {
        return await _context.Grievances
            .Include(g => g.Department)
            .Include(g => g.ComplainantUser)
            .Include(g => g.AssignedToUser)
            .Include(g => g.Logs)
                .ThenInclude(l => l.UpdatedByUser)
            .Where(g => g.AssignedToUserId == userId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Grievance>> GetEscalatedAsync()
    {
        return await _context.Grievances
            .Include(g => g.Department)
            .Include(g => g.ComplainantUser)
            .Include(g => g.AssignedToUser)
            .Include(g => g.Logs)
                .ThenInclude(l => l.UpdatedByUser)
            .Where(g => g.Status == GrievanceStatus.Escalated)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Grievance>> GetOverdueAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.Grievances
            .Include(g => g.Department)
            .Include(g => g.ComplainantUser)
            .Include(g => g.AssignedToUser)
            .Include(g => g.Logs)
                .ThenInclude(l => l.UpdatedByUser)
            .Where(g => g.Status != GrievanceStatus.Resolved && g.SlaDueAt.HasValue && g.SlaDueAt.Value < now)
            .OrderBy(g => g.SlaDueAt)
            .ToListAsync();
    }

    public async Task AddAsync(Grievance grievance, Guid? createdByUserId = null)
    {
        await _context.Grievances.AddAsync(grievance);

        var submitterId = createdByUserId ?? grievance.ComplainantUserId;
        if (submitterId.HasValue && submitterId.Value != Guid.Empty)
        {
            var log = new GrievanceLog
            {
                GrievanceId = grievance.GrievanceId,
                UpdatedByUserId = submitterId.Value,
                StatusChangedTo = GrievanceStatus.Submitted.ToString(),
                ResolutionNote = grievance.IsAnonymous ? "Grievance ticket submitted anonymously." : "Grievance ticket submitted by student.",
                Timestamp = DateTime.UtcNow
            };
            await _context.GrievanceLogs.AddAsync(log);
        }

        await _context.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(Guid grievanceId, GrievanceStatus newStatus, Guid updatedByUserId, string? note = null)
    {
        var grievance = await _context.Grievances.FindAsync(grievanceId);
        if (grievance == null) return;

        var oldStatus = grievance.Status;
        grievance.Status = newStatus;
        if (newStatus == GrievanceStatus.Resolved)
        {
            grievance.ResolvedAt = DateTime.UtcNow;
        }
        else if (oldStatus == GrievanceStatus.Resolved && newStatus != GrievanceStatus.Resolved)
        {
            grievance.ResolvedAt = null;
        }

        var defaultNote = newStatus switch
        {
            GrievanceStatus.Acknowledged => "Grievance acknowledged and queued for officer investigation.",
            GrievanceStatus.InProgress   => "Work initiated on grievance investigation/resolution.",
            GrievanceStatus.Resolved     => "Grievance marked as resolved.",
            GrievanceStatus.Escalated    => "Grievance escalated to Head of Department for intervention.",
            _                            => $"Status updated to {newStatus}."
        };

        var log = new GrievanceLog
        {
            GrievanceId = grievanceId,
            UpdatedByUserId = updatedByUserId,
            StatusChangedTo = newStatus.ToString(),
            ResolutionNote = string.IsNullOrWhiteSpace(note) ? defaultNote : note.Trim(),
            Timestamp = DateTime.UtcNow
        };

        await _context.GrievanceLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }

    public async Task AssignAsync(Guid grievanceId, Guid assignedToUserId, Guid updatedByUserId, string? note = null)
    {
        var grievance = await _context.Grievances.FindAsync(grievanceId);
        if (grievance == null) return;

        grievance.AssignedToUserId = assignedToUserId;
        if (grievance.Status == GrievanceStatus.Submitted)
        {
            grievance.Status = GrievanceStatus.Acknowledged;
        }

        var log = new GrievanceLog
        {
            GrievanceId = grievanceId,
            UpdatedByUserId = updatedByUserId,
            StatusChangedTo = grievance.Status.ToString(),
            ResolutionNote = string.IsNullOrWhiteSpace(note) ? "Assigned officer to grievance." : note.Trim(),
            Timestamp = DateTime.UtcNow
        };

        await _context.GrievanceLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }
}
