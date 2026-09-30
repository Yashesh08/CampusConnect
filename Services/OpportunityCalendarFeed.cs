using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CampusConnect.Data;
using CampusConnect.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Services;

public class OpportunityCalendarFeed : IOpportunityCalendarFeed
{
    private readonly AppDbContext _context;

    public OpportunityCalendarFeed(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<OpportunityCalendarItem>> GetCalendarEventsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
        var end = endDate ?? DateTime.UtcNow.AddMonths(3);

        var opportunities = await _context.Opportunities
            .Include(o => o.TargetDepartment)
            .Include(o => o.Organizer)
            .Where(o => o.ApprovalStatus == ApprovalStatus.Approved)
            .ToListAsync();

        var events = new List<OpportunityCalendarItem>();

        foreach (var opp in opportunities)
        {
            // Registration deadline event
            if (opp.RegistrationDeadline >= start && opp.RegistrationDeadline <= end)
            {
                events.Add(new OpportunityCalendarItem
                {
                    OpportunityId = opp.OpportunityId,
                    Title = $"[Deadline] {opp.Title}",
                    Category = opp.Category,
                    WorkMode = opp.WorkMode,
                    Date = opp.RegistrationDeadline,
                    EventType = "RegistrationDeadline",
                    DepartmentName = opp.TargetDepartment?.DepartmentName ?? "All Departments",
                    OrganizerEmail = opp.Organizer?.Email,
                    Url = $"/Opportunities/Details/{opp.OpportunityId}"
                });
            }

            // Event date (if specified)
            if (opp.EventDate.HasValue && opp.EventDate.Value >= start && opp.EventDate.Value <= end)
            {
                events.Add(new OpportunityCalendarItem
                {
                    OpportunityId = opp.OpportunityId,
                    Title = $"[Event Day] {opp.Title}",
                    Category = opp.Category,
                    WorkMode = opp.WorkMode,
                    Date = opp.EventDate.Value,
                    EventType = "EventDate",
                    DepartmentName = opp.TargetDepartment?.DepartmentName ?? "All Departments",
                    OrganizerEmail = opp.Organizer?.Email,
                    Url = $"/Opportunities/Details/{opp.OpportunityId}"
                });
            }
        }

        return events.OrderBy(e => e.Date).ToList();
    }
}
