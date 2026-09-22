using CampusConnect.Models;

namespace CampusConnect.Repositories;

public interface IStudentProfileRepository
{
    Task<List<StudentProfile>> GetAllAsync();
    Task<StudentProfile?> GetByIdAsync(Guid id);
    Task<StudentProfile?> GetByUserIdAsync(Guid userId);
    Task AddAsync(StudentProfile profile);
    Task UpdateAsync(StudentProfile profile);
    Task DeleteAsync(Guid id);
    Task AddSkillAsync(StudentSkill studentSkill);
    Task RemoveSkillAsync(int studentSkillId);
}
