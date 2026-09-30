using System.Net;
using CampusConnect.Data;
using CampusConnect.Models;
using CampusConnect.Models.Enums;
using CampusConnect.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CampusConnect.Tests;

public class ConcurrencyAndSecurityHardeningTests : IAsyncLifetime
{
    private readonly CampusConnectTestFactory _factory = new("hardening");
    private Guid _approvedOppId;
    private Guid _organizerUserId;

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var faculty = await db.Users.FirstAsync(u => u.Email == "faculty.smith@campusconnect.edu");
        var dept = await db.Departments.FirstAsync(d => d.DepartmentCode == "CSE");

        _organizerUserId = faculty.Id;

        var opp = new Opportunity
        {
            OpportunityId = Guid.NewGuid(),
            OrganizerId = faculty.Id,
            Title = "Hardening Test Opportunity",
            Description = "A safe opportunity for concurrency and validation tests.",
            Category = OpportunityCategory.Workshop,
            TargetDepartmentId = dept.DepartmentId,
            WorkMode = WorkMode.Onsite,
            RegistrationDeadline = DateTime.UtcNow.AddDays(15),
            Capacity = 50,
            ApprovalStatus = ApprovalStatus.Approved
        };

        db.Opportunities.Add(opp);
        await db.SaveChangesAsync();

        _approvedOppId = opp.OpportunityId;
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    // ─────────────────────────────────────────────────────────────────
    // 15. CONCURRENCY: TOCTOU Double-Apply Race Attack
    // ─────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Concurrency_DoubleApply_SimultaneousRequests_DoesNotCreateDuplicateApplications()
    {
        // ATTACK: Two concurrent HTTP requests calling Apply for the same student and same opportunity
        var client1 = await _factory.CreateAuthenticatedClientAsync("student.bob@campusconnect.edu");
        var (token1, _) = await _factory.GetPageWithTokenAsync(client1, $"/Opportunities/Details/{_approvedOppId}");

        var formData1 = new Dictionary<string, string>
        {
            { "opportunityId", _approvedOppId.ToString() },
            { "__RequestVerificationToken", token1 }
        };

        var formData2 = new Dictionary<string, string>
        {
            { "opportunityId", _approvedOppId.ToString() },
            { "__RequestVerificationToken", token1 }
        };

        // Fire both HTTP requests simultaneously
        var task1 = client1.PostAsync("/Applications/Apply", new FormUrlEncodedContent(formData1));
        var task2 = client1.PostAsync("/Applications/Apply", new FormUrlEncodedContent(formData2));

        await Task.WhenAll(task1, task2);

        // Count how many applications were created for Bob
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bob = await db.Users.Include(u => u.StudentProfile).FirstAsync(u => u.Email == "student.bob@campusconnect.edu");

        var appCount = await db.Applications.CountAsync(a => a.OpportunityId == _approvedOppId && a.StudentId == bob.StudentProfile!.ProfileId);

        // Record the exact concurrency result:
        // Expected: At most 1 application created (no duplicates)
        Assert.True(appCount <= 1, $"CONCURRENCY DEFECT: Found {appCount} applications created for the same student on the same opportunity!");
    }

