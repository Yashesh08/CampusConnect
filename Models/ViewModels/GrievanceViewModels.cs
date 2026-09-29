using CampusConnect.Models;
using CampusConnect.Models.Enums;

namespace CampusConnect.Models.ViewModels;

public class GrievanceTrackerViewModel
{
    public Grievance? SelectedGrievance { get; set; }
    public List<Grievance> MyGrievances { get; set; } = new();
    public string? SearchQuery { get; set; }
    public bool SearchAttempted { get; set; }
}

public class GrievanceOfficerDashboardViewModel
{
    public List<Grievance> AssignedGrievances { get; set; } = new();
    public List<Grievance> UnassignedDepartmentGrievances { get; set; } = new();
    public GrievanceStatus? FilterStatus { get; set; }
    public int TotalAssigned => AssignedGrievances.Count;
    public int InProgressCount => AssignedGrievances.Count(g => g.Status == GrievanceStatus.InProgress);
    public int AcknowledgedCount => AssignedGrievances.Count(g => g.Status == GrievanceStatus.Acknowledged);
    public int ResolvedCount => AssignedGrievances.Count(g => g.Status == GrievanceStatus.Resolved);
    public int OverdueCount => AssignedGrievances.Count(g => g.Status != GrievanceStatus.Resolved && g.SlaDueAt.HasValue && g.SlaDueAt.Value < DateTime.UtcNow);
}

public class GrievanceEscalationViewModel
{
    public List<Grievance> EscalatedGrievances { get; set; } = new();
    public List<Grievance> OverdueGrievances { get; set; } = new();
    public List<Grievance> CriticalGrievances { get; set; } = new();
    public List<User> AvailableOfficers { get; set; } = new();
    public int TotalEscalated => EscalatedGrievances.Count;
    public int TotalOverdue => OverdueGrievances.Count;
    public int TotalCritical => CriticalGrievances.Count;
}
