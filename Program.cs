using CampusConnect.Data;

using CampusConnect.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using CampusConnect.Models;
using CampusConnect.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));


builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<ISkillRepository, SkillRepository>();
builder.Services.AddScoped<IStudentProfileRepository, StudentProfileRepository>();
builder.Services.AddScoped<IOpportunityRepository, OpportunityRepository>();
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<ISkillMatchingEngine, SkillMatchingEngine>();

builder.Services.AddIdentity<User, IdentityRole<Guid>>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultUI()
    .AddDefaultTokenProviders();
builder.Services.AddRazorPages();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();
// Ensure DB is created/migrated and seed sample data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    if (!db.Departments.Any())
    {
        db.Departments.Add(new CampusConnect.Models.Department { DepartmentName = "Computer Science", DepartmentCode = "CSE" });
        db.Departments.Add(new CampusConnect.Models.Department { DepartmentName = "Mathematics", DepartmentCode = "MATH" });
        db.Departments.Add(new CampusConnect.Models.Department { DepartmentName = "Electrical Engineering", DepartmentCode = "EE" });
        db.SaveChanges();
    }

    if (!db.Skills.Any())
    {
        db.Skills.Add(new CampusConnect.Models.Skill { SkillName = "C# / .NET", Category = "Software Development" });
        db.Skills.Add(new CampusConnect.Models.Skill { SkillName = "Python", Category = "Software Development" });
        db.Skills.Add(new CampusConnect.Models.Skill { SkillName = "React", Category = "Frontend" });
        db.Skills.Add(new CampusConnect.Models.Skill { SkillName = "SQL / Database", Category = "Backend" });
        db.Skills.Add(new CampusConnect.Models.Skill { SkillName = "Event Management", Category = "Soft Skills" });
        db.SaveChanges();
    }

    // Seed dummy organizer user if none exists
    var userManager = services.GetService<UserManager<User>>();
    if (userManager != null && !db.Users.Any())
    {
        var defaultOrganizer = new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            UserName = "organizer@campusconnect.edu",
            Email = "organizer@campusconnect.edu",
            Role = CampusConnect.Models.Enums.UserRole.Faculty,
            EmailConfirmed = true
        };
        var defaultStudent = new User
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            UserName = "student@campusconnect.edu",
            Email = "student@campusconnect.edu",
            Role = CampusConnect.Models.Enums.UserRole.Student,
            EmailConfirmed = true
        };
        userManager.CreateAsync(defaultOrganizer, "Password123!").GetAwaiter().GetResult();
        userManager.CreateAsync(defaultStudent, "Password123!").GetAwaiter().GetResult();

        if (!db.StudentProfiles.Any(sp => sp.UserId == defaultStudent.Id))
        {
            db.StudentProfiles.Add(new StudentProfile
            {
                ProfileId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                UserId = defaultStudent.Id,
                DepartmentId = db.Departments.First().DepartmentId,
                RollNumber = "CS2024-001",
                BatchYear = 2024,
                Bio = "Computer Science enthusiast eager to participate in campus hackathons and research projects.",
                GitHubUrl = "https://github.com/sammiller",
                LinkedInUrl = "https://linkedin.com/in/sammiller"
            });
            db.SaveChanges();
        }
    }

    if (!db.Opportunities.Any())
    {
        var organizerId = db.Users.FirstOrDefault(u => u.Role == CampusConnect.Models.Enums.UserRole.Faculty)?.Id ?? Guid.NewGuid();
        var cseDept = db.Departments.FirstOrDefault(d => d.DepartmentCode == "CSE");
        var csharpSkill = db.Skills.FirstOrDefault(s => s.SkillName == "C# / .NET");
        var pythonSkill = db.Skills.FirstOrDefault(s => s.SkillName == "Python");

        var opp1 = new Opportunity
        {
            OpportunityId = Guid.NewGuid(),
            OrganizerId = organizerId,
            Title = "AI & Machine Learning Workshop",
            Description = "Join the CS Department ML laboratory for a semester-long project on predictive analytics in campus resource management.",
            Category = CampusConnect.Models.Enums.OpportunityCategory.Workshop,
            TargetDepartmentId = cseDept?.DepartmentId,
            WorkMode = CampusConnect.Models.Enums.WorkMode.Hybrid,
            StipendSalary = "$500 / month",
            RegistrationDeadline = DateTime.UtcNow.AddDays(14),
            Capacity = 5,
            ApprovalStatus = CampusConnect.Models.Enums.ApprovalStatus.Approved
        };

        var opp2 = new Opportunity
        {
            OpportunityId = Guid.NewGuid(),
            OrganizerId = organizerId,
            Title = "Annual Campus Hackathon Co-Organizer",
            Description = "Help plan, market, and execute the upcoming 48-hour Hackathon. Looking for enthusiastic students with leadership and event planning skills.",
            Category = CampusConnect.Models.Enums.OpportunityCategory.Hackathon,
            TargetDepartmentId = null,
            WorkMode = CampusConnect.Models.Enums.WorkMode.Onsite,
            StipendSalary = "Certificate & Meal Vouchers",
            RegistrationDeadline = DateTime.UtcNow.AddDays(7),
            Capacity = 10,
            ApprovalStatus = CampusConnect.Models.Enums.ApprovalStatus.Approved
        };

        var opp3 = new Opportunity
        {
            OpportunityId = Guid.NewGuid(),
            OrganizerId = organizerId,
            Title = "Full-Stack Web Developer Internship",
            Description = "Summer internship creating ASP.NET Core web portals for internal university administration toolkits.",
            Category = CampusConnect.Models.Enums.OpportunityCategory.Internship,
            TargetDepartmentId = cseDept?.DepartmentId,
            WorkMode = CampusConnect.Models.Enums.WorkMode.Remote,
            StipendSalary = "$1200 / month",
            RegistrationDeadline = DateTime.UtcNow.AddDays(20),
            Capacity = 3,
            ApprovalStatus = CampusConnect.Models.Enums.ApprovalStatus.PendingReview
        };

        db.Opportunities.AddRange(opp1, opp2, opp3);
        db.SaveChanges();

        if (csharpSkill != null)
        {
            db.OpportunitySkills.Add(new OpportunitySkill { OpportunityId = opp1.OpportunityId, SkillId = csharpSkill.SkillId });
            db.OpportunitySkills.Add(new OpportunitySkill { OpportunityId = opp3.OpportunityId, SkillId = csharpSkill.SkillId });
        }
        if (pythonSkill != null)
        {
            db.OpportunitySkills.Add(new OpportunitySkill { OpportunityId = opp1.OpportunityId, SkillId = pythonSkill.SkillId });
        }
        db.SaveChanges();
    }
}

app.UseStaticFiles();
app.UseSession();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
