using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CampusConnect.Models;
using CampusConnect.Models.Enums;

namespace CampusConnect.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration config, IWebHostEnvironment env)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        // Ensure database is created and migrated
        await dbContext.Database.MigrateAsync();

        // Ensure tables for announcements, connections, and messages exist in SQLite
        await dbContext.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""announcements"" (
                ""announcement_id"" TEXT NOT NULL CONSTRAINT ""PK_announcements"" PRIMARY KEY,
                ""author_user_id"" TEXT NOT NULL,
                ""department_id"" INTEGER NULL,
                ""Title"" TEXT NOT NULL,
                ""Content"" TEXT NOT NULL,
                ""created_at"" TEXT NOT NULL,
                CONSTRAINT ""FK_announcements_AspNetUsers_author_user_id"" FOREIGN KEY (""author_user_id"") REFERENCES ""AspNetUsers"" (""Id"") ON DELETE CASCADE,
                CONSTRAINT ""FK_announcements_departments_department_id"" FOREIGN KEY (""department_id"") REFERENCES ""departments"" (""department_id"") ON DELETE SET NULL
            );
            CREATE TABLE IF NOT EXISTS ""connections"" (
                ""connection_id"" TEXT NOT NULL CONSTRAINT ""PK_connections"" PRIMARY KEY,
                ""sender_user_id"" TEXT NOT NULL,
                ""receiver_user_id"" TEXT NOT NULL,
                ""Status"" INTEGER NOT NULL,
                ""created_at"" TEXT NOT NULL,
                CONSTRAINT ""FK_connections_AspNetUsers_sender_user_id"" FOREIGN KEY (""sender_user_id"") REFERENCES ""AspNetUsers"" (""Id"") ON DELETE CASCADE,
                CONSTRAINT ""FK_connections_AspNetUsers_receiver_user_id"" FOREIGN KEY (""receiver_user_id"") REFERENCES ""AspNetUsers"" (""Id"") ON DELETE CASCADE
            );
            CREATE TABLE IF NOT EXISTS ""messages"" (
                ""message_id"" TEXT NOT NULL CONSTRAINT ""PK_messages"" PRIMARY KEY,
                ""sender_user_id"" TEXT NOT NULL,
                ""receiver_user_id"" TEXT NOT NULL,
                ""Content"" TEXT NOT NULL,
                ""is_read"" INTEGER NOT NULL,
                ""sent_at"" TEXT NOT NULL,
                CONSTRAINT ""FK_messages_AspNetUsers_sender_user_id"" FOREIGN KEY (""sender_user_id"") REFERENCES ""AspNetUsers"" (""Id"") ON DELETE CASCADE,
                CONSTRAINT ""FK_messages_AspNetUsers_receiver_user_id"" FOREIGN KEY (""receiver_user_id"") REFERENCES ""AspNetUsers"" (""Id"") ON DELETE CASCADE
            );
        ");

        // 1. Seed Roles
        string[] roles = Enum.GetNames<UserRole>();
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid> { Name = roleName });
            }
        }

        // 2. Seed Departments
        if (!await dbContext.Departments.AnyAsync())
        {
            var departments = new List<Department>
            {
                new Department { DepartmentName = "Computer Science & Engineering", DepartmentCode = "CSE" },
                new Department { DepartmentName = "Electrical Engineering", DepartmentCode = "EE" },
                new Department { DepartmentName = "Mechanical Engineering", DepartmentCode = "ME" },
                new Department { DepartmentName = "Mathematics", DepartmentCode = "MATH" }
            };
            await dbContext.Departments.AddRangeAsync(departments);
            await dbContext.SaveChangesAsync();
        }

        var cseDept = await dbContext.Departments.FirstOrDefaultAsync(d => d.DepartmentCode == "CSE");
        var mathDept = await dbContext.Departments.FirstOrDefaultAsync(d => d.DepartmentCode == "MATH");

        // 3. Seed Skills
        if (!await dbContext.Skills.AnyAsync())
        {
            var defaultSkills = new List<Skill>
            {
                new Skill { SkillName = "C#", Category = "Programming" },
                new Skill { SkillName = "ASP.NET Core", Category = "Backend Development" },
                new Skill { SkillName = "JavaScript", Category = "Frontend Development" },
                new Skill { SkillName = "SQL", Category = "Database" },
                new Skill { SkillName = "Python", Category = "Data Science" },
                new Skill { SkillName = "Problem Solving", Category = "Soft Skill" }
            };
            await dbContext.Skills.AddRangeAsync(defaultSkills);
            await dbContext.SaveChangesAsync();
        }

        // 4. Helper function to create users with profiles
        var defaultPassword = config["SeedPassword"] ?? "Password123!";

        // Admin User
        var adminEmail = "admin@campusconnect.edu";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                Role = UserRole.Admin,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(adminUser, defaultPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, UserRole.Admin.ToString());
            }
        }

        if (adminUser != null && cseDept != null && !await dbContext.FacultyProfiles.AnyAsync(fp => fp.UserId == adminUser.Id))
        {
            var adminProfile = new FacultyProfile
            {
                UserId = adminUser.Id,
                DepartmentId = cseDept.DepartmentId,
                Designation = "Chief System Administrator",
                CabinNumber = "Admin-HQ"
            };
            await dbContext.FacultyProfiles.AddAsync(adminProfile);
            await dbContext.SaveChangesAsync();
        }

        // HOD User
        var hodEmail = "hod.cse@campusconnect.edu";
        User? hodUser = await userManager.FindByEmailAsync(hodEmail);
        if (hodUser == null)
        {
            hodUser = new User
            {
                UserName = hodEmail,
                Email = hodEmail,
                EmailConfirmed = true,
                Role = UserRole.Hod,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(hodUser, defaultPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(hodUser, UserRole.Hod.ToString());
                if (cseDept != null)
                {
                    var hodProfile = new FacultyProfile
                    {
                        UserId = hodUser.Id,
                        DepartmentId = cseDept.DepartmentId,
                        Designation = "Head of Department - Computer Science",
                        CabinNumber = "CSE-101"
                    };
                    await dbContext.FacultyProfiles.AddAsync(hodProfile);
                    await dbContext.SaveChangesAsync();
                }
            }
        }

        // Test Users (Faculty & Students) only in Development
        if (env.IsDevelopment())
        {
            // Faculty User 1
            var faculty1Email = "faculty.smith@campusconnect.edu";
            User? faculty1 = await userManager.FindByEmailAsync(faculty1Email);
            if (faculty1 == null)
        {
            faculty1 = new User
            {
                UserName = faculty1Email,
                Email = faculty1Email,
                EmailConfirmed = true,
                Role = UserRole.Faculty,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(faculty1, defaultPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(faculty1, UserRole.Faculty.ToString());
                if (cseDept != null)
                {
                    var fp = new FacultyProfile
                    {
                        UserId = faculty1.Id,
                        DepartmentId = cseDept.DepartmentId,
                        Designation = "Associate Professor",
                        CabinNumber = "CSE-302"
                    };
                    await dbContext.FacultyProfiles.AddAsync(fp);
                    await dbContext.SaveChangesAsync();
                }
            }
        }

        // Faculty User 2
        var faculty2Email = "faculty.jones@campusconnect.edu";
        User? faculty2 = await userManager.FindByEmailAsync(faculty2Email);
        if (faculty2 == null)
        {
            faculty2 = new User
            {
                UserName = faculty2Email,
                Email = faculty2Email,
                EmailConfirmed = true,
                Role = UserRole.Faculty,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(faculty2, defaultPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(faculty2, UserRole.Faculty.ToString());
                if (mathDept != null)
                {
                    var fp = new FacultyProfile
                    {
                        UserId = faculty2.Id,
                        DepartmentId = mathDept.DepartmentId,
                        Designation = "Professor",
                        CabinNumber = "MATH-105"
                    };
                    await dbContext.FacultyProfiles.AddAsync(fp);
                    await dbContext.SaveChangesAsync();
                }
            }
        }

        // Student User 1
        var student1Email = "student.alice@campusconnect.edu";
        User? student1 = await userManager.FindByEmailAsync(student1Email);
        StudentProfile? student1Profile = null;
        if (student1 == null)
        {
            student1 = new User
            {
                UserName = student1Email,
                Email = student1Email,
                EmailConfirmed = true,
                Role = UserRole.Student,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(student1, defaultPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(student1, UserRole.Student.ToString());
                if (cseDept != null)
                {
                    student1Profile = new StudentProfile
                    {
                        UserId = student1.Id,
                        RollNumber = "CS202401",
                        DepartmentId = cseDept.DepartmentId,
                        BatchYear = 2024,
                        Bio = "Passionate computer science student interested in full-stack engineering.",
                        ProfileCompletionScore = 85,
                        GitHubUrl = "https://github.com/alicejohnson",
                        LinkedInUrl = "https://linkedin.com/in/alicejohnson"
                    };
                    await dbContext.StudentProfiles.AddAsync(student1Profile);
                    await dbContext.SaveChangesAsync();
                }
            }
        }
        else
        {
            student1Profile = await dbContext.StudentProfiles.FirstOrDefaultAsync(sp => sp.UserId == student1.Id);
        }

        // Student User 2
        var student2Email = "student.bob@campusconnect.edu";
        User? student2 = await userManager.FindByEmailAsync(student2Email);
        if (student2 == null)
        {
            student2 = new User
            {
                UserName = student2Email,
                Email = student2Email,
                EmailConfirmed = true,
                Role = UserRole.Student,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(student2, defaultPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(student2, UserRole.Student.ToString());
                if (cseDept != null)
                {
                    var sp = new StudentProfile
                    {
                        UserId = student2.Id,
                        RollNumber = "CS202402",
                        DepartmentId = cseDept.DepartmentId,
                        BatchYear = 2024,
                        Bio = "Aspiring machine learning researcher and software developer.",
                        ProfileCompletionScore = 70
                    };
                    await dbContext.StudentProfiles.AddAsync(sp);
                    await dbContext.SaveChangesAsync();
                }
            }
        }

        // 5. Seed Faculty Office Hours
        if (faculty1 != null && !await dbContext.FacultyOfficeHours.AnyAsync())
        {
            var officeHours = new List<FacultyOfficeHour>
            {
                new FacultyOfficeHour
                {
                    FacultyUserId = faculty1.Id,
                    StartTime = DateTime.UtcNow.AddDays(1).Date.AddHours(10), // Tomorrow 10:00 AM
                    EndTime = DateTime.UtcNow.AddDays(1).Date.AddHours(11),
                    IsBooked = false
                },
                new FacultyOfficeHour
                {
                    FacultyUserId = faculty1.Id,
                    StartTime = DateTime.UtcNow.AddDays(2).Date.AddHours(14), // Day after tomorrow 2:00 PM
                    EndTime = DateTime.UtcNow.AddDays(2).Date.AddHours(15),
                    IsBooked = student1Profile != null,
                    BookedByStudentId = student1Profile?.ProfileId
                }
            };
            await dbContext.FacultyOfficeHours.AddRangeAsync(officeHours);
            await dbContext.SaveChangesAsync();
        }

        // 6. Seed Grievances & Grievance Logs
        if (!await dbContext.Grievances.AnyAsync() && cseDept != null && student1 != null && hodUser != null)
        {
            var grievance1 = new Grievance
            {
                ComplainantUserId = student1.Id,
                IsAnonymous = false,
                Category = GrievanceCategory.Infrastructure,
                DepartmentId = cseDept.DepartmentId,
                Description = "Flickering monitors and network connection drops in CSE Computer Lab 2.",
                Priority = GrievancePriority.High,
                AssignedToUserId = hodUser.Id,
                Status = GrievanceStatus.InProgress,
                SlaDueAt = DateTime.UtcNow.AddDays(3),
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var grievance2 = new Grievance
            {
                ComplainantUserId = null,
                IsAnonymous = true,
                Category = GrievanceCategory.Academic,
                DepartmentId = cseDept.DepartmentId,
                Description = "Request for additional library seating and reference book copies prior to mid-term exams.",
                Priority = GrievancePriority.Medium,
                AssignedToUserId = null,
                Status = GrievanceStatus.Submitted,
                SlaDueAt = DateTime.UtcNow.AddDays(5),
                CreatedAt = DateTime.UtcNow
            };

            await dbContext.Grievances.AddRangeAsync(grievance1, grievance2);
            await dbContext.SaveChangesAsync();

            var log1 = new GrievanceLog
            {
                GrievanceId = grievance1.GrievanceId,
                UpdatedByUserId = hodUser.Id,
                StatusChangedTo = GrievanceStatus.InProgress.ToString(),
                ResolutionNote = "Assigned IT technician to inspect display cabling and switch setup.",
                Timestamp = DateTime.UtcNow.AddHours(-6)
            };

            await dbContext.GrievanceLogs.AddAsync(log1);
            await dbContext.SaveChangesAsync();
        }

        // 7. Seed Announcements
        if (!await dbContext.Announcements.AnyAsync() && hodUser != null && faculty1 != null && cseDept != null)
        {
            var announcements = new List<Announcement>
            {
                new Announcement
                {
                    AnnouncementId = Guid.NewGuid(),
                    AuthorUserId = hodUser.Id,
                    DepartmentId = null, // College-wide!
                    Title = "Annual Campus Innovation & Hackathon 2026 Announced",
                    Content = "We are thrilled to announce the CampusConnect Annual Hackathon 2026. Teams from all academic departments are invited to register. Cash prizes and internship fast-tracks will be awarded to top finishers.",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new Announcement
                {
                    AnnouncementId = Guid.NewGuid(),
                    AuthorUserId = faculty1.Id,
                    DepartmentId = cseDept.DepartmentId, // CSE Department specific
                    Title = "CSE Mid-Term Project Submissions & Code Reviews",
                    Content = "All CSE 3rd and 4th year students must submit their project repositories by next Friday. Ensure your README and deployment instructions are up to date.",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            };
            await dbContext.Announcements.AddRangeAsync(announcements);
            await dbContext.SaveChangesAsync();
        }

        // 8. Seed Connections & Messages
        if (!await dbContext.Connections.AnyAsync() && student1 != null && student2 != null && faculty1 != null)
        {
            var conn1 = new Connection
            {
                ConnectionId = Guid.NewGuid(),
                SenderUserId = student1.Id,
                ReceiverUserId = student2.Id,
                Status = ConnectionStatus.Accepted,
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            };

            var conn2 = new Connection
            {
                ConnectionId = Guid.NewGuid(),
                SenderUserId = student1.Id,
                ReceiverUserId = faculty1.Id,
                Status = ConnectionStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            await dbContext.Connections.AddRangeAsync(conn1, conn2);

            var messages = new List<Message>
            {
                new Message
                {
                    MessageId = Guid.NewGuid(),
                    SenderUserId = student1.Id,
                    ReceiverUserId = student2.Id,
                    Content = "Hey Bob! Are you participating in the upcoming Campus Hackathon?",
                    IsRead = true,
                    SentAt = DateTime.UtcNow.AddDays(-2).AddHours(1)
                },
                new Message
                {
                    MessageId = Guid.NewGuid(),
                    SenderUserId = student2.Id,
                    ReceiverUserId = student1.Id,
                    Content = "Hey Alice! Yes, absolutely. Let's form a team. I can cover backend API development.",
                    IsRead = false,
                    SentAt = DateTime.UtcNow.AddDays(-2).AddHours(2)
                }
            };

            await dbContext.Messages.AddRangeAsync(messages);
            await dbContext.SaveChangesAsync();
        }
        } // End of Development-only seeding
    }
}