    // ─────────────────────────────────────────────────────────────────
    // 16. OVERPOSTING / MASS ASSIGNMENT ATTACKS
    // ─────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Overposting_OpportunityCreate_CannotForceApprovalStatusOrOrganizer()
    {
        // ATTACK: Faculty submits Create Opportunity with hidden/tampered ApprovalStatus=Approved and OrganizerId=<Admin>
        var client = await _factory.CreateAuthenticatedClientAsync("faculty.smith@campusconnect.edu");
        var (token, _) = await _factory.GetPageWithTokenAsync(client, "/Opportunities/Create");

        var formData = new Dictionary<string, string>
        {
            { "Title", "Overposting Exploit Test Opportunity" },
            { "Description", "Testing if mass assignment sets ApprovalStatus to Approved" },
            { "Category", OpportunityCategory.Workshop.ToString() },
            { "WorkMode", WorkMode.Remote.ToString() },
            { "RegistrationDeadline", DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd") },
            { "Capacity", "5" },
            // Overposting attack fields:
            { "ApprovalStatus", ApprovalStatus.Approved.ToString() },
            { "OrganizerId", "00000000-0000-0000-0000-000000000001" },
            { "__RequestVerificationToken", token }
        };

        var response = await client.PostAsync("/Opportunities/Create", new FormUrlEncodedContent(formData));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var created = await db.Opportunities.FirstOrDefaultAsync(o => o.Title == "Overposting Exploit Test Opportunity");

        Assert.NotNull(created);
        // Must remain PendingReview!
        Assert.Equal(ApprovalStatus.PendingReview, created.ApprovalStatus);
        // Organizer must be the authenticated user's ID, NOT the forged ID!
        Assert.Equal(_organizerUserId, created.OrganizerId);
    }

    // ─────────────────────────────────────────────────────────────────
    // 17. XSS: Harmless HTML Payload Verification
    // ─────────────────────────────────────────────────────────────────
    [Fact]
    public async Task XSS_OpportunityTitleAndDescription_AreSafelyEncoded()
    {
        // ATTACK: Submit an opportunity with script tags in Title and Description
        var adminClient = await _factory.CreateAuthenticatedClientAsync("admin@campusconnect.edu");
        var (token, _) = await _factory.GetPageWithTokenAsync(adminClient, "/Opportunities/Create");

        string xssPayload = "<script>console.log('XSS_HARMFUL_TEST')</script>";
        string imgPayload = "<img src=x onerror=console.log(1)>";

        var formData = new Dictionary<string, string>
        {
            { "Title", xssPayload },
            { "Description", imgPayload },
            { "Category", OpportunityCategory.Workshop.ToString() },
            { "WorkMode", WorkMode.Remote.ToString() },
            { "RegistrationDeadline", DateTime.UtcNow.AddDays(14).ToString("yyyy-MM-dd") },
            { "Capacity", "10" },
            { "__RequestVerificationToken", token }
        };

        var postResponse = await adminClient.PostAsync("/Opportunities/Create", new FormUrlEncodedContent(formData));

        // Retrieve created opportunity ID
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var opp = await db.Opportunities.FirstAsync(o => o.Title == xssPayload);
            opp.ApprovalStatus = ApprovalStatus.Approved;
            await db.SaveChangesAsync();

            // Request the Details page and Index page
            var getResponse = await adminClient.GetAsync($"/Opportunities/Details/{opp.OpportunityId}");
            var html = await getResponse.Content.ReadAsStringAsync();

            // VERIFY: The script tag must NOT be rendered as raw executable HTML!
            // Razor encodes < as &lt; and > as &gt;
            Assert.DoesNotContain("<script>console.log('XSS_HARMFUL_TEST')</script>", html);
            Assert.Contains("&lt;script&gt;console.log(&#x27;XSS_HARMFUL_TEST&#x27;)&lt;/script&gt;", html);
            Assert.DoesNotContain("<img src=x onerror=console.log(1)>", html);
        }
    }

    [Fact]
    public async Task XSS_GrievanceDescription_IsSafelyEncoded()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("student.alice@campusconnect.edu");
        var (token, _) = await _factory.GetPageWithTokenAsync(client, "/Grievances/Create");

        string xssPayload = "<script>alert('GRIEVANCE_XSS')</script>";

        var formData = new Dictionary<string, string>
        {
            { "Category", GrievanceCategory.HostelMess.ToString() },
            { "DepartmentId", "1" },
            { "Priority", GrievancePriority.Low.ToString() },
            { "Description", xssPayload },
            { "IsAnonymous", "false" },
            { "__RequestVerificationToken", token }
        };

        var response = await client.PostAsync("/Grievances/Create", new FormUrlEncodedContent(formData));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var g = await db.Grievances.FirstAsync(gr => gr.Description == xssPayload);

        var detailsResponse = await client.GetAsync($"/Grievances/Details/{g.GrievanceId}");
        var html = await detailsResponse.Content.ReadAsStringAsync();

