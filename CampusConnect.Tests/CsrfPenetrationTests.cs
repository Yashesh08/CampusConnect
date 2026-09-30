using System.Net;
using CampusConnect.Data;
using CampusConnect.Models;
using CampusConnect.Models.Enums;
using CampusConnect.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CampusConnect.Tests;

public class CsrfPenetrationTests : IAsyncLifetime
{
    private readonly CampusConnectTestFactory _factory = new("csrf");
    private Guid _approvedOppId;
    private Guid _pendingOppId;
    private Guid _grievanceId;
    private int _studentSkillId;
    private Guid _applicationId;
    private Guid _organizerAppId;
    private Guid _hodUserId;

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var studentA = await db.Users.Include(u => u.StudentProfile).FirstAsync(u => u.Email == "student.alice@campusconnect.edu");
        var facultyA = await db.Users.FirstAsync(u => u.Email == "faculty.smith@campusconnect.edu");
        var hod = await db.Users.FirstAsync(u => u.Email == "hod.cse@campusconnect.edu");
        var dept = await db.Departments.FirstAsync(d => d.DepartmentCode == "CSE");
        var skill = await db.Skills.FirstAsync();

        _hodUserId = hod.Id;

        // Approved opportunity
        var opp = new Opportunity
        {
            OpportunityId = Guid.NewGuid(),
            OrganizerId = facultyA.Id,
            Title = "CSRF Testing Approved Opp",
            Description = "Test Opp",
            Category = OpportunityCategory.Workshop,
            TargetDepartmentId = dept.DepartmentId,
            WorkMode = WorkMode.Onsite,
            RegistrationDeadline = DateTime.UtcNow.AddDays(10),
            Capacity = 10,
            ApprovalStatus = ApprovalStatus.Approved
        };

        // Pending opportunity
        var pendingOpp = new Opportunity
        {
            OpportunityId = Guid.NewGuid(),
            OrganizerId = facultyA.Id,
            Title = "CSRF Testing Pending Opp",
            Description = "Test Pending",
            Category = OpportunityCategory.Seminar,
            TargetDepartmentId = dept.DepartmentId,
            WorkMode = WorkMode.Onsite,
            RegistrationDeadline = DateTime.UtcNow.AddDays(10),
            Capacity = 10,
            ApprovalStatus = ApprovalStatus.PendingReview
        };

        db.Opportunities.AddRange(opp, pendingOpp);

        // Application for student
        var app = new Application
        {
            ApplicationId = Guid.NewGuid(),
            OpportunityId = opp.OpportunityId,
            StudentId = studentA.StudentProfile!.ProfileId,
            Status = ApplicationStatus.Applied,
            AppliedAt = DateTime.UtcNow
        };
        db.Applications.Add(app);

        // Skill for student
        var ss = new StudentSkill
        {
            StudentId = studentA.StudentProfile!.ProfileId,
            SkillId = skill.SkillId,
            ProficiencyLevel = ProficiencyLevel.Intermediate
        };
        db.StudentSkills.Add(ss);

        // Grievance
        var g = new Grievance
        {
            GrievanceId = Guid.NewGuid(),
            ComplainantUserId = studentA.Id,
            IsAnonymous = false,
            Category = GrievanceCategory.Infrastructure,
            DepartmentId = dept.DepartmentId,
            Description = "CSRF testing grievance description",
            Priority = GrievancePriority.Medium,
            Status = GrievanceStatus.Submitted,
            SlaDueAt = DateTime.UtcNow.AddDays(5),
            CreatedAt = DateTime.UtcNow
        };
        db.Grievances.Add(g);

        await db.SaveChangesAsync();

