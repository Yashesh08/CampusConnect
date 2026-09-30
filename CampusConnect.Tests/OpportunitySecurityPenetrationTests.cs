using System.Net;
using CampusConnect.Data;
using CampusConnect.Models;
using CampusConnect.Models.Enums;
using CampusConnect.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CampusConnect.Tests;

public class OpportunitySecurityPenetrationTests : IAsyncLifetime
{
    private readonly CampusConnectTestFactory _factory = new("opportunity");
    private Guid _pendingOppId;
    private Guid _rejectedOppId;
    private Guid _approvedOppId;
    private Guid _organizerAOppId;
    private Guid _organizerAUserId;
    private Guid _organizerBUserId;
    private Guid _mathDeptOppId;

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cseDept = await db.Departments.FirstAsync(d => d.DepartmentCode == "CSE");
        var mathDept = await db.Departments.FirstAsync(d => d.DepartmentCode == "MATH");
        var organizerA = await db.Users.FirstAsync(u => u.Email == "organizer.alpha@campusconnect.edu");
        var organizerB = await db.Users.FirstAsync(u => u.Email == "organizer.beta@campusconnect.edu");

        _organizerAUserId = organizerA.Id;
        _organizerBUserId = organizerB.Id;

        // 1. Pending opportunity owned by Organizer A
        var pendingOpp = new Opportunity
        {
            OpportunityId = Guid.NewGuid(),
            OrganizerId = organizerA.Id,
            Title = "Confidential Quantum Research Internship",
            Description = "Secret unapproved project details.",
            Category = OpportunityCategory.Internship,
            TargetDepartmentId = cseDept.DepartmentId,
            WorkMode = WorkMode.Remote,
            RegistrationDeadline = DateTime.UtcNow.AddDays(30),
            Capacity = 2,
            ApprovalStatus = ApprovalStatus.PendingReview
        };

        // 2. Rejected opportunity
        var rejectedOpp = new Opportunity
        {
            OpportunityId = Guid.NewGuid(),
            OrganizerId = organizerA.Id,
            Title = "Rejected Hazardous Experiment",
            Description = "Rejected safety hazard proposal.",
            Category = OpportunityCategory.Workshop,
            TargetDepartmentId = cseDept.DepartmentId,
            WorkMode = WorkMode.Onsite,
            RegistrationDeadline = DateTime.UtcNow.AddDays(10),
            Capacity = 5,
            ApprovalStatus = ApprovalStatus.Rejected
        };

        // 3. Approved opportunity
        var approvedOpp = new Opportunity
        {
            OpportunityId = Guid.NewGuid(),
            OrganizerId = organizerA.Id,
            Title = "Approved Open Hackathon",
            Description = "Open for all students.",
            Category = OpportunityCategory.Hackathon,
            TargetDepartmentId = cseDept.DepartmentId,
            WorkMode = WorkMode.Hybrid,
            RegistrationDeadline = DateTime.UtcNow.AddDays(14),
            Capacity = 20,
            ApprovalStatus = ApprovalStatus.Approved
        };

        // 4. Mathematics Department Pending Opportunity (for HOD boundary test)
        var mathOpp = new Opportunity
        {
            OpportunityId = Guid.NewGuid(),
            OrganizerId = organizerA.Id,
            Title = "Math Department Advanced Calculus Workshop",
            Description = "Mathematics workshop.",
            Category = OpportunityCategory.Workshop,
            TargetDepartmentId = mathDept.DepartmentId,
            WorkMode = WorkMode.Onsite,
            RegistrationDeadline = DateTime.UtcNow.AddDays(20),
            Capacity = 10,
            ApprovalStatus = ApprovalStatus.PendingReview
        };

        db.Opportunities.AddRange(pendingOpp, rejectedOpp, approvedOpp, mathOpp);
        await db.SaveChangesAsync();

