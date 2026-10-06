using CampusConnect.Models.Enums;

namespace CampusConnect.Models.ViewModels;

public class OpportunityFilterViewModel
{
    public string? SearchQuery { get; set; }
    public OpportunityCategory? Category { get; set; }
    public WorkMode? WorkMode { get; set; }
    public int? DepartmentId { get; set; }
    public string? SortBy { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 6;
    public int TotalItems { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalItems / PageSize) : 0;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public List<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
    public List<Department> Departments { get; set; } = new List<Department>();
    public List<Guid> SavedOpportunityIds { get; set; } = new List<Guid>();
}
