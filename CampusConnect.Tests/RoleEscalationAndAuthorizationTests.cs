using System.Net;
using CampusConnect.Tests.Integration;
using Xunit;

namespace CampusConnect.Tests;

public class RoleEscalationAndAuthorizationTests : IAsyncLifetime
{
    private readonly CampusConnectTestFactory _factory = new("auth_matrix");

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    // ─────────────────────────────────────────────────────────────────
    // ANONYMOUS MATRIX: Unauthenticated requests to protected endpoints
    // ─────────────────────────────────────────────────────────────────
    [Theory]
    [InlineData("/Departments")]
    [InlineData("/Departments/Create")]
    [InlineData("/Departments/Edit/1")]
    [InlineData("/Departments/Delete/1")]
    [InlineData("/Skills")]
    [InlineData("/Skills/Create")]
    [InlineData("/Skills/Edit/1")]
    [InlineData("/Skills/Delete/1")]
    [InlineData("/StudentProfiles/Dashboard")]
    [InlineData("/StudentProfiles/Create")]
    [InlineData("/StudentProfiles/Edit")]
    [InlineData("/StudentProfiles/ManageSkills")]
    [InlineData("/FacultyProfiles/Dashboard")]
    [InlineData("/FacultyProfiles/Create")]
    [InlineData("/FacultyProfiles/Edit")]
    [InlineData("/Opportunities/Create")]
    [InlineData("/Opportunities/PendingApprovals")]
    [InlineData("/Organizer/Dashboard")]
    [InlineData("/Organizer/Stats")]
    [InlineData("/Hod/Dashboard")]
    [InlineData("/Grievances")]
    [InlineData("/Grievances/OfficerDashboard")]
    [InlineData("/Grievances/Escalations")]
    public async Task Anonymous_RedirectedToLogin_OnProtectedEndpoints(string route)
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.ToString() ?? "";
        Assert.Contains("/Identity/Account/Login", location);
    }

    // ─────────────────────────────────────────────────────────────────
    // ROLE ESCALATION: Student attacking privileged endpoints
    // ─────────────────────────────────────────────────────────────────
    [Theory]
    [InlineData("/Departments/Create")]
    [InlineData("/Departments/Edit/1")]
    [InlineData("/Departments/Delete/1")]
    [InlineData("/Skills/Create")]
    [InlineData("/Skills/Edit/1")]
    [InlineData("/Skills/Delete/1")]
    [InlineData("/FacultyProfiles/Dashboard")]
    [InlineData("/FacultyProfiles/Create")]
    [InlineData("/FacultyProfiles/Edit")]
    [InlineData("/Opportunities/Create")]
    [InlineData("/Opportunities/PendingApprovals")]
    [InlineData("/Organizer/Dashboard")]
    [InlineData("/Organizer/Stats")]
    [InlineData("/Hod/Dashboard")]
    [InlineData("/Grievances/OfficerDashboard")]
    [InlineData("/Grievances/Escalations")]
    public async Task Student_AccessingPrivilegedEndpoints_IsDenied(string route)
    {
        var client = await _factory.CreateAuthenticatedClientAsync("student.alice@campusconnect.edu");
        var response = await client.GetAsync(route);

        CampusConnectTestFactory.AssertAccessDenied(response);
    }

    // ─────────────────────────────────────────────────────────────────
    // ROLE ESCALATION: Faculty attacking Admin/HOD endpoints
    // ─────────────────────────────────────────────────────────────────
    [Theory]
    [InlineData("/Departments/Create")]
    [InlineData("/Departments/Edit/1")]
    [InlineData("/Departments/Delete/1")]
    [InlineData("/Skills/Create")]
    [InlineData("/Skills/Edit/1")]
    [InlineData("/Skills/Delete/1")]
    [InlineData("/Opportunities/PendingApprovals")]
    [InlineData("/Hod/Dashboard")]
    [InlineData("/Grievances/Escalations")]
    public async Task Faculty_AccessingAdminHodEndpoints_IsDenied(string route)
    {
        var client = await _factory.CreateAuthenticatedClientAsync("faculty.smith@campusconnect.edu");
        var response = await client.GetAsync(route);

        CampusConnectTestFactory.AssertAccessDenied(response);
    }

    // ─────────────────────────────────────────────────────────────────
    // ROLE ESCALATION: Faculty attacking Student endpoints
    // ─────────────────────────────────────────────────────────────────
    [Theory]
    [InlineData("/StudentProfiles/Dashboard")]
    [InlineData("/StudentProfiles/Create")]
    [InlineData("/StudentProfiles/Edit")]
    [InlineData("/StudentProfiles/ManageSkills")]
    [InlineData("/Applications/MyApplications")]
    public async Task Faculty_AccessingStudentEndpoints_IsDenied(string route)
    {
        var client = await _factory.CreateAuthenticatedClientAsync("faculty.smith@campusconnect.edu");
        var response = await client.GetAsync(route);

        CampusConnectTestFactory.AssertAccessDenied(response);
    }

    // ─────────────────────────────────────────────────────────────────
    // ROLE ESCALATION: EventOrganizer attacking Admin/Student endpoints
    // ─────────────────────────────────────────────────────────────────
    [Theory]
    [InlineData("/Departments/Create")]
    [InlineData("/Skills/Create")]
    [InlineData("/Opportunities/PendingApprovals")]
    [InlineData("/Hod/Dashboard")]
    [InlineData("/StudentProfiles/Dashboard")]
    [InlineData("/Applications/MyApplications")]
    public async Task EventOrganizer_AccessingUnauthorizedEndpoints_IsDenied(string route)
    {
        var client = await _factory.CreateAuthenticatedClientAsync("organizer.alpha@campusconnect.edu");
        var response = await client.GetAsync(route);

        CampusConnectTestFactory.AssertAccessDenied(response);
    }

    // ─────────────────────────────────────────────────────────────────
    // ROLE ESCALATION: ClubCoordinator attacking Admin/Student endpoints
    // ─────────────────────────────────────────────────────────────────
    [Theory]
    [InlineData("/Departments/Create")]
    [InlineData("/Skills/Create")]
    [InlineData("/Opportunities/PendingApprovals")]
    [InlineData("/Hod/Dashboard")]
    [InlineData("/StudentProfiles/Dashboard")]
    [InlineData("/Applications/MyApplications")]
    public async Task ClubCoordinator_AccessingUnauthorizedEndpoints_IsDenied(string route)
    {
        var client = await _factory.CreateAuthenticatedClientAsync("organizer.beta@campusconnect.edu");
        var response = await client.GetAsync(route);

        CampusConnectTestFactory.AssertAccessDenied(response);
    }

    // ─────────────────────────────────────────────────────────────────
    // HOD Capabilities and Restrictions
    // ─────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Hod_CanAccess_PendingApprovals_Dashboard_And_Escalations()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("hod.cse@campusconnect.edu");

        var r1 = await client.GetAsync("/Opportunities/PendingApprovals");
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);

        var r2 = await client.GetAsync("/Hod/Dashboard");
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);

        var r3 = await client.GetAsync("/Grievances/Escalations");
        Assert.Equal(HttpStatusCode.OK, r3.StatusCode);
    }

    [Theory]
    [InlineData("/Departments/Create")]
    [InlineData("/Skills/Create")]
    public async Task Hod_CannotAccess_AdminOnlyCrud(string route)
    {
        var client = await _factory.CreateAuthenticatedClientAsync("hod.cse@campusconnect.edu");
        var response = await client.GetAsync(route);

        CampusConnectTestFactory.AssertAccessDenied(response);
    }

    // ─────────────────────────────────────────────────────────────────
    // Admin Full Access
    // ─────────────────────────────────────────────────────────────────
    [Theory]
    [InlineData("/Departments")]
    [InlineData("/Departments/Create")]
    [InlineData("/Skills")]
    [InlineData("/Skills/Create")]
    [InlineData("/Opportunities/PendingApprovals")]
    [InlineData("/Hod/Dashboard")]
    [InlineData("/Grievances/Escalations")]
    public async Task Admin_CanAccess_AllPrivilegedEndpoints(string route)
    {
        var client = await _factory.CreateAuthenticatedClientAsync("admin@campusconnect.edu");
        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
