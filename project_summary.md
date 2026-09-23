# CampusConnect — Week 1 & Week 2 Summary

## 📋 Project Overview

**CampusConnect** is an ASP.NET Core MVC web application (targeting **.NET 10.0**) built by a team of 3 people. The project uses **SQLite** as the database, **Entity Framework Core** for ORM, and **ASP.NET Core Identity** for authentication. The application follows the **Repository Pattern** with a **Service Layer** architecture.

**Your Role**: Person 2 — *Opportunities, Applications & Discovery*

---

## 🏗️ Project Structure (Current State)

```
CampusConnect-yashesh_213/
├── Areas/
│   └── Identity/Pages/Account/       # Login, Register, Logout (ASP.NET Identity)
├── Controllers/
│   ├── HomeController.cs              # Landing page + Skill Matching Test
│   ├── DepartmentsController.cs       # Department CRUD
│   ├── SkillsController.cs            # Skill CRUD
│   ├── StudentProfilesController.cs   # Student Profile CRUD + Skill management
│   └── OpportunitiesController.cs     # ⭐ Person 2: Create & Approval workflow
├── Data/
│   └── AppDbContext.cs                # EF Core DbContext (IdentityDbContext)
├── Migrations/
│   ├── 20260915_InitialCreate         # Week 1: Department table
│   ├── 20260915_AddSkillAndStudentProfileCrud  # Week 1: Skills + StudentProfiles
│   ├── 20260915_AddAuthentication     # Week 1: User model updates
│   ├── 20260922_AddIdentitySchema     # Week 2: Full ASP.NET Identity tables
│   ├── 20260922_AddStudentSkills      # Week 2: StudentSkill join table
│   ├── 20260922_AddOpportunitySkills  # Week 2: Opportunity + OpportunitySkill + Application tables
│   └── 20260922_SyncPendingChanges    # Fix: Sync model changes after FacultyProfile Ignore
├── Models/
│   ├── User.cs                        # Extends IdentityUser<Guid>
│   ├── Department.cs, Skill.cs        # Shared entities
│   ├── StudentProfile.cs, StudentSkill.cs  # Person 1 entities
│   ├── Opportunity.cs, OpportunitySkill.cs, Application.cs  # ⭐ Person 2 entities
│   ├── Grievance.cs, GrievanceLog.cs  # Person 3 entities (model only)
│   ├── FacultyProfile.cs, FacultyOfficeHour.cs  # Future scope (model only)
│   ├── Enums/Enums.cs                 # All shared enums
│   └── ViewModels/
│       ├── StudentProfileViewModel.cs
│       └── CreateOpportunityViewModel.cs  # ⭐ Person 2
├── Repositories/                      # Repository interfaces + implementations
│   ├── IDepartmentRepository + DepartmentRepository
│   ├── ISkillRepository + SkillRepository
│   ├── IStudentProfileRepository + StudentProfileRepository
│   ├── IOpportunityRepository + OpportunityRepository      # ⭐ Person 2
│   ├── IApplicationRepository + ApplicationRepository      # ⭐ Person 2
│   └── 10+ empty interface stubs (ICampusGroupRepository, IGrievanceRepository, etc.)
├── Services/
│   ├── ISkillMatchingEngine.cs        # Person 1 service interface
│   └── SkillMatchingEngine.cs         # Skill-to-Opportunity matching logic
├── Views/
│   ├── Home/ (Index, TestEngine)
│   ├── Departments/ (CRUD views)
│   ├── Skills/ (CRUD views)
│   ├── StudentProfiles/ (Index, Create, Edit, Delete, ManageSkills)
│   ├── Opportunities/ (Create, PendingApprovals)  # ⭐ Person 2
│   ├── Student/
│   └── Shared/ (_Layout, _LoginPartial, _ValidationScriptsPartial)
├── wwwroot/css/site.css               # Global styles
├── Program.cs                         # App configuration, DI, middleware, seed data
├── CampusConnect.csproj               # Project file with NuGet packages
├── appsettings.json                   # Connection string (SQLite)
└── docs/WAD FR - CampusConnect.pdf    # Original requirements document
```

---

## 🔥 Problems Faced & How They Were Resolved

