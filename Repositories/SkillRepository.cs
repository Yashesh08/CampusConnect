using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Repositories;

public class SkillRepository : ISkillRepository
{
    private readonly AppDbContext _context;
    public SkillRepository(AppDbContext context) => _context = context;

    public async Task<List<Skill>> GetAllAsync() =>
        await _context.Skills.AsNoTracking().ToListAsync();

    public async Task<Skill?> GetByIdAsync(int id) =>
        await _context.Skills.FindAsync(id);

    public async Task AddAsync(Skill skill)
    {
        _context.Skills.Add(skill);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Skill skill)
    {
        _context.Skills.Update(skill);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var s = await _context.Skills.FindAsync(id);
        if (s != null)
        {
            _context.Skills.Remove(s);
            await _context.SaveChangesAsync();
        }
    }
}
