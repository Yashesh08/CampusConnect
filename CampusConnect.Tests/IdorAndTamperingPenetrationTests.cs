using System.Net;
using CampusConnect.Data;
using CampusConnect.Models;
using CampusConnect.Models.Enums;
using CampusConnect.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CampusConnect.Tests;

public class IdorAndTamperingPenetrationTests : IAsyncLifetime
{
    private readonly CampusConnectTestFactory _factory = new("idor");
    private int _studentASkillId;
    private Guid _studentAProfileId;
    private Guid _studentBProfileId;
    private Guid _studentAApplicationId;
    private Guid _organizerAOppId;
    private Guid _organizerAAppId;

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var studentA = await db.Users.Include(u => u.StudentProfile).FirstAsync(u => u.Email == "student.alice@campusconnect.edu");
        var studentB = await db.Users.Include(u => u.StudentProfile).FirstAsync(u => u.Email == "student.bob@campusconnect.edu");
        var organizerA = await db.Users.FirstAsync(u => u.Email == "organizer.alpha@campusconnect.edu");
        var skill1 = await db.Skills.FirstAsync();
        var opp = await db.Opportunities.FirstAsync(o => o.ApprovalStatus == ApprovalStatus.Approved);

        _studentAProfileId = studentA.StudentProfile!.ProfileId;
        _studentBProfileId = studentB.StudentProfile!.ProfileId;

        // Add a skill to Student A
        var ssA = new StudentSkill
        {
            StudentId = _studentAProfileId,
            SkillId = skill1.SkillId,
            ProficiencyLevel = ProficiencyLevel.Advanced
        };
        db.StudentSkills.Add(ssA);

        // Add an application for Student A
        var appA = new Application
        {
            ApplicationId = Guid.NewGuid(),
            OpportunityId = opp.OpportunityId,
            StudentId = _studentAProfileId,
            Status = ApplicationStatus.Applied,
            AppliedAt = DateTime.UtcNow
        };
        db.Applications.Add(appA);

        // Opportunity owned by Organizer A with an application
        var oppA = new Opportunity
        {
            OpportunityId = Guid.NewGuid(),
            OrganizerId = organizerA.Id,
            Title = "Organizer A Special Event",
            Description = "Event by organizer A",
            Category = OpportunityCategory.CulturalEvent,
            WorkMode = WorkMode.Onsite,
            RegistrationDeadline = DateTime.UtcNow.AddDays(7),
            Capacity = 10,
            ApprovalStatus = ApprovalStatus.Approved
        };
        db.Opportunities.Add(oppA);

        var appOrgA = new Application
        {
            ApplicationId = Guid.NewGuid(),
            OpportunityId = oppA.OpportunityId,
            StudentId = _studentBProfileId,
            Status = ApplicationStatus.Applied,
            AppliedAt = DateTime.UtcNow
        };
        db.Applications.Add(appOrgA);

        await db.SaveChangesAsync();

        _studentASkillId = ssA.StudentSkillId;
        _studentAApplicationId = appA.ApplicationId;
        _organizerAOppId = oppA.OpportunityId;
        _organizerAAppId = appOrgA.ApplicationId;
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task V001_HTTP_StudentB_CannotDelete_StudentA_Skill_Via_RemoveSkill()
    {
        // ATTACK: Student B posts to /StudentProfiles/RemoveSkill with studentSkillId = Student A's skill
        var clientB = await _factory.CreateAuthenticatedClientAsync("student.bob@campusconnect.edu");
        var (token, _) = await _factory.GetPageWithTokenAsync(clientB, "/StudentProfiles/ManageSkills");

        var formData = new Dictionary<string, string>
        {
            { "studentSkillId", _studentASkillId.ToString() },
            { "__RequestVerificationToken", token }
        };

        var response = await clientB.PostAsync("/StudentProfiles/RemoveSkill", new FormUrlEncodedContent(formData));

        // The controller checks: if (!profile.StudentSkills.Any(ss => ss.StudentSkillId == studentSkillId)) return Forbid();
        CampusConnectTestFactory.AssertAccessDenied(response);

        // Verify Student A's skill was NOT deleted
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        bool skillStillExists = await db.StudentSkills.AnyAsync(ss => ss.StudentSkillId == _studentASkillId);
        Assert.True(skillStillExists, "Student A's skill must remain intact after unauthorized deletion attempt!");
    }

