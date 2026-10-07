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

        // Ensure auxiliary tables for SQLite
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
                new Department { DepartmentName = "Computer Engineering", DepartmentCode = "CSE" },
                new Department { DepartmentName = "Electrical Engineering", DepartmentCode = "EE" },
                new Department { DepartmentName = "Information Technology", DepartmentCode = "IT" },
                new Department { DepartmentName = "Civil Engineering", DepartmentCode = "CE" },
                new Department { DepartmentName = "Mechanical Engineering", DepartmentCode = "ME" },
                new Department { DepartmentName = "Mathematics", DepartmentCode = "MATH" }
            };
            await dbContext.Departments.AddRangeAsync(departments);
            await dbContext.SaveChangesAsync();
        }

        var cseDept = await dbContext.Departments.FirstOrDefaultAsync(d => d.DepartmentCode == "CSE");
        var eeDept = await dbContext.Departments.FirstOrDefaultAsync(d => d.DepartmentCode == "EE");
        var itDept = await dbContext.Departments.FirstOrDefaultAsync(d => d.DepartmentCode == "IT");
        var ceDept = await dbContext.Departments.FirstOrDefaultAsync(d => d.DepartmentCode == "CE");
        var meDept = await dbContext.Departments.FirstOrDefaultAsync(d => d.DepartmentCode == "ME");
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
                new Skill { SkillName = "Problem Solving", Category = "Soft Skill" },
                new Skill { SkillName = "UI/UX Design", Category = "Design" },
                new Skill { SkillName = "IoT & Hardware", Category = "Electronics" },
                new Skill { SkillName = "CAD & Structural Analysis", Category = "Engineering" }
            };
            await dbContext.Skills.AddRangeAsync(defaultSkills);
            await dbContext.SaveChangesAsync();
        }

        var defaultPassword = config["SeedPassword"] ?? "Password123!";

        // 4. Seed Admin User
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

        // 5. Seed HOD User
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
                        Designation = "Head of Department - Computer Engineering",
                        CabinNumber = "CSE-101"
                    };
                    await dbContext.FacultyProfiles.AddAsync(hodProfile);
                    await dbContext.SaveChangesAsync();
                }
            }
        }

        // 5b. Seed HOD for Electrical Engineering
        var hodEeEmail = "hod.ee@campusconnect.edu";
        User? hodEe = await userManager.FindByEmailAsync(hodEeEmail);
        if (hodEe == null)
        {
            hodEe = new User
            {
                UserName = hodEeEmail,
                Email = hodEeEmail,
                EmailConfirmed = true,
                Role = UserRole.Hod,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            var hodEeResult = await userManager.CreateAsync(hodEe, defaultPassword);
            if (hodEeResult.Succeeded)
            {
                await userManager.AddToRoleAsync(hodEe, UserRole.Hod.ToString());
                if (eeDept != null)
                {
                    await dbContext.FacultyProfiles.AddAsync(new FacultyProfile
                    {
                        UserId = hodEe.Id,
                        DepartmentId = eeDept.DepartmentId,
                        Designation = "Head of Department - Electrical Engineering",
                        CabinNumber = "EE-101"
                    });
                    await dbContext.SaveChangesAsync();
                }
            }
        }

        // 5c. Seed HOD for Information Technology
        var hodItEmail = "hod.it@campusconnect.edu";
        User? hodIt = await userManager.FindByEmailAsync(hodItEmail);
        if (hodIt == null)
        {
            hodIt = new User
            {
                UserName = hodItEmail,
                Email = hodItEmail,
                EmailConfirmed = true,
                Role = UserRole.Hod,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            var hodItResult = await userManager.CreateAsync(hodIt, defaultPassword);
            if (hodItResult.Succeeded)
            {
                await userManager.AddToRoleAsync(hodIt, UserRole.Hod.ToString());
                if (itDept != null)
                {
                    await dbContext.FacultyProfiles.AddAsync(new FacultyProfile
                    {
                        UserId = hodIt.Id,
                        DepartmentId = itDept.DepartmentId,
                        Designation = "Head of Department - Information Technology",
                        CabinNumber = "IT-101"
                    });
                    await dbContext.SaveChangesAsync();
                }
            }
        }

        // 5d. Seed HOD for Civil Engineering
        var hodCeEmail = "hod.ce@campusconnect.edu";
        User? hodCe = await userManager.FindByEmailAsync(hodCeEmail);
        if (hodCe == null)
        {
            hodCe = new User
            {
                UserName = hodCeEmail,
                Email = hodCeEmail,
                EmailConfirmed = true,
                Role = UserRole.Hod,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            var hodCeResult = await userManager.CreateAsync(hodCe, defaultPassword);
            if (hodCeResult.Succeeded)
            {
                await userManager.AddToRoleAsync(hodCe, UserRole.Hod.ToString());
                if (ceDept != null)
                {
                    await dbContext.FacultyProfiles.AddAsync(new FacultyProfile
                    {
                        UserId = hodCe.Id,
                        DepartmentId = ceDept.DepartmentId,
                        Designation = "Head of Department - Civil Engineering",
                        CabinNumber = "CE-101"
                    });
                    await dbContext.SaveChangesAsync();
                }
            }
        }

        // 5e. Seed HOD for Mechanical Engineering
        var hodMeEmail = "hod.me@campusconnect.edu";
        User? hodMe = await userManager.FindByEmailAsync(hodMeEmail);
        if (hodMe == null)
        {
            hodMe = new User
            {
                UserName = hodMeEmail,
                Email = hodMeEmail,
                EmailConfirmed = true,
                Role = UserRole.Hod,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            var hodMeResult = await userManager.CreateAsync(hodMe, defaultPassword);
            if (hodMeResult.Succeeded)
            {
                await userManager.AddToRoleAsync(hodMe, UserRole.Hod.ToString());
                if (meDept != null)
                {
                    await dbContext.FacultyProfiles.AddAsync(new FacultyProfile
                    {
                        UserId = hodMe.Id,
                        DepartmentId = meDept.DepartmentId,
                        Designation = "Head of Department - Mechanical Engineering",
                        CabinNumber = "ME-101"
                    });
                    await dbContext.SaveChangesAsync();
                }
            }
        }

        // 6. Seed Faculty & Students (Dev environment & Seeding)
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
                        ProfileCompletionScore = 85
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

        // 6b. Seed Faculty for Electrical Engineering
        var faculty2Email = "faculty.patel@campusconnect.edu";
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
            var f2Result = await userManager.CreateAsync(faculty2, defaultPassword);
            if (f2Result.Succeeded)
            {
                await userManager.AddToRoleAsync(faculty2, UserRole.Faculty.ToString());
                if (eeDept != null)
                {
                    await dbContext.FacultyProfiles.AddAsync(new FacultyProfile
                    {
                        UserId = faculty2.Id,
                        DepartmentId = eeDept.DepartmentId,
                        Designation = "Assistant Professor",
                        CabinNumber = "EE-204"
                    });
                    await dbContext.SaveChangesAsync();
                }
            }
        }

        // 6c. Seed Student for Electrical Engineering
        var student3Email = "student.priya@campusconnect.edu";
        User? student3 = await userManager.FindByEmailAsync(student3Email);
        if (student3 == null)
        {
            student3 = new User
            {
                UserName = student3Email,
                Email = student3Email,
                EmailConfirmed = true,
                Role = UserRole.Student,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            var s3Result = await userManager.CreateAsync(student3, defaultPassword);
            if (s3Result.Succeeded)
            {
                await userManager.AddToRoleAsync(student3, UserRole.Student.ToString());
                if (eeDept != null)
                {
                    await dbContext.StudentProfiles.AddAsync(new StudentProfile
                    {
                        UserId = student3.Id,
                        RollNumber = "EE202401",
                        DepartmentId = eeDept.DepartmentId,
                        BatchYear = 2024,
                        Bio = "Electrical engineering student focused on renewable energy and IoT systems.",
                        ProfileCompletionScore = 78
                    });
                    await dbContext.SaveChangesAsync();
                }
            }
        }

        // 7. Seed Faculty Office Hours
        if (faculty1 != null && !await dbContext.FacultyOfficeHours.AnyAsync())
        {
            var officeHours = new List<FacultyOfficeHour>
            {
                new FacultyOfficeHour
                {
                    FacultyUserId = faculty1.Id,
                    StartTime = DateTime.UtcNow.AddDays(1).Date.AddHours(10),
                    EndTime = DateTime.UtcNow.AddDays(1).Date.AddHours(11),
                    IsBooked = false
                }
            };
            await dbContext.FacultyOfficeHours.AddRangeAsync(officeHours);
            await dbContext.SaveChangesAsync();
        }

        // 8. Seed Grievances
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
            await dbContext.Grievances.AddAsync(grievance1);
            await dbContext.SaveChangesAsync();
        }

        // 9. Seed Announcements
        if (!await dbContext.Announcements.AnyAsync() && hodUser != null)
        {
            var announcement = new Announcement
            {
                AnnouncementId = Guid.NewGuid(),
                AuthorUserId = hodUser.Id,
                DepartmentId = null,
                Title = "Annual Campus Innovation & Hackathon 2026 Announced",
                Content = "We are thrilled to announce the CampusConnect Annual Hackathon 2026. Teams from all academic departments are invited to register.",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            };
            await dbContext.Announcements.AddAsync(announcement);
            await dbContext.SaveChangesAsync();
        }

        // 10. Seed Opportunities (Clean checks before insertion to prevent duplicate inserts on restart)
        if (!await dbContext.Opportunities.AnyAsync() && faculty1 != null)
        {
            var csharpSkill = await dbContext.Skills.FirstOrDefaultAsync(s => s.SkillName == "C#");
            var pythonSkill = await dbContext.Skills.FirstOrDefaultAsync(s => s.SkillName == "Python");
            var iotSkill = await dbContext.Skills.FirstOrDefaultAsync(s => s.SkillName == "IoT & Hardware");
            var uiSkill = await dbContext.Skills.FirstOrDefaultAsync(s => s.SkillName == "UI/UX Design");
            var cadSkill = await dbContext.Skills.FirstOrDefaultAsync(s => s.SkillName == "CAD & Structural Analysis");

            var opportunities = new List<Opportunity>
            {
                // Opportunity 1 (Computer Engineering)
                new Opportunity
                {
                    OpportunityId = Guid.NewGuid(),
                    OrganizerId = faculty1.Id,
                    Title = "AI-Driven Campus Bot Development",
                    Description = "R&D project building an intelligent conversational bot for student queries, campus navigation, and automated academic schedule reminders.",
                    Category = OpportunityCategory.ResearchAndDevelopment,
                    TargetDepartmentId = cseDept?.DepartmentId,
                    WorkMode = WorkMode.Hybrid,
                    StipendSalary = "$500 / month",
                    RegistrationDeadline = DateTime.UtcNow.AddDays(14),
                    EventDate = DateTime.UtcNow.AddDays(20),
                    Capacity = 3,
                    ApprovalStatus = ApprovalStatus.Approved
                },

                // Opportunity 2 (Electrical Engineering)
                new Opportunity
                {
                    OpportunityId = Guid.NewGuid(),
                    OrganizerId = faculty1.Id,
                    Title = "Solar Powered IoT Weather Station",
                    Description = "Hardware project designing and deploying solar-powered IoT micro-weather stations across university campus grounds.",
                    Category = OpportunityCategory.HardwareProject,
                    TargetDepartmentId = eeDept?.DepartmentId,
                    WorkMode = WorkMode.Onsite,
                    StipendSalary = "$600 / month + Lab Pass",
                    RegistrationDeadline = DateTime.UtcNow.AddDays(20),
                    Capacity = 4,
                    ApprovalStatus = ApprovalStatus.Approved
                },

                // Opportunity 3 (Information Technology)
                new Opportunity
                {
                    OpportunityId = Guid.NewGuid(),
                    OrganizerId = faculty1.Id,
                    Title = "Annual Tech Fest UI/UX Redesign",
                    Description = "Design & media project focusing on wireframing, interactive prototyping, and frontend UI redesign for the main campus Tech Fest portal.",
                    Category = OpportunityCategory.DesignAndMedia,
                    TargetDepartmentId = itDept?.DepartmentId,
                    WorkMode = WorkMode.Remote,
                    StipendSalary = "Certificate & $400 Stipend",
                    RegistrationDeadline = DateTime.UtcNow.AddDays(10),
                    Capacity = 2,
                    ApprovalStatus = ApprovalStatus.Approved
                },

                // Opportunity 4 (Civil Engineering)
                new Opportunity
                {
                    OpportunityId = Guid.NewGuid(),
                    OrganizerId = faculty1.Id,
                    Title = "Structural Load Analysis Workshop Assistant",
                    Description = "Lab assistance opportunity helping prepare simulation software, load-testing models, and assisting students during stress-strain testing labs.",
                    Category = OpportunityCategory.LabAssistance,
                    TargetDepartmentId = ceDept?.DepartmentId,
                    WorkMode = WorkMode.Onsite,
                    StipendSalary = "$350 / month",
                    RegistrationDeadline = DateTime.UtcNow.AddDays(15),
                    EventDate = DateTime.UtcNow.AddDays(18),
                    Capacity = 5,
                    ApprovalStatus = ApprovalStatus.Approved
                },

                // Opportunity 5 (Mechanical Engineering) - Kept PendingReview for HOD approval demo live!
                new Opportunity
                {
                    OpportunityId = Guid.NewGuid(),
                    OrganizerId = faculty1.Id,
                    Title = "Robotics Competition Operations Volunteer",
                    Description = "Event management role overseeing logistics, arena setup, safety compliance, and team coordination during the upcoming State Robotics Expo.",
                    Category = OpportunityCategory.EventManagement,
                    TargetDepartmentId = meDept?.DepartmentId,
                    WorkMode = WorkMode.Onsite,
                    StipendSalary = "Volunteer Certificate & Food Passes",
                    RegistrationDeadline = DateTime.UtcNow.AddDays(25),
                    EventDate = DateTime.UtcNow.AddDays(30),
                    Capacity = 8,
                    ApprovalStatus = ApprovalStatus.PendingReview
                }
            };

            await dbContext.Opportunities.AddRangeAsync(opportunities);
            await dbContext.SaveChangesAsync();

            // Link skills
            if (pythonSkill != null) dbContext.OpportunitySkills.Add(new OpportunitySkill { OpportunityId = opportunities[0].OpportunityId, SkillId = pythonSkill.SkillId });
            if (csharpSkill != null) dbContext.OpportunitySkills.Add(new OpportunitySkill { OpportunityId = opportunities[0].OpportunityId, SkillId = csharpSkill.SkillId });
            if (iotSkill != null) dbContext.OpportunitySkills.Add(new OpportunitySkill { OpportunityId = opportunities[1].OpportunityId, SkillId = iotSkill.SkillId });
            if (uiSkill != null) dbContext.OpportunitySkills.Add(new OpportunitySkill { OpportunityId = opportunities[2].OpportunityId, SkillId = uiSkill.SkillId });
            if (cadSkill != null) dbContext.OpportunitySkills.Add(new OpportunitySkill { OpportunityId = opportunities[3].OpportunityId, SkillId = cadSkill.SkillId });

            await dbContext.SaveChangesAsync();
        }

        // 11. Seed Applications (Clean check)
        if (!await dbContext.Applications.AnyAsync() && student1Profile != null)
        {
            var approvedOpp = await dbContext.Opportunities.FirstOrDefaultAsync(o => o.ApprovalStatus == ApprovalStatus.Approved);
            if (approvedOpp != null)
            {
                var app = new Application
                {
                    ApplicationId = Guid.NewGuid(),
                    OpportunityId = approvedOpp.OpportunityId,
                    StudentId = student1Profile.ProfileId,
                    Status = ApplicationStatus.Applied,
                    AppliedAt = DateTime.UtcNow.AddDays(-1)
                };
                await dbContext.Applications.AddAsync(app);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
