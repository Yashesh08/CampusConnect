using CampusConnect.Data;
using CampusConnect.Models;
using CampusConnect.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Repositories;

public class ApplicationRepository : IApplicationRepository
{
    private readonly AppDbContext _context;

    public ApplicationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Application?> GetByIdAsync(Guid id)
    {
        return await _context.Applications
            .Include(a => a.Opportunity)
                .ThenInclude(o => o.Organizer)
            .Include(a => a.Student)
            .FirstOrDefaultAsync(a => a.ApplicationId == id);
    }

    public async Task<List<Application>> GetByStudentIdAsync(Guid studentId)
    {
        return await _context.Applications
            .Include(a => a.Opportunity)
                .ThenInclude(o => o.Organizer)
            .Include(a => a.Opportunity)
                .ThenInclude(o => o.TargetDepartment)
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync();
    }

    public async Task<List<Application>> GetByOpportunityIdAsync(Guid opportunityId)
    {
        return await _context.Applications
            .Include(a => a.Student)
            .Where(a => a.OpportunityId == opportunityId)
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync();
    }

    public async Task<bool> HasAlreadyAppliedAsync(Guid opportunityId, Guid studentId)
    {
        return await _context.Applications
            .AnyAsync(a => a.OpportunityId == opportunityId && a.StudentId == studentId);
    }

    public async Task AddAsync(Application application)
    {
        await _context.Applications.AddAsync(application);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(Guid applicationId, ApplicationStatus status, string? remarks = null)
    {
        var app = await _context.Applications.FindAsync(applicationId);
        if (app != null)
        {
            app.Status = status;
            if (remarks != null)
            {
                app.OrganizerRemarks = remarks;
            }
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        var app = await _context.Applications.FindAsync(id);
        if (app != null)
        {
            _context.Applications.Remove(app);
            await _context.SaveChangesAsync();
        }
    }
}
