using CampusConnect.Models;

namespace CampusConnect.Repositories;

public interface IFacultyOfficeHourRepository
{
    Task<List<FacultyOfficeHour>> GetSlotsByFacultyAsync(Guid facultyUserId, bool includePast = false);
    Task<List<FacultyOfficeHour>> GetUpcomingAvailableSlotsAsync(int? departmentId = null, Guid? facultyUserId = null);
    Task<List<FacultyOfficeHour>> GetBookedSlotsByStudentAsync(Guid studentProfileId);
    Task<FacultyOfficeHour?> GetSlotByIdAsync(Guid slotId);
    Task<(bool Success, string Message)> AddSlotAsync(FacultyOfficeHour slot);
    Task<(bool Success, string Message)> UpdateSlotAsync(Guid slotId, DateTime startTime, DateTime endTime, Guid facultyUserId);
    Task<(bool Success, string Message)> DeleteSlotAsync(Guid slotId, Guid facultyUserId);
    Task<(bool Success, string Message)> BookSlotAsync(Guid slotId, Guid studentProfileId);
    Task<(bool Success, string Message)> CancelBookingAsync(Guid slotId, Guid currentUserId, bool isFaculty);
}
