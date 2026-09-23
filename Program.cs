using CampusConnect.Data;
using CampusConnect.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using CampusConnect.Models;
using CampusConnect.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<ISkillRepository, SkillRepository>();
builder.Services.AddScoped<IStudentProfileRepository, StudentProfileRepository>();
builder.Services.AddScoped<IOpportunityRepository, OpportunityRepository>();
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<ISkillMatchingEngine, SkillMatchingEngine>();

builder.Services.AddIdentity<User, IdentityRole<Guid>>(options => 
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 6;
    })
    .AddRoles<IdentityRole<Guid>>()
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

// Ensure DB is created/migrated and seed default data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await DbInitializer.SeedAsync(services);

    var db = services.GetRequiredService<AppDbContext>();
    if (!db.Opportunities.Any())
    {
        var organizerUser = db.Users.FirstOrDefault(u => u.Role == CampusConnect.Models.Enums.UserRole.Faculty)
                           ?? db.Users.FirstOrDefault(u => u.Email == "faculty.smith@campusconnect.edu")
                           ?? db.Users.FirstOrDefault();
        if (organizerUser != null)
        {
            var organizerId = organizerUser.Id;
            var cseDept = db.Departments.FirstOrDefault(d => d.DepartmentCode == "CSE");
            var csharpSkill = db.Skills.FirstOrDefault(s => s.SkillName == "C#" || s.SkillName == "ASP.NET Core");
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
