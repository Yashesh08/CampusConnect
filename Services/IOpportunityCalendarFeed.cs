using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CampusConnect.Models.Enums;

namespace CampusConnect.Services;

public class OpportunityCalendarItem
{
    public Guid OpportunityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public OpportunityCategory Category { get; set; }
    public WorkMode WorkMode { get; set; }
    public DateTime Date { get; set; }
    public string EventType { get; set; } = string.Empty; // "RegistrationDeadline" or "EventDate"
    public string? DepartmentName { get; set; }
    public string? OrganizerEmail { get; set; }
    public string Url { get; set; } = string.Empty;
}

public interface IOpportunityCalendarFeed
{
    Task<List<OpportunityCalendarItem>> GetCalendarEventsAsync(DateTime? startDate = null, DateTime? endDate = null);
}
