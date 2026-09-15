using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Repositories;

public class StudentProfileRepository : IStudentProfileRepository
{
    private readonly AppDbContext _context;
    public StudentProfileRepository(AppDbContext context) => _context = context;

    public async Task<List<StudentProfile>> GetAllAsync() =>
        await _context.StudentProfiles.AsNoTracking().Include(p => p.Department).ToListAsync();

    public async Task<StudentProfile?> GetByIdAsync(Guid id) =>
        await _context.StudentProfiles.Include(p => p.Department).FirstOrDefaultAsync(p => p.ProfileId == id);

    public async Task AddAsync(StudentProfile profile)
    {
        _context.StudentProfiles.Add(profile);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(StudentProfile profile)
    {
        _context.StudentProfiles.Update(profile);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var p = await _context.StudentProfiles.FindAsync(id);
        if (p != null)
        {
            _context.StudentProfiles.Remove(p);
            await _context.SaveChangesAsync();
        }
    }
}
