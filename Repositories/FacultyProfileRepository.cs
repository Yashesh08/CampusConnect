using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Repositories;

public class FacultyProfileRepository : IFacultyProfileRepository
{
    private readonly AppDbContext _context;

    public FacultyProfileRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<FacultyProfile?> GetByUserIdAsync(Guid userId)
    {
        return await _context.FacultyProfiles
            .Include(p => p.Department)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<FacultyProfile?> GetByIdAsync(Guid profileId)
    {
        return await _context.FacultyProfiles
            .Include(p => p.Department)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.FacultyProfileId == profileId);
    }

    public async Task AddAsync(FacultyProfile profile)
    {
        await _context.FacultyProfiles.AddAsync(profile);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(FacultyProfile profile)
    {
        _context.FacultyProfiles.Update(profile);
        await _context.SaveChangesAsync();
    }
}
