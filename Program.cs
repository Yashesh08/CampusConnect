using CampusConnect.Data;
using CampusConnect.Middleware;
using CampusConnect.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<ISkillRepository, SkillRepository>();
builder.Services.AddScoped<IStudentProfileRepository, StudentProfileRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();
// Ensure DB is created/migrated and seed a sample Department if none exist
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    if (!db.Departments.Any())
    {
        db.Departments.Add(new CampusConnect.Models.Department { DepartmentName = "Computer Science", DepartmentCode = "CSE" });
        db.Departments.Add(new CampusConnect.Models.Department { DepartmentName = "Mathematics", DepartmentCode = "MATH" });
        db.SaveChanges();
    }
}

app.UseStaticFiles();
app.UseSession();
app.UseMiddleware<AuthenticationMiddleware>();
app.UseRouting();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
