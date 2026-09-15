using Microsoft.EntityFrameworkCore;
using CampusConnect.Models;

namespace CampusConnect.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Keep FacultyProfile out of the model for now
        modelBuilder.Ignore<FacultyProfile>();

        // Map StudentProfile but ignore heavy navigations we don't want to bring in
        modelBuilder.Entity<StudentProfile>().Ignore(p => p.User);
        modelBuilder.Entity<StudentProfile>().Ignore(p => p.StudentSkills);

        // Skills are standalone master data; ignore StudentSkill on Skill side
        modelBuilder.Entity<Skill>().Ignore(s => s.StudentSkills);

        base.OnModelCreating(modelBuilder);
    }
}
