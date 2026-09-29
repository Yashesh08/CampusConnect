using CampusConnect.Models;

namespace CampusConnect.Repositories;

public interface IFacultyProfileRepository
{
    Task<FacultyProfile?> GetByUserIdAsync(Guid userId);
    Task<FacultyProfile?> GetByIdAsync(Guid profileId);
    Task AddAsync(FacultyProfile profile);
    Task UpdateAsync(FacultyProfile profile);
}
