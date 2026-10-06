using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Repositories;

public class AnnouncementRepository : IAnnouncementRepository
{
    private readonly AppDbContext _context;

    public AnnouncementRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Announcement>> GetAnnouncementsAsync(int? departmentId = null, bool collegeWideOnly = false)
    {
        var query = _context.Announcements
            .Include(a => a.AuthorUser)
                .ThenInclude(u => u.FacultyProfile)
            .Include(a => a.Department)
            .AsQueryable();

        if (collegeWideOnly)
        {
            query = query.Where(a => a.DepartmentId == null);
        }
        else if (departmentId.HasValue)
        {
            // Department-specific + College-wide
            query = query.Where(a => a.DepartmentId == null || a.DepartmentId == departmentId.Value);
        }

        return await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
    }

    public async Task<Announcement?> GetByIdAsync(Guid id)
    {
        return await _context.Announcements
            .Include(a => a.AuthorUser)
                .ThenInclude(u => u.FacultyProfile)
            .Include(a => a.Department)
            .FirstOrDefaultAsync(a => a.AnnouncementId == id);
    }

    public async Task AddAsync(Announcement announcement)
    {
        await _context.Announcements.AddAsync(announcement);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Announcement announcement)
    {
        _context.Announcements.Update(announcement);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var ann = await _context.Announcements.FindAsync(id);
        if (ann != null)
        {
            _context.Announcements.Remove(ann);
            await _context.SaveChangesAsync();
        }
    }
}
