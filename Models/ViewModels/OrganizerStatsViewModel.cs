using CampusConnect.Models;
using CampusConnect.Models.Enums;

namespace CampusConnect.Models.ViewModels;

public class OrganizerStatsViewModel
{
    public int TotalOpportunities { get; set; }
    public int ActiveOpportunities { get; set; }
    public int PendingOpportunities { get; set; }
    public int TotalApplications { get; set; }
    public int ShortlistedApplications { get; set; }
    public int SelectedApplications { get; set; }
    public int RejectedApplications { get; set; }
    public double AcceptanceRate { get; set; }
    public List<Opportunity> ClosingSoon { get; set; } = new();
    public Dictionary<OpportunityCategory, int> CategoryBreakdown { get; set; } = new();
}