        _pendingOppId = pendingOpp.OpportunityId;
        _rejectedOppId = rejectedOpp.OpportunityId;
        _approvedOppId = approvedOpp.OpportunityId;
        _organizerAOppId = approvedOpp.OpportunityId;
        _mathDeptOppId = mathOpp.OpportunityId;
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task V009_DEMO_Student_CanView_PendingReviewOpportunity_Via_Details_DemonstratesVulnerability()
    {
        // ATTACK: Student visits /Opportunities/Details/{pendingOppId} directly
        var studentClient = await _factory.CreateAuthenticatedClientAsync("student.alice@campusconnect.edu");
        var response = await studentClient.GetAsync($"/Opportunities/Details/{_pendingOppId}");

        var html = await response.Content.ReadAsStringAsync();
        bool canViewPending = response.StatusCode == HttpStatusCode.OK && html.Contains("Confidential Quantum Research Internship");

        // V009 Proof: The unapproved opportunity is visible to students!
        Assert.True(canViewPending, "V009 REPRODUCED: Student is able to view a PendingReview opportunity via direct URL!");
    }

    [Fact]
    public async Task V009_DEMO_Student_CanView_RejectedOpportunity_Via_Details_DemonstratesVulnerability()
    {
        // ATTACK: Student visits /Opportunities/Details/{rejectedOppId} directly
        var studentClient = await _factory.CreateAuthenticatedClientAsync("student.alice@campusconnect.edu");
        var response = await studentClient.GetAsync($"/Opportunities/Details/{_rejectedOppId}");

        var html = await response.Content.ReadAsStringAsync();
        bool canViewRejected = response.StatusCode == HttpStatusCode.OK && html.Contains("Rejected Hazardous Experiment");

        // V009 Proof: The rejected opportunity is visible to students!
        Assert.True(canViewRejected, "V009 REPRODUCED: Student is able to view a Rejected opportunity via direct URL!");
    }

    [Fact]
    public async Task V017_DEMO_Student_CanApply_To_PendingReviewOpportunity_DemonstratesVulnerability()
    {
        // ATTACK: Student sends POST /Applications/Apply with opportunityId = pendingOppId
        var studentClient = await _factory.CreateAuthenticatedClientAsync("student.alice@campusconnect.edu");
        var (token, _) = await _factory.GetPageWithTokenAsync(studentClient, $"/Opportunities/Details/{_approvedOppId}");

        var formData = new Dictionary<string, string>
        {
            { "opportunityId", _pendingOppId.ToString() },
            { "__RequestVerificationToken", token }
        };

        var response = await studentClient.PostAsync("/Applications/Apply", new FormUrlEncodedContent(formData));

        // Check if an application was created in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var student = await db.Users.Include(u => u.StudentProfile).FirstAsync(u => u.Email == "student.alice@campusconnect.edu");
        
        bool applicationCreated = await db.Applications.AnyAsync(a => a.OpportunityId == _pendingOppId && a.StudentId == student.StudentProfile!.ProfileId);

        Assert.True(applicationCreated, "V017 REPRODUCED: Student was able to apply to an unapproved (PendingReview) opportunity!");
    }

    [Fact]
    public async Task V017_DEMO_Student_CanApply_To_RejectedOpportunity_DemonstratesVulnerability()
    {
        // ATTACK: Student sends POST /Applications/Apply with opportunityId = rejectedOppId
        var studentClient = await _factory.CreateAuthenticatedClientAsync("student.bob@campusconnect.edu");
        var (token, _) = await _factory.GetPageWithTokenAsync(studentClient, $"/Opportunities/Details/{_approvedOppId}");

        var formData = new Dictionary<string, string>
        {
            { "opportunityId", _rejectedOppId.ToString() },
            { "__RequestVerificationToken", token }
        };

        var response = await studentClient.PostAsync("/Applications/Apply", new FormUrlEncodedContent(formData));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var student = await db.Users.Include(u => u.StudentProfile).FirstAsync(u => u.Email == "student.bob@campusconnect.edu");

        bool applicationCreated = await db.Applications.AnyAsync(a => a.OpportunityId == _rejectedOppId && a.StudentId == student.StudentProfile!.ProfileId);

        Assert.True(applicationCreated, "V017 REPRODUCED: Student was able to apply to a REJECTED opportunity!");
    }

