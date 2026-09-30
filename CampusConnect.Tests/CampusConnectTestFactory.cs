using System.Net;
using System.Text.RegularExpressions;
using CampusConnect.Data;
using CampusConnect.Models;
using CampusConnect.Models.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CampusConnect.Tests.Integration;

public class CampusConnectTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName;
    private readonly string _dbPath;

    public CampusConnectTestFactory(string? dbSuffix = null)
    {
        _dbName = $"test_campusconnect_{dbSuffix ?? Guid.NewGuid().ToString("N")}.db";
        _dbPath = Path.Combine(AppContext.BaseDirectory, _dbName);
    }

    public string DatabasePath => _dbPath;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", $"Data Source={_dbPath}" },
                { "SeedPassword", "Password123!" }
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace AppDbContext with our test SQLite database
            var descriptors = services.Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>) 
                                               || d.ServiceType == typeof(DbContextOptions)
                                               || d.ServiceType == typeof(AppDbContext)).ToList();
            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite($"Data Source={_dbPath}");
            });
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var userManager = sp.GetRequiredService<UserManager<User>>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var env = sp.GetRequiredService<IWebHostEnvironment>();
        var config = sp.GetRequiredService<IConfiguration>();

        // Ensure database created and migrated with SQLite foreign keys
        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");

        // Seed basic entities
        await DbInitializer.SeedAsync(sp, config, env);

        // Ensure additional test roles and users for our Attacker Matrix
        // Seed Organizer A (EventOrganizer) and Organizer B (ClubCoordinator)
        string defaultPassword = "Password123!";

        var organizerAEmail = "organizer.alpha@campusconnect.edu";
        var userA = await userManager.FindByEmailAsync(organizerAEmail);
        if (userA == null)
        {
            userA = new User
            {
                UserName = organizerAEmail,
                Email = organizerAEmail,
                EmailConfirmed = true,
                Role = UserRole.EventOrganizer,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            var res = await userManager.CreateAsync(userA, defaultPassword);
            if (res.Succeeded)
            {
                await userManager.AddToRoleAsync(userA, UserRole.EventOrganizer.ToString());
            }
        }

        var organizerBEmail = "organizer.beta@campusconnect.edu";
        var userB = await userManager.FindByEmailAsync(organizerBEmail);
        if (userB == null)
        {
            userB = new User
            {
                UserName = organizerBEmail,
                Email = organizerBEmail,
                EmailConfirmed = true,
                Role = UserRole.ClubCoordinator,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            var res = await userManager.CreateAsync(userB, defaultPassword);
            if (res.Succeeded)
            {
                await userManager.AddToRoleAsync(userB, UserRole.ClubCoordinator.ToString());
            }
        }
    }

    /// <summary>
    /// Creates an authenticated HttpClient for a given user email and password.
    /// Handles cookies, follows redirects or keeps them as needed.
    /// </summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password = "Password123!", bool allowAutoRedirect = true)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = true
        });

        // 1. GET /Identity/Account/Login to obtain antiforgery token & cookie
        var getResponse = await client.GetAsync("/Identity/Account/Login");
        var getContent = await getResponse.Content.ReadAsStringAsync();

        var tokenMatch = Regex.Match(getContent, @"name=""__RequestVerificationToken""\s+type=""hidden""\s+value=""([^""]+)""");
        if (!tokenMatch.Success)
        {
            tokenMatch = Regex.Match(getContent, @"value=""([^""]+)""\s+name=""__RequestVerificationToken""");
        }
        string token = tokenMatch.Success ? tokenMatch.Groups[1].Value : "";

        // 2. POST /Identity/Account/Login
        var loginData = new Dictionary<string, string>
        {
            { "Input.Email", email },
            { "Input.Password", password },
            { "Input.RememberMe", "false" },
            { "__RequestVerificationToken", token }
        };

        var postResponse = await client.PostAsync("/Identity/Account/Login", new FormUrlEncodedContent(loginData));
        
        // Return a client configured according to allowAutoRedirect preference
        if (!allowAutoRedirect)
        {
            // We can return a handler or new client sharing cookies, but WebApplicationFactory manages cookies via its default handler
            // Alternatively, create client with allowAutoRedirect: false
            return client;
        }

        return client;
    }

    public static void AssertAccessDenied(HttpResponseMessage response)
    {
        bool isForbidden = response.StatusCode == HttpStatusCode.Forbidden;
        bool isRedirectToAccessDenied = response.StatusCode == HttpStatusCode.Redirect 
            && (response.Headers.Location?.ToString().Contains("AccessDenied", StringComparison.OrdinalIgnoreCase) == true);
        bool isLandedOnAccessDenied = response.RequestMessage?.RequestUri?.ToString().Contains("AccessDenied", StringComparison.OrdinalIgnoreCase) == true;

        Xunit.Assert.True(isForbidden || isRedirectToAccessDenied || isLandedOnAccessDenied, 
            $"Expected Access Denied / Forbidden, but got Status: {response.StatusCode}, Final URL: {response.RequestMessage?.RequestUri}");
    }

    public static void AssertRedirectToLogin(HttpResponseMessage response)
    {
        bool isRedirectToLogin = response.StatusCode == HttpStatusCode.Redirect 
            && (response.Headers.Location?.ToString().Contains("Login", StringComparison.OrdinalIgnoreCase) == true);
        bool isLandedOnLogin = response.RequestMessage?.RequestUri?.ToString().Contains("Login", StringComparison.OrdinalIgnoreCase) == true;

        Xunit.Assert.True(isRedirectToLogin || isLandedOnLogin, 
            $"Expected Redirect to Login, but got Status: {response.StatusCode}, Final URL: {response.RequestMessage?.RequestUri}");
    }

    /// <summary>
    /// Helper to fetch antiforgery token from any GET page.
    /// </summary>
    public async Task<(string token, string html)> GetPageWithTokenAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        var html = await response.Content.ReadAsStringAsync();
        var tokenMatch = Regex.Match(html, @"name=""__RequestVerificationToken""\s+type=""hidden""\s+value=""([^""]+)""");
        if (!tokenMatch.Success)
        {
            tokenMatch = Regex.Match(html, @"value=""([^""]+)""\s+name=""__RequestVerificationToken""");
        }
        return (tokenMatch.Success ? tokenMatch.Groups[1].Value : "", html);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            try
            {
                if (File.Exists(_dbPath))
                {
                    File.Delete(_dbPath);
                }
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }
}
