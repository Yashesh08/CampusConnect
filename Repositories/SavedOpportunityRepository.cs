using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Repositories;

public class SavedOpportunityRepository : ISavedOpportunityRepository
{
    private readonly AppDbContext _context;

    public SavedOpportunityRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SavedOpportunity?> GetAsync(Guid studentId, Guid opportunityId)
    {
        return await _context.SavedOpportunities
            .Include(so => so.Opportunity)
                .ThenInclude(o => o.Organizer)
            .Include(so => so.Opportunity)
                .ThenInclude(o => o.TargetDepartment)
            .FirstOrDefaultAsync(so => so.StudentId == studentId && so.OpportunityId == opportunityId);
    }

    public async Task<bool> IsSavedAsync(Guid studentId, Guid opportunityId)
    {
        return await _context.SavedOpportunities
            .AnyAsync(so => so.StudentId == studentId && so.OpportunityId == opportunityId);
    }

    public async Task AddAsync(SavedOpportunity savedOpportunity)
    {
        bool exists = await _context.SavedOpportunities
            .AnyAsync(so => so.StudentId == savedOpportunity.StudentId && so.OpportunityId == savedOpportunity.OpportunityId);

        if (!exists)
        {
            await _context.SavedOpportunities.AddAsync(savedOpportunity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveAsync(Guid studentId, Guid opportunityId)
    {
        var saved = await _context.SavedOpportunities
            .FirstOrDefaultAsync(so => so.StudentId == studentId && so.OpportunityId == opportunityId);

        if (saved != null)
        {
            _context.SavedOpportunities.Remove(saved);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<SavedOpportunity>> GetByStudentIdAsync(Guid studentId)
    {
        return await _context.SavedOpportunities
            .Include(so => so.Opportunity)
                .ThenInclude(o => o.Organizer)
            .Include(so => so.Opportunity)
                .ThenInclude(o => o.TargetDepartment)
            .Where(so => so.StudentId == studentId)
            .OrderByDescending(so => so.SavedAt)
            .ToListAsync();
    }
}
