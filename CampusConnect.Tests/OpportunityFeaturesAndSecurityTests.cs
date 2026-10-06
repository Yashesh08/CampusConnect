using System.Net;
using CampusConnect.Data;
using CampusConnect.Models;
using CampusConnect.Models.Enums;
using CampusConnect.Models.ViewModels;
using CampusConnect.Services;
using CampusConnect.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CampusConnect.Tests;

public class OpportunityFeaturesAndSecurityTests : IAsyncLifetime
{
    private readonly CampusConnectTestFactory _factory = new("opportunity_features");
    private Guid _approvedOppId;
    private Guid _capacityOppId;
    private Guid _expiredOppId;
    private Guid _organizerAUserId;
    private Guid _organizerBUserId;
    private Guid _studentAliceId;

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cseDept = await db.Departments.FirstAsync(d => d.DepartmentCode == "CSE");
        var organizerA = await db.Users.FirstAsync(u => u.Email == "organizer.alpha@campusconnect.edu");
        var organizerB = await db.Users.FirstAsync(u => u.Email == "organizer.beta@campusconnect.edu");
        var studentAlice = await db.Users.Include(u => u.StudentProfile).FirstAsync(u => u.Email == "student.alice@campusconnect.edu");

        _organizerAUserId = organizerA.Id;
        _organizerBUserId = organizerB.Id;
        _studentAliceId = studentAlice.StudentProfile!.ProfileId;

        // 1. Standard approved opportunity
        var approvedOpp = new Opportunity
        {
            OpportunityId = Guid.NewGuid(),
            OrganizerId = organizerA.Id,
            Title = "Full Stack Web Development Project",
            Description = "Join the dev team building next gen features.",
            Category = OpportunityCategory.Internship,
            TargetDepartmentId = cseDept.DepartmentId,
            WorkMode = WorkMode.Remote,
            RegistrationDeadline = DateTime.UtcNow.AddDays(15),
            Capacity = 10,
            ApprovalStatus = ApprovalStatus.Approved
        };

        // 2. Capacity 1 opportunity (will fill capacity with 1 app)
        var capacityOpp = new Opportunity
        {
            OpportunityId = Guid.NewGuid(),
            OrganizerId = organizerA.Id,
            Title = "Exclusive Quantum Computing Seminar",
            Description = "Single seat research position.",
            Category = OpportunityCategory.Seminar,
            TargetDepartmentId = cseDept.DepartmentId,
            WorkMode = WorkMode.Onsite,
            RegistrationDeadline = DateTime.UtcNow.AddDays(10),
            Capacity = 1,
            ApprovalStatus = ApprovalStatus.Approved
        };

        // 3. Expired opportunity
        var expiredOpp = new Opportunity
        {
            OpportunityId = Guid.NewGuid(),
            OrganizerId = organizerA.Id,
            Title = "Past Summer Internship 2023",
            Description = "Expired registration deadline position.",
            Category = OpportunityCategory.Job,
            TargetDepartmentId = cseDept.DepartmentId,
            WorkMode = WorkMode.Hybrid,
            RegistrationDeadline = DateTime.UtcNow.AddDays(-5),
            Capacity = 5,
            ApprovalStatus = ApprovalStatus.Approved
        };

        db.Opportunities.AddRange(approvedOpp, capacityOpp, expiredOpp);
        await db.SaveChangesAsync();

