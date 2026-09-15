using CampusConnect.Models;

namespace CampusConnect.Repositories;

public interface IStudentProfileRepository
{
    Task<List<StudentProfile>> GetAllAsync();
    Task<StudentProfile?> GetByIdAsync(Guid id);
    Task AddAsync(StudentProfile profile);
    Task UpdateAsync(StudentProfile profile);
    Task DeleteAsync(Guid id);
}
