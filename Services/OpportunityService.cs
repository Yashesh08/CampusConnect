using CampusConnect.Models;
using CampusConnect.Models.Enums;
using CampusConnect.Models.ViewModels;
using CampusConnect.Repositories;

namespace CampusConnect.Services;

public class OpportunityService : IOpportunityService
{
    private readonly IOpportunityRepository _opportunityRepo;
    private readonly IDepartmentRepository _departmentRepo;
    private readonly ISavedOpportunityRepository _savedRepo;

    public OpportunityService(
        IOpportunityRepository opportunityRepo,
        IDepartmentRepository departmentRepo,
        ISavedOpportunityRepository savedRepo)
    {
        _opportunityRepo = opportunityRepo;
        _departmentRepo = departmentRepo;
        _savedRepo = savedRepo;
    }

    public async Task<OpportunityFilterViewModel> GetFilteredOpportunitiesAsync(
        string? search,
        OpportunityCategory? category,
        WorkMode? mode,
        int? departmentId,
        string? sortBy,
        int page,
        int pageSize,
        Guid? studentId = null)
    {
        var allApproved = await _opportunityRepo.GetAllAsync(category, mode, ApprovalStatus.Approved, search);

        if (departmentId.HasValue && departmentId.Value > 0)
        {
            allApproved = allApproved
                .Where(o => o.TargetDepartmentId == departmentId.Value || o.TargetDepartmentId == null)
                .ToList();
        }

        // Sorting
        var sorted = sortBy?.ToLower() switch
        {
            "deadline_asc" => allApproved.OrderBy(o => o.RegistrationDeadline).ToList(),
            "deadline_desc" => allApproved.OrderByDescending(o => o.RegistrationDeadline).ToList(),
            "title_asc" => allApproved.OrderBy(o => o.Title).ToList(),
            "title_desc" => allApproved.OrderByDescending(o => o.Title).ToList(),
            "newest" => allApproved.OrderByDescending(o => o.OpportunityId).ToList(),
            _ => allApproved.OrderBy(o => o.RegistrationDeadline).ToList() // Default upcoming / deadline sorting
        };

        int totalItems = sorted.Count;
        int currentPage = Math.Max(1, page);
        int validPageSize = pageSize > 0 ? pageSize : 6;

        var pagedOpportunities = sorted
            .Skip((currentPage - 1) * validPageSize)
            .Take(validPageSize)
            .ToList();

        var departments = await _departmentRepo.GetAllAsync();

        List<Guid> savedIds = new();
        if (studentId.HasValue && studentId.Value != Guid.Empty)
        {
            var saved = await _savedRepo.GetByStudentIdAsync(studentId.Value);
            savedIds = saved.Select(s => s.OpportunityId).ToList();
        }

        return new OpportunityFilterViewModel
        {
            SearchQuery = search,
            Category = category,
            WorkMode = mode,
            DepartmentId = departmentId,
            SortBy = sortBy,
            Page = currentPage,
            PageSize = validPageSize,
            TotalItems = totalItems,
            Opportunities = pagedOpportunities,
            Departments = departments,
            SavedOpportunityIds = savedIds
        };
    }

    public async Task<Opportunity?> GetOpportunityByIdAsync(Guid id)
    {
        return await _opportunityRepo.GetByIdAsync(id);
    }

    public async Task<Opportunity> CreateOpportunityAsync(CreateOpportunityViewModel model, Guid organizerId)
    {
        var opportunity = new Opportunity
        {
            OpportunityId = Guid.NewGuid(),
            OrganizerId = organizerId,
            Title = model.Title,
            Description = model.Description,
            Category = model.Category,
            TargetDepartmentId = model.TargetDepartmentId,
            WorkMode = model.WorkMode,
            StipendSalary = model.StipendSalary,
            RegistrationDeadline = model.RegistrationDeadline,
            EventDate = model.EventDate,
            Capacity = model.Capacity,
            ApprovalStatus = ApprovalStatus.PendingReview
        };

        await _opportunityRepo.AddAsync(opportunity, model.SelectedSkillIds);
        return opportunity;
    }

