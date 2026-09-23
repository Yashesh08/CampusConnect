using CampusConnect.Data;
using CampusConnect.Models;
using CampusConnect.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Repositories;

public class OpportunityRepository : IOpportunityRepository
{
    private readonly AppDbContext _context;

    public OpportunityRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Opportunity?> GetByIdAsync(Guid id)
    {
        return await _context.Opportunities
            .Include(o => o.Organizer)
            .Include(o => o.TargetDepartment)
            .Include(o => o.RequiredSkills)
                .ThenInclude(os => os.Skill)
            .Include(o => o.Applications)
                .ThenInclude(a => a.Student)
            .FirstOrDefaultAsync(o => o.OpportunityId == id);
    }

    public async Task<List<Opportunity>> GetAllAsync(OpportunityCategory? category = null, WorkMode? mode = null, ApprovalStatus? status = ApprovalStatus.Approved, string? search = null)
    {
        var query = _context.Opportunities
            .Include(o => o.Organizer)
            .Include(o => o.TargetDepartment)
            .Include(o => o.RequiredSkills)
                .ThenInclude(os => os.Skill)
            .Include(o => o.Applications)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(o => o.ApprovalStatus == status.Value);
        }

        if (category.HasValue)
        {
            query = query.Where(o => o.Category == category.Value);
        }

        if (mode.HasValue)
        {
            query = query.Where(o => o.WorkMode == mode.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(o => o.Title.ToLower().Contains(searchLower) || o.Description.ToLower().Contains(searchLower));
        }

        return await query.OrderByDescending(o => o.RegistrationDeadline).ToListAsync();
    }

    public async Task<List<Opportunity>> GetByOrganizerIdAsync(Guid organizerId)
    {
        return await _context.Opportunities
            .Include(o => o.TargetDepartment)
            .Include(o => o.RequiredSkills)
                .ThenInclude(os => os.Skill)
            .Include(o => o.Applications)
                .ThenInclude(a => a.Student)
            .Where(o => o.OrganizerId == organizerId)
            .OrderByDescending(o => o.RegistrationDeadline)
            .ToListAsync();
    }

    public async Task<List<Opportunity>> GetPendingApprovalsAsync()
    {
        return await _context.Opportunities
            .Include(o => o.Organizer)
            .Include(o => o.TargetDepartment)
            .Include(o => o.RequiredSkills)
                .ThenInclude(os => os.Skill)
            .Where(o => o.ApprovalStatus == ApprovalStatus.PendingReview)
            .OrderByDescending(o => o.RegistrationDeadline)
            .ToListAsync();
    }

    public async Task AddAsync(Opportunity opportunity, List<int>? requiredSkillIds = null)
    {
        await _context.Opportunities.AddAsync(opportunity);
        
        if (requiredSkillIds != null && requiredSkillIds.Any())
        {
            foreach (var skillId in requiredSkillIds)
            {
                _context.OpportunitySkills.Add(new OpportunitySkill
                {
                    OpportunityId = opportunity.OpportunityId,
                    SkillId = skillId
                });
            }
        }
        
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Opportunity opportunity, List<int>? requiredSkillIds = null)
    {
        _context.Opportunities.Update(opportunity);

        if (requiredSkillIds != null)
        {
            var existingSkills = await _context.OpportunitySkills
                .Where(os => os.OpportunityId == opportunity.OpportunityId)
                .ToListAsync();

            _context.OpportunitySkills.RemoveRange(existingSkills);

            foreach (var skillId in requiredSkillIds)
            {
                _context.OpportunitySkills.Add(new OpportunitySkill
                {
                    OpportunityId = opportunity.OpportunityId,
                    SkillId = skillId
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(Guid opportunityId, ApprovalStatus status)
    {
        var opp = await _context.Opportunities.FindAsync(opportunityId);
        if (opp != null)
        {
            opp.ApprovalStatus = status;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        var opp = await _context.Opportunities.FindAsync(id);
        if (opp != null)
        {
            _context.Opportunities.Remove(opp);
            await _context.SaveChangesAsync();
        }
    }
}
