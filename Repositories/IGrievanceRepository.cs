using CampusConnect.Models;
using CampusConnect.Models.Enums;

namespace CampusConnect.Repositories;

public interface IGrievanceRepository
{
    Task<Grievance?> GetByIdAsync(Guid id);
    Task<List<Grievance>> GetAllAsync(GrievanceStatus? status = null, GrievanceCategory? category = null, int? departmentId = null);
    Task<List<Grievance>> GetByComplainantIdAsync(Guid userId);
    Task<List<Grievance>> GetAssignedToUserAsync(Guid userId);
    Task<List<Grievance>> GetEscalatedAsync();
    Task<List<Grievance>> GetOverdueAsync();
    Task AddAsync(Grievance grievance, Guid? createdByUserId = null);
    Task UpdateStatusAsync(Guid grievanceId, GrievanceStatus newStatus, Guid updatedByUserId, string? note = null);
    Task AssignAsync(Guid grievanceId, Guid assignedToUserId, Guid updatedByUserId, string? note = null);
}