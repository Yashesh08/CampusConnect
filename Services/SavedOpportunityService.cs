using CampusConnect.Models;
using CampusConnect.Repositories;

namespace CampusConnect.Services;

public class SavedOpportunityService : ISavedOpportunityService
{
    private readonly ISavedOpportunityRepository _savedRepo;
    private readonly IOpportunityRepository _opportunityRepo;

    public SavedOpportunityService(
        ISavedOpportunityRepository savedRepo,
        IOpportunityRepository opportunityRepo)
    {
        _savedRepo = savedRepo;
        _opportunityRepo = opportunityRepo;
    }

    public async Task<(bool Success, string Message)> SaveOpportunityAsync(Guid studentProfileId, Guid opportunityId)
    {
        var opp = await _opportunityRepo.GetByIdAsync(opportunityId);
        if (opp == null)
        {
            return (false, "Opportunity not found.");
        }

        bool alreadySaved = await _savedRepo.IsSavedAsync(studentProfileId, opportunityId);
        if (alreadySaved)
        {
            return (true, "Opportunity is already saved.");
        }

        var saved = new SavedOpportunity
        {
            SavedOpportunityId = Guid.NewGuid(),
            StudentId = studentProfileId,
            OpportunityId = opportunityId,
            SavedAt = DateTime.UtcNow
        };

        await _savedRepo.AddAsync(saved);
        return (true, "Opportunity saved successfully to your bookmarks!");
    }

    public async Task<(bool Success, string Message)> UnsaveOpportunityAsync(Guid studentProfileId, Guid opportunityId)
    {
        await _savedRepo.RemoveAsync(studentProfileId, opportunityId);
        return (true, "Opportunity removed from your saved list.");
    }

    public async Task<List<SavedOpportunity>> GetSavedOpportunitiesAsync(Guid studentProfileId)
    {
        return await _savedRepo.GetByStudentIdAsync(studentProfileId);
    }

    public async Task<bool> IsSavedAsync(Guid studentProfileId, Guid opportunityId)
    {
        return await _savedRepo.IsSavedAsync(studentProfileId, opportunityId);
    }
}
