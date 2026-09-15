using CampusConnect.Models;

namespace CampusConnect.Repositories;

public interface ISkillRepository
{
    Task<List<Skill>> GetAllAsync();
    Task<Skill?> GetByIdAsync(int id);
    Task AddAsync(Skill skill);
    Task UpdateAsync(Skill skill);
    Task DeleteAsync(int id);
}