        // Must be encoded
        Assert.DoesNotContain("<script>alert('GRIEVANCE_XSS')</script>", html);
        Assert.Contains("&lt;script&gt;alert(&#x27;GRIEVANCE_XSS&#x27;)&lt;/script&gt;", html);
    }

    // ─────────────────────────────────────────────────────────────────
    // 18 & 20. INPUT VALIDATION & ERROR HANDLING
    // ─────────────────────────────────────────────────────────────────
    [Theory]
    [InlineData("/Opportunities/Details/invalid-guid")]
    [InlineData("/Opportunities/Details/12345")]
    [InlineData("/Opportunities/Details/00000000-0000-0000-0000-000000000000")]
    [InlineData("/Grievances/Details/not-a-guid")]
    [InlineData("/Grievances/Details/00000000-0000-0000-0000-000000000000")]
    public async Task MalformedAndNonexistentIds_DoNotLeakStackTracesOrInternalExceptions(string route)
    {
        var client = await _factory.CreateAuthenticatedClientAsync("student.alice@campusconnect.edu");
        var response = await client.GetAsync(route);

        var content = await response.Content.ReadAsStringAsync();

        // Must not return 500 Internal Server Error
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);

        // Must not leak stack traces or exception details
        Assert.DoesNotContain("Exception: ", content);
        Assert.DoesNotContain("SqlException", content);
        Assert.DoesNotContain("at CampusConnect.Controllers", content);
        Assert.DoesNotContain("StackTrace", content);
    }

    // ─────────────────────────────────────────────────────────────────
    // 19. SQL INJECTION PAYLOAD TESTING
    // ─────────────────────────────────────────────────────────────────
    [Theory]
    [InlineData("' OR '1'='1")]
    [InlineData("'; DROP TABLE \"opportunities\"; --")]
    [InlineData("' UNION SELECT 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 --")]
    [InlineData("admin' --")]
    [InlineData("\" OR \"\"=\"")]
    public async Task SqlInjectionPayloads_InSearch_DoNotCorruptDatabaseOrDiscloseData(string payload)
    {
        var client = await _factory.CreateAuthenticatedClientAsync("student.alice@campusconnect.edu");
        var response = await client.GetAsync($"/Opportunities?search={Uri.EscapeDataString(payload)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify the database tables are completely intact
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        bool oppTableIntact = await db.Opportunities.AnyAsync();
        Assert.True(oppTableIntact, "Opportunities table must remain intact after SQL injection probe!");
    }

    // ─────────────────────────────────────────────────────────────────
    // 14. SQLITE DATABASE INTEGRITY: Foreign Keys and Constraints
    // ─────────────────────────────────────────────────────────────────
    [Fact]
    public async Task SQLite_ForeignKeyConstraint_EnforcedOnApplication()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var invalidApp = new Application
        {
            ApplicationId = Guid.NewGuid(),
            OpportunityId = Guid.NewGuid(), // Nonexistent Opportunity
            StudentId = Guid.NewGuid(),     // Nonexistent Student
            Status = ApplicationStatus.Applied,
            AppliedAt = DateTime.UtcNow
        };

        db.Applications.Add(invalidApp);

        // SQLite with PRAGMA foreign_keys = ON must reject this with DbUpdateException
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task SQLite_CascadeDelete_DeletesApplicationsWhenOpportunityDeleted()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var student = await db.StudentProfiles.FirstAsync();
        var faculty = await db.Users.FirstAsync(u => u.Email == "faculty.smith@campusconnect.edu");

        var opp = new Opportunity
        {
            OpportunityId = Guid.NewGuid(),
            OrganizerId = faculty.Id,
            Title = "Cascade Test Opp",
            Description = "To be deleted",
            Category = OpportunityCategory.Workshop,
            WorkMode = WorkMode.Onsite,
            RegistrationDeadline = DateTime.UtcNow.AddDays(5),
            Capacity = 5,
            ApprovalStatus = ApprovalStatus.Approved
        };

        db.Opportunities.Add(opp);

        var app = new Application
        {
            ApplicationId = Guid.NewGuid(),
            OpportunityId = opp.OpportunityId,
            StudentId = student.ProfileId,
            Status = ApplicationStatus.Applied,
            AppliedAt = DateTime.UtcNow
        };

        db.Applications.Add(app);
        await db.SaveChangesAsync();

        // Delete the opportunity
        db.Opportunities.Remove(opp);
        await db.SaveChangesAsync();

        // Application must be cascade deleted by SQLite foreign key constraint
        var orphanedApp = await db.Applications.FindAsync(app.ApplicationId);
        Assert.Null(orphanedApp);
    }
}
