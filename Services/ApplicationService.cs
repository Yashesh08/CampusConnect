using CampusConnect.Models;
using CampusConnect.Models.Enums;
using CampusConnect.Repositories;

namespace CampusConnect.Services;

public class ApplicationService : IApplicationService
{
    private readonly IApplicationRepository _applicationRepo;
    private readonly IOpportunityRepository _opportunityRepo;

    public ApplicationService(
        IApplicationRepository applicationRepo,
        IOpportunityRepository opportunityRepo)
    {
        _applicationRepo = applicationRepo;
        _opportunityRepo = opportunityRepo;
    }

    public async Task<(bool Success, string Message, Guid? ApplicationId)> ApplyAsync(Guid opportunityId, Guid studentProfileId)
    {
        var opportunity = await _opportunityRepo.GetByIdAsync(opportunityId);
        if (opportunity == null)
        {
            return (false, "Opportunity not found.", null);
        }

        if (opportunity.ApprovalStatus != ApprovalStatus.Approved)
        {
            return (false, "Applications are only accepted for approved opportunities.", null);
        }

        if (opportunity.RegistrationDeadline < DateTime.UtcNow)
        {
            return (false, "Registration deadline for this opportunity has already passed.", null);
        }

        bool alreadyApplied = await _applicationRepo.HasAlreadyAppliedAsync(opportunityId, studentProfileId);
        if (alreadyApplied)
        {
            return (false, "You have already applied for this opportunity.", null);
        }

        // Capacity check (Priority 4)
        if (opportunity.Capacity.HasValue && opportunity.Capacity.Value > 0)
        {
            var existingApps = await _applicationRepo.GetByOpportunityIdAsync(opportunityId);
            int activeAppsCount = existingApps.Count(a => a.Status != ApplicationStatus.Rejected);

            if (activeAppsCount >= opportunity.Capacity.Value)
            {
                return (false, "This opportunity has reached its maximum participant capacity.", null);
            }
        }

        var application = new Application
        {
            ApplicationId = Guid.NewGuid(),
            OpportunityId = opportunityId,
            StudentId = studentProfileId,
            Status = ApplicationStatus.Applied,
            AppliedAt = DateTime.UtcNow
        };

        await _applicationRepo.AddAsync(application);
        return (true, "Application submitted successfully!", application.ApplicationId);
    }

    public async Task<List<Application>> GetStudentApplicationsAsync(Guid studentProfileId, ApplicationStatus? status = null)
    {
        var applications = await _applicationRepo.GetByStudentIdAsync(studentProfileId);
        if (status.HasValue)
        {
            applications = applications.Where(a => a.Status == status.Value).ToList();
        }
        return applications;
    }

    public async Task<(bool Success, string Message)> WithdrawAsync(Guid applicationId, Guid studentProfileId)
    {
        var application = await _applicationRepo.GetByIdAsync(applicationId);
        if (application == null)
        {
            return (false, "Application not found.");
        }

        if (application.StudentId != studentProfileId)
        {
            return (false, "Forbidden: You are not authorized to withdraw this application.");
        }

        await _applicationRepo.DeleteAsync(applicationId);
        return (true, "Application withdrawn successfully.");
    }

    public bool IsValidStatusTransition(ApplicationStatus currentStatus, ApplicationStatus newStatus)
    {
        if (currentStatus == newStatus) return true;

        return currentStatus switch
        {
            ApplicationStatus.Applied => newStatus is ApplicationStatus.UnderReview or ApplicationStatus.Shortlisted or ApplicationStatus.Selected or ApplicationStatus.Rejected,
            ApplicationStatus.Registered => newStatus is ApplicationStatus.Applied or ApplicationStatus.UnderReview or ApplicationStatus.Shortlisted or ApplicationStatus.Selected or ApplicationStatus.Rejected,
            ApplicationStatus.UnderReview => newStatus is ApplicationStatus.Shortlisted or ApplicationStatus.Selected or ApplicationStatus.Rejected,
            ApplicationStatus.Shortlisted => newStatus is ApplicationStatus.Selected or ApplicationStatus.Rejected,
            ApplicationStatus.Selected => false, // Terminal state
            ApplicationStatus.Rejected => false, // Terminal state
            _ => false
        };
    }
}