### Problem 1: Duplicate/Nested Project Structure
| Aspect | Details |
|--------|---------|
| **Issue** | The project had a **nested directory problem** — there was a `CampusConnect-yashesh_213/` folder *inside* another `CampusConnect-yashesh_213/` folder. The outer folder contained duplicate copies of `Controllers/`, `Models/`, `Views/`, `Program.cs`, etc. alongside the inner git repo. |
| **Impact** | Confusion about which folder to run `dotnet run` from. Some files existed in both locations with slightly different content. |
| **Resolution** | Identified the inner `CampusConnect-yashesh_213/` (containing `.git/`) as the actual project root. Cleaned up by consolidating — moved the docs folder inside and removed outer duplicate files like `desktop.ini`, `ScaffoldingReadMe.txt`, and the extra `.gitignore`. |

---

### Problem 2: `dotnet-ef` Tool Not Installed
| Aspect | Details |
|--------|---------|
| **Issue** | Running `dotnet ef database update` failed with *"Could not execute because the specified command or file was not found"*. The `dotnet-ef` global tool was not installed on the machine. |
| **Impact** | Could not create new migrations or update the database schema through the CLI. |
| **Resolution** | Installed the EF Core tools globally: `dotnet tool install --global dotnet-ef` (version 10.0.12 installed successfully). |

---

### Problem 3: Pending Model Changes Migration Error
| Aspect | Details |
|--------|---------|
| **Issue** | Running `dotnet run` threw a `PendingModelChangesWarning` exception: *"The model for context 'AppDbContext' has pending changes. Add a new migration before updating the database."* |
| **Root Cause** | After adding `modelBuilder.Ignore<FacultyProfile>()` in `AppDbContext.OnModelCreating()` and configuring the `StudentProfile.User` ignore, the EF model was out of sync with the last migration (`AddOpportunitySkills`). |
| **Resolution** | Created a sync migration: `dotnet ef migrations add SyncPendingChanges`, then ran `dotnet ef database update` to apply it. The `20260922195410_SyncPendingChanges` migration was generated and successfully applied. |

---

### Problem 4: ASP.NET Identity Integration Challenges
| Aspect | Details |
|--------|---------|
| **Issue** | The original `User` model was a plain POCO class. Switching to `IdentityUser<Guid>` required significant refactoring of the `AppDbContext`, `Program.cs`, and all user-referencing code. |
| **Changes Made** | ① `AppDbContext` changed from `DbContext` → `IdentityDbContext<User, IdentityRole<Guid>, Guid>`. ② `User.cs` changed from standalone class → extends `IdentityUser<Guid>`. ③ `Program.cs` added `AddIdentity<User, IdentityRole<Guid>>()`, `.AddEntityFrameworkStores<AppDbContext>()`, `.AddDefaultUI()`, and `MapRazorPages()`. ④ A large migration (`AddIdentitySchema`) was needed to create all Identity tables (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, etc.). |
| **Resolution** | Successfully scaffolded Identity UI pages (Login, Register, Logout) under `Areas/Identity/Pages/Account/`. Added `_LoginPartial.cshtml` to the layout for sign-in/sign-out links. Added Session middleware for auth state. |

---

### Problem 5: FacultyProfile Entity Causing Migration Conflicts
| Aspect | Details |
|--------|---------|
| **Issue** | The `FacultyProfile` model existed in the `Models/` folder but wasn't meant to be used in Weeks 1-2. EF Core was trying to include it in migrations via the `User.FacultyProfile` navigation property, causing schema conflicts. |
| **Resolution** | Added `modelBuilder.Ignore<FacultyProfile>()` in `AppDbContext.OnModelCreating()` to explicitly exclude it from the EF model. Also added `.Ignore(p => p.User)` for `StudentProfile` to prevent circular navigation issues. |

---

### Problem 6: NuGet Package Vulnerability Warnings
| Aspect | Details |
|--------|---------|
| **Issue** | Every build showed warnings: *"Package 'NuGet.Packaging' 6.12.1 has a known low severity vulnerability"* and same for `NuGet.Protocol`. |
| **Impact** | Non-blocking (low severity), but cluttered the build output. |
| **Status** | Acknowledged but not addressed — these are transitive dependencies from `Microsoft.VisualStudio.Web.CodeGeneration.Design` and do not affect the application at runtime. |

---

### Problem 7: Week 3 Scope Creep → Reverted
| Aspect | Details |
|--------|---------|
| **Issue** | During implementation, Person 2's work was initially built out to include Week 3 features (ApplicationsController, OrganizerController, Discovery views, applicant management, CSV export). This was beyond the current sprint scope. |
| **Resolution** | Cleanly reverted to Week 1 & Week 2 scope by removing: `ApplicationsController.cs`, `OrganizerController.cs`, `Views/Applications/`, `Views/Organizer/`, `Views/Opportunities/Index.cshtml`, `Views/Opportunities/Details.cshtml`, `OpportunityFilterViewModel.cs`, and related navigation links from `_Layout.cshtml`. The database schema/migrations were kept intact. |

