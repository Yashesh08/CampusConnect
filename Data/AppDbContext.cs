using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CampusConnect.Models;

namespace CampusConnect.Data;

public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<StudentSkill> StudentSkills => Set<StudentSkill>();
    public DbSet<OpportunitySkill> OpportunitySkills => Set<OpportunitySkill>();
    // Users DbSet is provided by IdentityDbContext

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Keep FacultyProfile out of the model for now
        modelBuilder.Ignore<FacultyProfile>();

        modelBuilder.Entity<StudentProfile>().Ignore(p => p.User);
        
        // Configure many-to-many relationship for StudentSkills
        modelBuilder.Entity<StudentSkill>()
            .HasOne(ss => ss.Student)
            .WithMany(sp => sp.StudentSkills)
            .HasForeignKey(ss => ss.StudentId);
            
        modelBuilder.Entity<StudentSkill>()
            .HasOne(ss => ss.Skill)
            .WithMany(s => s.StudentSkills)
            .HasForeignKey(ss => ss.SkillId);
            
        // Configure many-to-many relationship for OpportunitySkills
        modelBuilder.Entity<OpportunitySkill>()
            .HasOne(os => os.Opportunity)
            .WithMany(o => o.RequiredSkills)
            .HasForeignKey(os => os.OpportunityId);
            
        modelBuilder.Entity<OpportunitySkill>()
            .HasOne(os => os.Skill)
            .WithMany() // Assuming Skill doesn't need to know all opportunities
            .HasForeignKey(os => os.SkillId);

        base.OnModelCreating(modelBuilder);
    }
}
