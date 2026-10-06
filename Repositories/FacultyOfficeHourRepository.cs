using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Repositories;

public class FacultyOfficeHourRepository : IFacultyOfficeHourRepository
{
    private readonly AppDbContext _context;

    public FacultyOfficeHourRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<FacultyOfficeHour>> GetSlotsByFacultyAsync(Guid facultyUserId, bool includePast = false)
    {
        var now = DateTime.UtcNow;
        var query = _context.FacultyOfficeHours
            .Include(foh => foh.FacultyUser)
                .ThenInclude(u => u.FacultyProfile)
                    .ThenInclude(fp => fp!.Department)
            .Include(foh => foh.BookedByStudent)
                .ThenInclude(sp => sp!.User)
            .Include(foh => foh.BookedByStudent)
                .ThenInclude(sp => sp!.Department)
            .Where(foh => foh.FacultyUserId == facultyUserId);

        if (!includePast)
        {
            query = query.Where(foh => foh.EndTime >= now);
        }

        return await query.OrderBy(foh => foh.StartTime).ToListAsync();
    }

    public async Task<List<FacultyOfficeHour>> GetUpcomingAvailableSlotsAsync(int? departmentId = null, Guid? facultyUserId = null)
    {
        var now = DateTime.UtcNow;
        var query = _context.FacultyOfficeHours
            .Include(foh => foh.FacultyUser)
                .ThenInclude(u => u.FacultyProfile)
                    .ThenInclude(fp => fp!.Department)
            .Where(foh => !foh.IsBooked && foh.StartTime >= now);

        if (facultyUserId.HasValue)
        {
            query = query.Where(foh => foh.FacultyUserId == facultyUserId.Value);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(foh => foh.FacultyUser.FacultyProfile != null && foh.FacultyUser.FacultyProfile.DepartmentId == departmentId.Value);
        }

        return await query.OrderBy(foh => foh.StartTime).ToListAsync();
    }

    public async Task<List<FacultyOfficeHour>> GetBookedSlotsByStudentAsync(Guid studentProfileId)
    {
        return await _context.FacultyOfficeHours
            .Include(foh => foh.FacultyUser)
                .ThenInclude(u => u.FacultyProfile)
                    .ThenInclude(fp => fp!.Department)
            .Where(foh => foh.IsBooked && foh.BookedByStudentId == studentProfileId)
            .OrderBy(foh => foh.StartTime)
            .ToListAsync();
    }

    public async Task<FacultyOfficeHour?> GetSlotByIdAsync(Guid slotId)
    {
        return await _context.FacultyOfficeHours
            .Include(foh => foh.FacultyUser)
                .ThenInclude(u => u.FacultyProfile)
                    .ThenInclude(fp => fp!.Department)
            .Include(foh => foh.BookedByStudent)
                .ThenInclude(sp => sp!.User)
            .Include(foh => foh.BookedByStudent)
                .ThenInclude(sp => sp!.Department)
            .FirstOrDefaultAsync(foh => foh.SlotId == slotId);
    }

    public async Task<(bool Success, string Message)> AddSlotAsync(FacultyOfficeHour slot)
    {
        if (slot.StartTime >= slot.EndTime)
            return (false, "Slot start time must be before end time.");

        if (slot.StartTime < DateTime.UtcNow.AddMinutes(-5))
            return (false, "Slot cannot be in the past.");

        // Check for overlapping slots by the same faculty
        bool hasOverlap = await _context.FacultyOfficeHours
            .AnyAsync(s => s.FacultyUserId == slot.FacultyUserId &&
                           slot.StartTime < s.EndTime &&
                           slot.EndTime > s.StartTime);

        if (hasOverlap)
            return (false, "You already have another office hours slot scheduled during this time window.");

        await _context.FacultyOfficeHours.AddAsync(slot);
        await _context.SaveChangesAsync();
        return (true, "Office hours slot created successfully.");
    }

    public async Task<(bool Success, string Message)> UpdateSlotAsync(Guid slotId, DateTime startTime, DateTime endTime, Guid facultyUserId)
    {
        var slot = await _context.FacultyOfficeHours.FindAsync(slotId);
        if (slot == null || slot.FacultyUserId != facultyUserId)
            return (false, "Slot not found or unauthorized.");

        if (slot.IsBooked)
            return (false, "Cannot edit a slot that has already been booked by a student. Please cancel the booking first.");

        if (startTime >= endTime)
            return (false, "Start time must be before end time.");

        // Check for overlapping slots excluding this slot
        bool hasOverlap = await _context.FacultyOfficeHours
            .AnyAsync(s => s.SlotId != slotId &&
                           s.FacultyUserId == facultyUserId &&
                           startTime < s.EndTime &&
                           endTime > s.StartTime);

        if (hasOverlap)
            return (false, "Another slot already overlaps with this new time window.");

        slot.StartTime = startTime;
        slot.EndTime = endTime;
        await _context.SaveChangesAsync();
        return (true, "Slot updated successfully.");
    }

    public async Task<(bool Success, string Message)> DeleteSlotAsync(Guid slotId, Guid facultyUserId)
    {
        var slot = await _context.FacultyOfficeHours.FindAsync(slotId);
        if (slot == null || slot.FacultyUserId != facultyUserId)
            return (false, "Slot not found or unauthorized.");

        _context.FacultyOfficeHours.Remove(slot);
        await _context.SaveChangesAsync();
        return (true, "Slot deleted successfully.");
    }

    public async Task<(bool Success, string Message)> BookSlotAsync(Guid slotId, Guid studentProfileId)
    {
        var slot = await _context.FacultyOfficeHours.FindAsync(slotId);
        if (slot == null)
            return (false, "Office hour slot not found.");

        if (slot.StartTime < DateTime.UtcNow)
            return (false, "Cannot book a slot that has already passed.");

        // Double-booking check 1: Is this slot already booked?
        if (slot.IsBooked)
            return (false, "This office hour slot has already been booked by another student.");

        // Double-booking check 2: Does the student already have an overlapping booking at this time?
        bool studentHasConflict = await _context.FacultyOfficeHours
            .AnyAsync(s => s.IsBooked &&
                           s.BookedByStudentId == studentProfileId &&
                           slot.StartTime < s.EndTime &&
                           slot.EndTime > s.StartTime);

        if (studentHasConflict)
            return (false, "Double-booking prevention: You already have another office hour appointment booked during this time.");

        slot.IsBooked = true;
        slot.BookedByStudentId = studentProfileId;
        await _context.SaveChangesAsync();
        return (true, "Office hours slot booked successfully!");
    }

    public async Task<(bool Success, string Message)> CancelBookingAsync(Guid slotId, Guid currentUserId, bool isFaculty)
    {
        var slot = await _context.FacultyOfficeHours
            .Include(s => s.BookedByStudent)
            .FirstOrDefaultAsync(s => s.SlotId == slotId);

        if (slot == null || !slot.IsBooked)
            return (false, "Booking not found or slot is not currently booked.");

        // Check permission: either the faculty owner or the student who booked can cancel
        bool canCancel = isFaculty
            ? slot.FacultyUserId == currentUserId
            : slot.BookedByStudent?.UserId == currentUserId;

        if (!canCancel)
            return (false, "You are not authorized to cancel this booking.");

        slot.IsBooked = false;
        slot.BookedByStudentId = null;
        await _context.SaveChangesAsync();
        return (true, "Booking cancelled successfully. The slot is now open for booking.");
    }
}