---

## ✅ Week 1 Implementation Summary

### What Was Built

| Component | Files | Description |
|-----------|-------|-------------|
| **Database Foundation** | [AppDbContext.cs](file:///c:/Users/DHYEY/Desktop/CampusConnect/CampusConnect-yashesh_213/Data/AppDbContext.cs) | Configured all `DbSet<>` properties and EF Core relationships (FK constraints, delete behaviors, many-to-many joins) |
| **All 18 Domain Models** | [Models/](file:///c:/Users/DHYEY/Desktop/CampusConnect/CampusConnect-yashesh_213/Models) | `User`, `Department`, `Skill`, `StudentProfile`, `StudentSkill`, `Opportunity`, `OpportunitySkill`, `Application`, `Grievance`, `GrievanceLog`, `FacultyProfile`, `FacultyOfficeHour`, `Connection`, `CampusGroup`, `GroupMember`, `Message`, `Notification`, `Announcement` |
| **Enums** | [Enums.cs](file:///c:/Users/DHYEY/Desktop/CampusConnect/CampusConnect-yashesh_213/Models/Enums/Enums.cs) | `UserRole`, `UserStatus`, `ProficiencyLevel`, `OpportunityCategory`, `WorkMode`, `ApprovalStatus`, `ApplicationStatus`, `GrievanceCategory`, `GrievancePriority`, `GrievanceStatus`, `ConnectionStatus` |
| **Repository Interfaces** | [Repositories/](file:///c:/Users/DHYEY/Desktop/CampusConnect/CampusConnect-yashesh_213/Repositories) | 15 interfaces created (most as stubs for future use) |
| **Implemented Repos** | `DepartmentRepository`, `SkillRepository`, `StudentProfileRepository`, `OpportunityRepository`, `ApplicationRepository` | Full async CRUD operations |
| **Seed Data** | [Program.cs](file:///c:/Users/DHYEY/Desktop/CampusConnect/CampusConnect-yashesh_213/Program.cs) | Auto-seeds 2 sample departments (CSE, MATH) on first run |
| **6 Migrations** | [Migrations/](file:///c:/Users/DHYEY/Desktop/CampusConnect/CampusConnect-yashesh_213/Migrations) | Progressive schema evolution from basic tables to full Identity + all entity tables |

### Person 2 (Your) Week 1 Deliverables
- ⭐ [Opportunity.cs](file:///c:/Users/DHYEY/Desktop/CampusConnect/CampusConnect-yashesh_213/Models/Opportunity.cs) — Full model with relationships to `User`, `Department`, `OpportunitySkill`, `Application`
- ⭐ [OpportunitySkill.cs](file:///c:/Users/DHYEY/Desktop/CampusConnect/CampusConnect-yashesh_213/Models/OpportunitySkill.cs) — Join entity for many-to-many with `Skill`
- ⭐ [Application.cs](file:///c:/Users/DHYEY/Desktop/CampusConnect/CampusConnect-yashesh_213/Models/Application.cs) — Student application entity with status workflow
- ⭐ [IOpportunityRepository.cs](file:///c:/Users/DHYEY/Desktop/CampusConnect/CampusConnect-yashesh_213/Repositories/IOpportunityRepository.cs) & [OpportunityRepository.cs](file:///c:/Users/DHYEY/Desktop/CampusConnect/CampusConnect-yashesh_213/Repositories/OpportunityRepository.cs) — Async CRUD, filtering, approval queries
- ⭐ [IApplicationRepository.cs](file:///c:/Users/DHYEY/Desktop/CampusConnect/CampusConnect-yashesh_213/Repositories/IApplicationRepository.cs) & [ApplicationRepository.cs](file:///c:/Users/DHYEY/Desktop/CampusConnect/CampusConnect-yashesh_213/Repositories/ApplicationRepository.cs) — Application submission, duplicate check, status management

---

## ✅ Week 2 Implementation Summary

### What Was Built

| Component | Files | Description |
|-----------|-------|-------------|
| **Identity Auth System** | [Areas/Identity/](file:///c:/Users/DHYEY/Desktop/CampusConnect/CampusConnect-yashesh_213/Areas/Identity) | Full Login, Register, Logout pages with ASP.NET Core Identity |
| **Layout & Navigation** | [_Layout.cshtml](file:///c:/Users/DHYEY/Desktop/CampusConnect/CampusConnect-yashesh_213/Views/Shared/_Layout.cshtml), [_LoginPartial.cshtml](file:///c:/Users/DHYEY/Desktop/CampusConnect/CampusConnect-yashesh_213/Views/Shared/_LoginPartial.cshtml) | Responsive navbar with auth links |
| **Student Profile CRUD** | [StudentProfilesController.cs](file:///c:/Users/DHYEY/Desktop/CampusConnect/CampusConnect-yashesh_213/Controllers/StudentProfilesController.cs) | Create, Edit, Delete, Index, ManageSkills |
| **Skill Matching Engine** | [SkillMatchingEngine.cs](file:///c:/Users/DHYEY/Desktop/CampusConnect/CampusConnect-yashesh_213/Services/SkillMatchingEngine.cs) | Calculates % match between student skills and opportunity requirements |
| **Skill Matching Test Page** | [TestEngine.cshtml](file:///c:/Users/DHYEY/Desktop/CampusConnect/CampusConnect-yashesh_213/Views/Home/TestEngine.cshtml) | UI to test skill matching between profiles and opportunities |
| **Global Styles** | [site.css](file:///c:/Users/DHYEY/Desktop/CampusConnect/CampusConnect-yashesh_213/wwwroot/css/site.css) | Application-wide styling |

### Person 2 (Your) Week 2 Deliverables
- ⭐ [CreateOpportunityViewModel.cs](file:///c:/Users/DHYEY/Desktop/CampusConnect/CampusConnect-yashesh_213/Models/ViewModels/CreateOpportunityViewModel.cs) — ViewModel with validation for opportunity creation form
- ⭐ [OpportunitiesController.cs](file:///c:/Users/DHYEY/Desktop/CampusConnect/CampusConnect-yashesh_213/Controllers/OpportunitiesController.cs) — `Create` (GET/POST) + `PendingApprovals` + `Approve` + `Reject` actions
- ⭐ [Create.cshtml](file:///c:/Users/DHYEY/Desktop/CampusConnect/CampusConnect-yashesh_213/Views/Opportunities/Create.cshtml) — Full form with Category, WorkMode, Deadline, Stipend, Department dropdown, Skill checkboxes
- ⭐ [PendingApprovals.cshtml](file:///c:/Users/DHYEY/Desktop/CampusConnect/CampusConnect-yashesh_213/Views/Opportunities/PendingApprovals.cshtml) — Admin/HOD table view with Approve/Reject buttons

---

## 📊 Commit Status

> [!WARNING]
> **No commits have been made yet.** The project is on branch `dhyey` with all files showing as **untracked**. Nothing has been staged or committed to git.

### Recommended Commit Strategy

| Commit # | Scope | Files to Include |
|----------|-------|------------------|
| **1** | Week 1 — Project Foundation | `.gitignore`, `CampusConnect.csproj`, `Program.cs`, `appsettings.json`, `appsettings.Development.json`, `Properties/` |
| **2** | Week 1 — Domain Models & Enums | All 18 model files in `Models/` + `Models/Enums/Enums.cs` |
| **3** | Week 1 — Database & Repositories | `Data/AppDbContext.cs`, all `Repositories/` files, all `Migrations/` |
| **4** | Week 2 — Identity & Authentication | `Areas/Identity/` (all files) |
| **5** | Week 2 — Views & UI | `Views/` (all folders), `wwwroot/css/site.css` |
| **6** | Week 2 — Person 2 Opportunity Feature | `Controllers/OpportunitiesController.cs`, `Models/ViewModels/CreateOpportunityViewModel.cs`, `Views/Opportunities/` |
| **7** | Week 2 — Services | `Services/SkillMatchingEngine.cs`, `Services/ISkillMatchingEngine.cs` |
| **8** | Docs | `docs/WAD FR - CampusConnect.pdf` |

---

## 🔮 What's Pending for Week 3 (Person 2)

Based on the implementation plan, the following items remain for your next sprint:

| Feature | Description |
|---------|-------------|
| **Opportunity Discovery (Index)** | Browse & filter approved opportunities by category, department, work mode |
| **Opportunity Details Page** | Detailed view with skills list, deadline status, Apply Now button |
| **Application Submission** | Students can apply to opportunities with duplicate prevention |
| **My Applications Tracker** | Student view to track application status, withdraw applications |
| **Organizer Dashboard** | View posted opportunities, review applicants, update statuses |
| **Applicant Management** | Inline status updates (Applied → Under Review → Shortlisted → Selected/Rejected), remarks, CSV export |
