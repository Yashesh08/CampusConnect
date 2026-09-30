using System.Net;
using CampusConnect.Tests.Integration;
using Xunit;

namespace CampusConnect.Tests;

public class SmokeIntegrationTests : IAsyncLifetime
{
    private readonly CampusConnectTestFactory _factory = new("smoke");

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Anonymous_CanAccess_HomePage()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Student_CanLogin_AndAccessDashboard()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("student.alice@campusconnect.edu");
        var response = await client.GetAsync("/StudentProfiles/Dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_RedirectedToLogin_OnProtectedStudentDashboard()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var response = await client.GetAsync("/StudentProfiles/Dashboard");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Identity/Account/Login", response.Headers.Location?.ToString());
    }
}
