using CampusConnect.Models.Enums;

namespace CampusConnect.Models.ViewModels;

public class OpportunityFilterViewModel
{
    public string? SearchQuery { get; set; }
    public OpportunityCategory? Category { get; set; }
    public WorkMode? WorkMode { get; set; }
    public int? DepartmentId { get; set; }
    public List<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
    public List<Department> Departments { get; set; } = new List<Department>();
}