    [Fact]
    public async Task StudentB_CannotTamper_StudentA_ProfileId_In_ProfileEdit()
    {
        // ATTACK: Student B submits Profile Edit with ProfileId = Student A's ProfileId
        var clientB = await _factory.CreateAuthenticatedClientAsync("student.bob@campusconnect.edu");
        var (token, _) = await _factory.GetPageWithTokenAsync(clientB, "/StudentProfiles/Edit");

        var formData = new Dictionary<string, string>
        {
            { "ProfileId", _studentAProfileId.ToString() },
            { "RollNumber", "HACKED_ROLL" },
            { "DepartmentId", "1" },
            { "BatchYear", "2025" },
            { "Bio", "Hacked Bio" },
            { "__RequestVerificationToken", token }
        };

        var response = await clientB.PostAsync("/StudentProfiles/Edit", new FormUrlEncodedContent(formData));

        // Controller checks: if (profile == null || profile.ProfileId != model.ProfileId) return NotFound();
        // Returns 404 NotFound
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Verify Student A's profile was not altered
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var profileA = await db.StudentProfiles.FindAsync(_studentAProfileId);
        Assert.NotEqual("HACKED_ROLL", profileA!.RollNumber);
    }

    [Fact]
    public async Task StudentB_CannotWithdraw_StudentA_Application()
    {
        // ATTACK: Student B posts to /Applications/Withdraw/{StudentA_ApplicationId}
        var clientB = await _factory.CreateAuthenticatedClientAsync("student.bob@campusconnect.edu");
        var (token, _) = await _factory.GetPageWithTokenAsync(clientB, "/Applications/MyApplications");

        var formData = new Dictionary<string, string>
        {
            { "__RequestVerificationToken", token }
        };

        var response = await clientB.PostAsync($"/Applications/Withdraw/{_studentAApplicationId}", new FormUrlEncodedContent(formData));

        // Controller checks: if (application.StudentId != studentProfile.ProfileId) return Forbid();
        CampusConnectTestFactory.AssertAccessDenied(response);

        // Verify application still exists
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        bool appExists = await db.Applications.AnyAsync(a => a.ApplicationId == _studentAApplicationId);
        Assert.True(appExists, "Student A's application must not be deleted by Student B!");
    }

    [Fact]
    public async Task OrganizerB_CannotUpdateStatus_Of_OrganizerA_Applicant()
    {
        // ATTACK: Organizer B attempts to update status of an applicant on Organizer A's opportunity
        var clientB = await _factory.CreateAuthenticatedClientAsync("organizer.beta@campusconnect.edu");
        var (token, _) = await _factory.GetPageWithTokenAsync(clientB, "/Organizer/Dashboard");

        var formData = new Dictionary<string, string>
        {
            { "applicationId", _organizerAAppId.ToString() },
            { "status", ApplicationStatus.Selected.ToString() },
            { "remarks", "Tampered by Organizer B" },
            { "__RequestVerificationToken", token }
        };

        var response = await clientB.PostAsync("/Organizer/UpdateStatus", new FormUrlEncodedContent(formData));

        // Controller checks: if (opportunity?.OrganizerId != user?.Id && !User.IsInRole("Admin")) return Forbid();
        CampusConnectTestFactory.AssertAccessDenied(response);

        // Verify applicant status is unchanged
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var app = await db.Applications.FindAsync(_organizerAAppId);
        Assert.Equal(ApplicationStatus.Applied, app!.Status);
    }

    [Fact]
    public async Task OrganizerB_CannotBulkUpdate_Applicants_Of_OrganizerA_Opportunity()
    {
        // ATTACK: Organizer B attempts bulk update of applicants for Organizer A's opportunity
        var clientB = await _factory.CreateAuthenticatedClientAsync("organizer.beta@campusconnect.edu");
        var (token, _) = await _factory.GetPageWithTokenAsync(clientB, "/Organizer/Dashboard");

        var formData = new Dictionary<string, string>
        {
            { "opportunityId", _organizerAOppId.ToString() },
            { "applicationIds", _organizerAAppId.ToString() },
            { "status", ApplicationStatus.Rejected.ToString() },
            { "__RequestVerificationToken", token }
        };

        var response = await clientB.PostAsync("/Organizer/BulkUpdateStatus", new FormUrlEncodedContent(formData));

        CampusConnectTestFactory.AssertAccessDenied(response);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var app = await db.Applications.FindAsync(_organizerAAppId);
        Assert.Equal(ApplicationStatus.Applied, app!.Status);
    }

    [Fact]
    public async Task V003_HTTP_UserWithoutProfile_CannotLeakOtherStudentData_In_MyApplications()
    {
        // Register/create a new student user who has NO profile
        string freshEmail = "fresh.student@campusconnect.edu";
        using (var scope = _factory.Services.CreateScope())
        {
            var userMgr = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<User>>();
            var freshUser = new User
            {
                UserName = freshEmail,
                Email = freshEmail,
                EmailConfirmed = true,
                Role = UserRole.Student,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            await userMgr.CreateAsync(freshUser, "Password123!");
            await userMgr.AddToRoleAsync(freshUser, UserRole.Student.ToString());
        }

        var client = await _factory.CreateAuthenticatedClientAsync(freshEmail);
        var response = await client.GetAsync("/Applications/MyApplications");

        var html = await response.Content.ReadAsStringAsync();

        // If V003 were present, this new student would see Student A's application!
        // With V003 fixed, empty list is returned.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(_studentAApplicationId.ToString(), html);
    }
}
