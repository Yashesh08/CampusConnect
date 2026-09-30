using System.Net;
using CampusConnect.Data;
using CampusConnect.Models;
using CampusConnect.Models.Enums;
using CampusConnect.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CampusConnect.Tests;

public class GrievanceSecurityPenetrationTests : IAsyncLifetime
{
    private readonly CampusConnectTestFactory _factory = new("grievance");
    private Guid _studentAGrievanceId;
    private Guid _studentBGrievanceId;
    private Guid _anonymousGrievanceId;
    private Guid _studentAUserId;
    private Guid _studentBUserId;
    private Guid _hodUserId;
    private Guid _facultyUserId;

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var studentA = await db.Users.FirstAsync(u => u.Email == "student.alice@campusconnect.edu");
        var studentB = await db.Users.FirstAsync(u => u.Email == "student.bob@campusconnect.edu");
        var hod = await db.Users.FirstAsync(u => u.Email == "hod.cse@campusconnect.edu");
        var faculty = await db.Users.FirstAsync(u => u.Email == "faculty.smith@campusconnect.edu");
        var dept = await db.Departments.FirstAsync(d => d.DepartmentCode == "CSE");

        _studentAUserId = studentA.Id;
        _studentBUserId = studentB.Id;
        _hodUserId = hod.Id;
        _facultyUserId = faculty.Id;

        // Create Alice's private grievance
        var gA = new Grievance
        {
            GrievanceId = Guid.NewGuid(),
            ComplainantUserId = studentA.Id,
            IsAnonymous = false,
            Category = GrievanceCategory.HarassmentRagging,
            DepartmentId = dept.DepartmentId,
            Description = "Alice's confidential harassment grievance against a senior student.",
            Priority = GrievancePriority.Critical,
            Status = GrievanceStatus.Submitted,
            SlaDueAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        };

        // Create Bob's grievance
        var gB = new Grievance
        {
            GrievanceId = Guid.NewGuid(),
            ComplainantUserId = studentB.Id,
            IsAnonymous = false,
            Category = GrievanceCategory.Infrastructure,
            DepartmentId = dept.DepartmentId,
            Description = "Bob's broken chair in lab 3.",
            Priority = GrievancePriority.Low,
            Status = GrievanceStatus.Submitted,
            SlaDueAt = DateTime.UtcNow.AddDays(10),
            CreatedAt = DateTime.UtcNow
        };

        // Create Anonymous grievance
        var gAnon = new Grievance
        {
            GrievanceId = Guid.NewGuid(),
            ComplainantUserId = null,
            IsAnonymous = true,
            Category = GrievanceCategory.Academic,
            DepartmentId = dept.DepartmentId,
            Description = "Anonymous complaint regarding exam grading irregularities.",
            Priority = GrievancePriority.High,
            Status = GrievanceStatus.Submitted,
            SlaDueAt = DateTime.UtcNow.AddDays(3),
            CreatedAt = DateTime.UtcNow
        };

        db.Grievances.AddRange(gA, gB, gAnon);
        await db.SaveChangesAsync();

        _studentAGrievanceId = gA.GrievanceId;
        _studentBGrievanceId = gB.GrievanceId;
        _anonymousGrievanceId = gAnon.GrievanceId;
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task V007_DEMO_StudentB_Accessing_StudentA_Grievance_Via_Tracker_DemonstratesVulnerability()
    {
        // ATTACK: Student B accesses /Grievances/Tracker?ticketId={StudentA's Grievance ID}
        var clientB = await _factory.CreateAuthenticatedClientAsync("student.bob@campusconnect.edu");
        var response = await clientB.GetAsync($"/Grievances/Tracker?ticketId={_studentAGrievanceId}");

        var html = await response.Content.ReadAsStringAsync();

        // If vulnerable, Student B can see Alice's confidential grievance details!
        // We record whether this leaks Alice's confidential information:
        bool leaksAliceConfidentialData = html.Contains("Alice&#x27;s confidential harassment") || html.Contains("Alice's confidential harassment");
        
        // This test proves whether V007 exists:
        // Expected secure behavior: B should NOT see A's grievance (403 Forbidden or "No Ticket Found").
        // Current actual behavior: leaksAliceConfidentialData is TRUE!
        Assert.True(leaksAliceConfidentialData, "V007 REPRODUCED: Student B is able to view Student A's private grievance through Tracker!");
    }

    [Fact]
    public async Task V007_DEMO_StudentB_Accessing_StudentA_Grievance_Via_PrefixTicketCode_DemonstratesVulnerability()
    {
        // ATTACK: Student B accesses Tracker with 8-char prefix
        var prefix = _studentAGrievanceId.ToString()[..8];
        var clientB = await _factory.CreateAuthenticatedClientAsync("student.bob@campusconnect.edu");
        var response = await clientB.GetAsync($"/Grievances/Tracker?ticketId={prefix}");

        var html = await response.Content.ReadAsStringAsync();
        bool leaksAliceConfidentialData = html.Contains("Alice&#x27;s confidential harassment") || html.Contains("Alice's confidential harassment");

        Assert.True(leaksAliceConfidentialData, "V007 REPRODUCED: Student B can view Student A's grievance using 8-character prefix search!");
    }

