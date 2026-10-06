using CampusConnect.Models;

namespace CampusConnect.Repositories;

public interface IAnnouncementRepository
{
    Task<List<Announcement>> GetAnnouncementsAsync(int? departmentId = null, bool collegeWideOnly = false);
    Task<Announcement?> GetByIdAsync(Guid id);
    Task AddAsync(Announcement announcement);
    Task UpdateAsync(Announcement announcement);
    Task DeleteAsync(Guid id);
}