    public async Task<EditOpportunityViewModel?> GetEditViewModelAsync(Guid opportunityId)
    {
        var opp = await _opportunityRepo.GetByIdAsync(opportunityId);
        if (opp == null) return null;

        return new EditOpportunityViewModel
        {
            OpportunityId = opp.OpportunityId,
            Title = opp.Title,
            Description = opp.Description,
            Category = opp.Category,
            TargetDepartmentId = opp.TargetDepartmentId,
            WorkMode = opp.WorkMode,
            StipendSalary = opp.StipendSalary,
            RegistrationDeadline = opp.RegistrationDeadline,
            EventDate = opp.EventDate,
            Capacity = opp.Capacity,
            SelectedSkillIds = opp.RequiredSkills.Select(s => s.SkillId).ToList()
        };
    }

    public async Task<bool> UpdateOpportunityAsync(Guid opportunityId, EditOpportunityViewModel model, Guid userId, bool isPrivileged)
    {
        var existing = await _opportunityRepo.GetByIdAsync(opportunityId);
        if (existing == null) return false;

        if (existing.OrganizerId != userId && !isPrivileged)
        {
            return false;
        }

        existing.Title = model.Title;
        existing.Description = model.Description;
        existing.Category = model.Category;
        existing.TargetDepartmentId = model.TargetDepartmentId;
        existing.WorkMode = model.WorkMode;
        existing.StipendSalary = model.StipendSalary;
        existing.RegistrationDeadline = model.RegistrationDeadline;
        existing.EventDate = model.EventDate;
        existing.Capacity = model.Capacity;

        await _opportunityRepo.UpdateAsync(existing, model.SelectedSkillIds);
        return true;
    }

    public async Task<bool> DeleteOpportunityAsync(Guid opportunityId, Guid userId, bool isPrivileged)
    {
        var existing = await _opportunityRepo.GetByIdAsync(opportunityId);
        if (existing == null) return false;

        if (existing.OrganizerId != userId && !isPrivileged)
        {
            return false;
        }

        await _opportunityRepo.DeleteAsync(opportunityId);
        return true;
    }

    public async Task<(bool Success, string Message)> ApproveOpportunityAsync(Guid opportunityId, Guid userId, bool isAdmin, bool isHod, int? userDepartmentId)
    {
        var opp = await _opportunityRepo.GetByIdAsync(opportunityId);
        if (opp == null) return (false, "Opportunity not found.");

        if (opp.ApprovalStatus != ApprovalStatus.PendingReview)
        {
            return (false, $"Cannot approve opportunity with status '{opp.ApprovalStatus}'. Only pending review opportunities can be approved.");
        }

        if (isHod && !isAdmin)
        {
            if (userDepartmentId == null || opp.TargetDepartmentId == null || opp.TargetDepartmentId != userDepartmentId)
            {
                return (false, "Forbidden: HOD can only approve opportunities for their department.");
            }
        }

        await _opportunityRepo.UpdateStatusAsync(opportunityId, ApprovalStatus.Approved);
        return (true, "Opportunity approved and published successfully!");
    }

    public async Task<(bool Success, string Message)> RejectOpportunityAsync(Guid opportunityId, Guid userId, bool isAdmin, bool isHod, int? userDepartmentId)
    {
        var opp = await _opportunityRepo.GetByIdAsync(opportunityId);
        if (opp == null) return (false, "Opportunity not found.");

        if (opp.ApprovalStatus == ApprovalStatus.Rejected)
        {
            return (false, "Opportunity is already rejected.");
        }

        if (isHod && !isAdmin)
        {
            if (userDepartmentId == null || opp.TargetDepartmentId == null || opp.TargetDepartmentId != userDepartmentId)
            {
                return (false, "Forbidden: HOD can only reject opportunities for their department.");
            }
        }

        await _opportunityRepo.UpdateStatusAsync(opportunityId, ApprovalStatus.Rejected);
        return (true, "Opportunity has been rejected.");
    }

    public async Task<List<Opportunity>> GetPendingApprovalsAsync()
    {
        return await _opportunityRepo.GetPendingApprovalsAsync();
    }
}