    [Fact]
    public async Task Opportunities_PendingApprovals_StudentCannotAccess()
    {
        var studentClient = await _factory.CreateAuthenticatedClientAsync("student.alice@campusconnect.edu");
        var response = await studentClient.GetAsync("/Opportunities/PendingApprovals");
        CampusConnectTestFactory.AssertAccessDenied(response);
    }

    [Fact]
    public async Task Opportunities_Approve_StudentCannotApprove()
    {
        var studentClient = await _factory.CreateAuthenticatedClientAsync("student.alice@campusconnect.edu");
        var (token, _) = await _factory.GetPageWithTokenAsync(studentClient, $"/Opportunities/Details/{_approvedOppId}");

        var formData = new Dictionary<string, string>
        {
            { "id", _pendingOppId.ToString() },
            { "__RequestVerificationToken", token }
        };

        var response = await studentClient.PostAsync($"/Opportunities/Approve/{_pendingOppId}", new FormUrlEncodedContent(formData));
        CampusConnectTestFactory.AssertAccessDenied(response);
    }

    [Fact]
    public async Task Opportunities_Approve_HodCse_CannotApprove_MathOpportunity()
    {
        // HOD CSE has department CSE. Math opportunity has department MATH.
        // OpportunitiesController line 149-154 checks:
        // if (User.IsInRole("Hod") && !User.IsInRole("Admin")) { facProfile.DepartmentId != opp.TargetDepartmentId -> Forbid() }
        var hodClient = await _factory.CreateAuthenticatedClientAsync("hod.cse@campusconnect.edu");
        var (token, _) = await _factory.GetPageWithTokenAsync(hodClient, "/Opportunities/PendingApprovals");

        var formData = new Dictionary<string, string>
        {
            { "id", _mathDeptOppId.ToString() },
            { "__RequestVerificationToken", token }
        };

        var response = await hodClient.PostAsync($"/Opportunities/Approve/{_mathDeptOppId}", new FormUrlEncodedContent(formData));
        CampusConnectTestFactory.AssertAccessDenied(response);
    }

    [Fact]
    public async Task Opportunities_Approve_Admin_CanApprove_AnyDepartment()
    {
        var adminClient = await _factory.CreateAuthenticatedClientAsync("admin@campusconnect.edu");
        var (token, _) = await _factory.GetPageWithTokenAsync(adminClient, "/Opportunities/PendingApprovals");

        var formData = new Dictionary<string, string>
        {
            { "id", _mathDeptOppId.ToString() },
            { "__RequestVerificationToken", token }
        };

        var response = await adminClient.PostAsync($"/Opportunities/Approve/{_mathDeptOppId}", new FormUrlEncodedContent(formData));
        // Redirects to PendingApprovals on success
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var opp = await db.Opportunities.FindAsync(_mathDeptOppId);
        Assert.Equal(ApprovalStatus.Approved, opp!.ApprovalStatus);
    }

    [Fact]
    public async Task OrganizerB_CannotView_OrganizerA_Applicants()
    {
        // Organizer B attempts to view Organizer A's opportunity applicants
        var clientB = await _factory.CreateAuthenticatedClientAsync("organizer.beta@campusconnect.edu");
        var response = await clientB.GetAsync($"/Organizer/Applicants/{_organizerAOppId}");

        CampusConnectTestFactory.AssertAccessDenied(response);
    }

    [Fact]
    public async Task OrganizerB_CannotExportCsv_For_OrganizerA_Opportunity()
    {
        // Organizer B attempts to export CSV for Organizer A's opportunity
        var clientB = await _factory.CreateAuthenticatedClientAsync("organizer.beta@campusconnect.edu");
        var response = await clientB.GetAsync($"/Organizer/ExportCsv/{_organizerAOppId}");

        CampusConnectTestFactory.AssertAccessDenied(response);
    }
}