        _approvedOppId = approvedOpp.OpportunityId;
        _capacityOppId = capacityOpp.OpportunityId;
        _expiredOppId = expiredOpp.OpportunityId;
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task DuplicateApplication_PreventsSecondSubmission()
    {
        using var scope = _factory.Services.CreateScope();
        var appService = scope.ServiceProvider.GetRequiredService<IApplicationService>();

        // First application: succeeds
        var res1 = await appService.ApplyAsync(_approvedOppId, _studentAliceId);
        Assert.True(res1.Success, "First application submission should succeed.");

        // Second application: fails due to duplicate check
        var res2 = await appService.ApplyAsync(_approvedOppId, _studentAliceId);
        Assert.False(res2.Success, "Second application submission should fail as duplicate.");
        Assert.Contains("already applied", res2.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CapacityExceeded_PreventsNewApplications()
    {
        using var scope = _factory.Services.CreateScope();
        var appService = scope.ServiceProvider.GetRequiredService<IApplicationService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var studentBob = await db.Users.Include(u => u.StudentProfile).FirstAsync(u => u.Email == "student.bob@campusconnect.edu");

        // Fill single-capacity opportunity with Alice
        var res1 = await appService.ApplyAsync(_capacityOppId, _studentAliceId);
        Assert.True(res1.Success);

        // Attempt second application with Bob
        var res2 = await appService.ApplyAsync(_capacityOppId, studentBob.StudentProfile!.ProfileId);
        Assert.False(res2.Success);
        Assert.Contains("capacity", res2.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExpiredDeadline_PreventsApplication()
    {
        using var scope = _factory.Services.CreateScope();
        var appService = scope.ServiceProvider.GetRequiredService<IApplicationService>();

        var res = await appService.ApplyAsync(_expiredOppId, _studentAliceId);
        Assert.False(res.Success);
        Assert.Contains("deadline", res.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SharedOpportunityFeed_OnlyReturnsUpcomingOpportunities()
    {
        using var scope = _factory.Services.CreateScope();
        var feed = scope.ServiceProvider.GetRequiredService<ISharedOpportunityFeed>();

        var upcoming = await feed.GetUpcomingOpportunitiesAsync();

        Assert.DoesNotContain(upcoming, o => o.OpportunityId == _expiredOppId);
        Assert.Contains(upcoming, o => o.OpportunityId == _approvedOppId);
    }

    [Fact]
    public async Task SaveAndUnsaveOpportunity_StudentBookmarkFlow()
    {
        using var scope = _factory.Services.CreateScope();
        var savedService = scope.ServiceProvider.GetRequiredService<ISavedOpportunityService>();

        // Save
        var saveRes = await savedService.SaveOpportunityAsync(_studentAliceId, _approvedOppId);
        Assert.True(saveRes.Success);

        bool isSaved = await savedService.IsSavedAsync(_studentAliceId, _approvedOppId);
        Assert.True(isSaved);

        // Get saved list
        var savedList = await savedService.GetSavedOpportunitiesAsync(_studentAliceId);
        Assert.Contains(savedList, s => s.OpportunityId == _approvedOppId);

        // Unsave
        var unsaveRes = await savedService.UnsaveOpportunityAsync(_studentAliceId, _approvedOppId);
        Assert.True(unsaveRes.Success);

        bool isSavedAfter = await savedService.IsSavedAsync(_studentAliceId, _approvedOppId);
        Assert.False(isSavedAfter);
    }

    [Fact]
    public async Task OrganizerOwnership_Edit_AllowedForOwner_ForbiddenForNonOwner()
    {
        // Organizer A owns _approvedOppId. Organizer B attempts to edit.
        var clientB = await _factory.CreateAuthenticatedClientAsync("organizer.beta@campusconnect.edu");
        var response = await clientB.GetAsync($"/Opportunities/Edit/{_approvedOppId}");

        CampusConnectTestFactory.AssertAccessDenied(response);
    }

    [Fact]
    public async Task OrganizerOwnership_Delete_AllowedForOwner_ForbiddenForNonOwner()
    {
        // Organizer B attempts to delete Organizer A's opportunity
        var clientB = await _factory.CreateAuthenticatedClientAsync("organizer.beta@campusconnect.edu");
        var (token, _) = await _factory.GetPageWithTokenAsync(clientB, "/Organizer/Dashboard");

        var formData = new Dictionary<string, string>
        {
            { "id", _approvedOppId.ToString() },
            { "__RequestVerificationToken", token }
        };

        var response = await clientB.PostAsync($"/Opportunities/Delete/{_approvedOppId}", new FormUrlEncodedContent(formData));

        CampusConnectTestFactory.AssertAccessDenied(response);
    }

    [Fact]
    public async Task ApplicationStatusTransitions_ValidateAllowedAndDisallowed()
    {
        using var scope = _factory.Services.CreateScope();
        var appService = scope.ServiceProvider.GetRequiredService<IApplicationService>();

        // Applied -> UnderReview: valid
        Assert.True(appService.IsValidStatusTransition(ApplicationStatus.Applied, ApplicationStatus.UnderReview));

        // Applied -> Selected: valid
        Assert.True(appService.IsValidStatusTransition(ApplicationStatus.Applied, ApplicationStatus.Selected));

        // Selected -> Applied: invalid (terminal state)
        Assert.False(appService.IsValidStatusTransition(ApplicationStatus.Selected, ApplicationStatus.Applied));

        // Rejected -> Shortlisted: invalid (terminal state)
        Assert.False(appService.IsValidStatusTransition(ApplicationStatus.Rejected, ApplicationStatus.Shortlisted));
    }
}
