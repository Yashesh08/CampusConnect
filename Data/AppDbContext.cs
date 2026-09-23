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
    public DbSet<FacultyProfile> FacultyProfiles => Set<FacultyProfile>();
    public DbSet<FacultyOfficeHour> FacultyOfficeHours => Set<FacultyOfficeHour>();
    public DbSet<Grievance> Grievances => Set<Grievance>();
    public DbSet<GrievanceLog> GrievanceLogs => Set<GrievanceLog>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<Application> Applications => Set<Application>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // StudentProfile <-> User (1 to 1)
        modelBuilder.Entity<StudentProfile>()
            .HasOne(sp => sp.User)
            .WithOne(u => u.StudentProfile)
            .HasForeignKey<StudentProfile>(sp => sp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // StudentProfile <-> Department (Many to 1)
        modelBuilder.Entity<StudentProfile>()
            .HasOne(sp => sp.Department)
            .WithMany(d => d.StudentProfiles)
            .HasForeignKey(sp => sp.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // FacultyProfile <-> User (1 to 1)
        modelBuilder.Entity<FacultyProfile>()
            .HasOne(fp => fp.User)
            .WithOne(u => u.FacultyProfile)
            .HasForeignKey<FacultyProfile>(fp => fp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // FacultyProfile <-> Department (Many to 1)
        modelBuilder.Entity<FacultyProfile>()
            .HasOne(fp => fp.Department)
            .WithMany(d => d.FacultyProfiles)
            .HasForeignKey(fp => fp.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // FacultyOfficeHour relationships
        modelBuilder.Entity<FacultyOfficeHour>()
            .HasOne(foh => foh.FacultyUser)
            .WithMany()
            .HasForeignKey(foh => foh.FacultyUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FacultyOfficeHour>()
            .HasOne(foh => foh.BookedByStudent)
            .WithMany()
            .HasForeignKey(foh => foh.BookedByStudentId)
            .OnDelete(DeleteBehavior.SetNull);

        // Grievance relationships
        modelBuilder.Entity<Grievance>()
            .HasOne(g => g.ComplainantUser)
            .WithMany()
            .HasForeignKey(g => g.ComplainantUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Grievance>()
            .HasOne(g => g.AssignedToUser)
            .WithMany()
            .HasForeignKey(g => g.AssignedToUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Grievance>()
            .HasOne(g => g.Department)
            .WithMany()
            .HasForeignKey(g => g.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // GrievanceLog relationships
        modelBuilder.Entity<GrievanceLog>()
            .HasOne(gl => gl.Grievance)
            .WithMany(g => g.Logs)
            .HasForeignKey(gl => gl.GrievanceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GrievanceLog>()
            .HasOne(gl => gl.UpdatedByUser)
            .WithMany()
            .HasForeignKey(gl => gl.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

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
            .WithMany()
            .HasForeignKey(os => os.SkillId);

        // Configure Opportunity relationships
        modelBuilder.Entity<Opportunity>()
            .HasOne(o => o.Organizer)
            .WithMany()
            .HasForeignKey(o => o.OrganizerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Opportunity>()
            .HasOne(o => o.TargetDepartment)
            .WithMany()
            .HasForeignKey(o => o.TargetDepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure Application relationships
        modelBuilder.Entity<Application>()
            .HasOne(a => a.Opportunity)
            .WithMany(o => o.Applications)
            .HasForeignKey(a => a.OpportunityId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Application>()
            .HasOne(a => a.Student)
            .WithMany()
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