        _approvedOppId = opp.OpportunityId;
        _pendingOppId = pendingOpp.OpportunityId;
        _applicationId = app.ApplicationId;
        _studentSkillId = ss.StudentSkillId;
        _grievanceId = g.GrievanceId;
        _organizerAppId = app.ApplicationId;
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Theory]
    [InlineData("/StudentProfiles/AddSkill")]
    [InlineData("/StudentProfiles/RemoveSkill")]
    [InlineData("/Applications/Apply")]
    [InlineData("/Applications/Withdraw/")]
    [InlineData("/Grievances/Create")]
    public async Task StudentPost_WithoutAntiforgeryToken_ReturnsBadRequest(string endpoint)
    {
        var client = await _factory.CreateAuthenticatedClientAsync("student.alice@campusconnect.edu");
        
        var targetUrl = endpoint.EndsWith("/") ? endpoint + _applicationId : endpoint;
        var postData = new Dictionary<string, string>
        {
            { "opportunityId", _approvedOppId.ToString() },
            { "studentSkillId", _studentSkillId.ToString() },
            { "skillId", "1" },
            { "proficiencyLevel", "Intermediate" },
            { "Description", "Missing token attack" },
            { "Category", "Academic" },
            { "Priority", "Low" },
            { "DepartmentId", "1" }
        };

        // Send request with NO __RequestVerificationToken
        var response = await client.PostAsync(targetUrl, new FormUrlEncodedContent(postData));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/StudentProfiles/AddSkill")]
    [InlineData("/StudentProfiles/RemoveSkill")]
    [InlineData("/Applications/Apply")]
    [InlineData("/Grievances/Create")]
    public async Task StudentPost_WithInvalidAntiforgeryToken_ReturnsBadRequest(string endpoint)
    {
        var client = await _factory.CreateAuthenticatedClientAsync("student.alice@campusconnect.edu");

        var postData = new Dictionary<string, string>
        {
            { "opportunityId", _approvedOppId.ToString() },
            { "studentSkillId", _studentSkillId.ToString() },
            { "skillId", "1" },
            { "proficiencyLevel", "Intermediate" },
            { "Description", "Forged token attack" },
            { "Category", "Academic" },
            { "Priority", "Low" },
            { "DepartmentId", "1" },
            { "__RequestVerificationToken", "FORGED_MALICIOUS_TOKEN_12345" }
        };

        var response = await client.PostAsync(endpoint, new FormUrlEncodedContent(postData));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/Opportunities/Create")]
    [InlineData("/Organizer/UpdateStatus")]
    public async Task FacultyPost_WithoutAntiforgeryToken_ReturnsBadRequest(string endpoint)
    {
        var client = await _factory.CreateAuthenticatedClientAsync("faculty.smith@campusconnect.edu");

        var postData = new Dictionary<string, string>
        {
            { "Title", "Forged Opportunity" },
            { "Description", "Forged Desc" },
            { "applicationId", _organizerAppId.ToString() },
            { "status", "Shortlisted" }
        };

        var response = await client.PostAsync(endpoint, new FormUrlEncodedContent(postData));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/Opportunities/Approve/")]
    [InlineData("/Opportunities/Reject/")]
    public async Task AdminPost_WithoutAntiforgeryToken_ReturnsBadRequest(string endpointPrefix)
    {
        var client = await _factory.CreateAuthenticatedClientAsync("admin@campusconnect.edu");

        var targetUrl = endpointPrefix + _pendingOppId;
        var postData = new Dictionary<string, string>
        {
            { "id", _pendingOppId.ToString() }
        };

        var response = await client.PostAsync(targetUrl, new FormUrlEncodedContent(postData));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ValidAntiforgeryToken_Succeeds()
    {
        // When a valid token is obtained from the page, submission succeeds
        var client = await _factory.CreateAuthenticatedClientAsync("student.alice@campusconnect.edu");
        var (token, _) = await _factory.GetPageWithTokenAsync(client, "/Grievances/Create");

        Assert.False(string.IsNullOrWhiteSpace(token), "Form must render a valid __RequestVerificationToken");

        var postData = new Dictionary<string, string>
        {
            { "Category", GrievanceCategory.HostelMess.ToString() },
            { "DepartmentId", "1" },
            { "Priority", GrievancePriority.Low.ToString() },
            { "Description", "Valid antiforgery submission test grievance." },
            { "IsAnonymous", "false" },
            { "__RequestVerificationToken", token }
        };

        var response = await client.PostAsync("/Grievances/Create", new FormUrlEncodedContent(postData));

        // Successful submission redirects to Tracker
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? "";
        Assert.Contains("/Grievances/Tracker", finalUrl);
    }
}
