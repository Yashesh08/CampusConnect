using CampusConnect.Models;
using CampusConnect.Services;

namespace CampusConnect.Models.ViewModels;

public class DashboardOpportunityMatch
{
    public Opportunity Opportunity { get; set; } = null!;
    public MatchResult MatchResult { get; set; } = null!;
}