    [Fact]
    public async Task V007b_DEMO_StudentB_Accessing_AnonymousGrievance_Via_Details_DemonstratesVulnerability()
    {
        // ATTACK: Student B calls /Grievances/Details/{AnonymousGrievanceId}
        // Because of '&& !grievance.IsAnonymous' in GrievancesController line 209, non-privileged users bypass Forbid()!
        var clientB = await _factory.CreateAuthenticatedClientAsync("student.bob@campusconnect.edu");
        var response = await clientB.GetAsync($"/Grievances/Details/{_anonymousGrievanceId}");

        var html = await response.Content.ReadAsStringAsync();

        // If vulnerable, Student B can read the anonymous grievance!
        bool leaksAnonymousGrievance = response.StatusCode == HttpStatusCode.OK && 
                                       (html.Contains("exam grading irregularities"));

        Assert.True(leaksAnonymousGrievance, "V007b REPRODUCED: Student B can view anonymous grievances via /Grievances/Details!");
    }

    [Fact]
    public async Task Grievances_Details_StudentB_Accessing_StudentA_Grievance_IsForbidden()
    {
        var clientB = await _factory.CreateAuthenticatedClientAsync("student.bob@campusconnect.edu");
        var response = await clientB.GetAsync($"/Grievances/Details/{_studentAGrievanceId}");

        CampusConnectTestFactory.AssertAccessDenied(response);
    }

    [Fact]
    public async Task Grievances_UpdateStatus_StudentCannotUpdateStatus()
    {
        // ATTACK: Student attempts to update status of a grievance
        var client = await _factory.CreateAuthenticatedClientAsync("student.bob@campusconnect.edu");
        var (token, _) = await _factory.GetPageWithTokenAsync(client, "/Grievances/Create");

        var formData = new Dictionary<string, string>
        {
            { "id", _studentBGrievanceId.ToString() },
            { "newStatus", GrievanceStatus.Resolved.ToString() },
            { "note", "Student self-resolving ticket" },
            { "__RequestVerificationToken", token }
        };

        var response = await client.PostAsync($"/Grievances/UpdateStatus/{_studentBGrievanceId}", new FormUrlEncodedContent(formData));
        CampusConnectTestFactory.AssertAccessDenied(response);
    }

    [Fact]
    public async Task Grievances_Assign_StudentCannotAssignGrievance()
    {
        // ATTACK: Student attempts to assign a grievance to themselves or someone else
        var client = await _factory.CreateAuthenticatedClientAsync("student.bob@campusconnect.edu");
        var (token, _) = await _factory.GetPageWithTokenAsync(client, "/Grievances/Create");

        var formData = new Dictionary<string, string>
        {
            { "id", _studentBGrievanceId.ToString() },
            { "assignedToUserId", _studentBUserId.ToString() },
            { "note", "Assigning to myself" },
            { "__RequestVerificationToken", token }
        };

        var response = await client.PostAsync($"/Grievances/Assign/{_studentBGrievanceId}", new FormUrlEncodedContent(formData));
        CampusConnectTestFactory.AssertAccessDenied(response);
    }

    [Fact]
    public async Task Grievances_OfficerDashboard_StudentIsForbidden()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("student.bob@campusconnect.edu");
        var response = await client.GetAsync("/Grievances/OfficerDashboard");
        CampusConnectTestFactory.AssertAccessDenied(response);
    }

    [Fact]
    public async Task Grievances_Escalations_StudentAndFacultyAreForbidden()
    {
        var studentClient = await _factory.CreateAuthenticatedClientAsync("student.bob@campusconnect.edu");
        var studentResponse = await studentClient.GetAsync("/Grievances/Escalations");
        CampusConnectTestFactory.AssertAccessDenied(studentResponse);

        var facultyClient = await _factory.CreateAuthenticatedClientAsync("faculty.smith@campusconnect.edu");
        var facultyResponse = await facultyClient.GetAsync("/Grievances/Escalations");
        CampusConnectTestFactory.AssertAccessDenied(facultyResponse);
    }

    [Fact]
    public async Task Grievances_Escalations_HodAndAdminCanAccess()
    {
        var hodClient = await _factory.CreateAuthenticatedClientAsync("hod.cse@campusconnect.edu");
        var hodResponse = await hodClient.GetAsync("/Grievances/Escalations");
        Assert.Equal(HttpStatusCode.OK, hodResponse.StatusCode);

        var adminClient = await _factory.CreateAuthenticatedClientAsync("admin@campusconnect.edu");
        var adminResponse = await adminClient.GetAsync("/Grievances/Escalations");
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
    }
}
