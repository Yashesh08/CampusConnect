# 🎓 CampusConnect

> A modern, comprehensive campus collaboration and opportunity discovery web platform built with **ASP.NET Core MVC (.NET 10.0)**, **Entity Framework Core**, and **SQLite**.

---

## 📖 About the Project

**CampusConnect** bridges the gap between students, faculty members, heads of departments (HODs), and campus administrators. It centralizes academic and extracurricular engagements, opportunity discovery, faculty office hours, student portfolios, and campus grievances into a single unified portal.

### 🌟 Key Features & Modules

1. **🔐 Authentication & Role-Based Access Control (RBAC)**
   - Powered by ASP.NET Core Identity with custom extended user entities.
   - Distinct roles: `Student`, `Faculty`, `Hod`, `Admin`, `EventOrganizer`, `ClubCoordinator`.
   - Role-aware navigation and contextual dashboards.

2. **🎯 Opportunity Discovery & Skill Matching**
   - Faculty, Club Coordinators, and Admins can publish campus opportunities (Internships, Workshops, Hackathons, Research projects, Volunteering).
   - **Approval Workflow**: Opportunities submitted by organizers enter `PendingReview` and require review and approval by an HOD or Admin before going live.
   - **Intelligent Skill-Matching Engine**: Calculates real-time percentage match between student skills and opportunity requirements.
   - **Advanced Filters**: Filter by Category, Department, Work Mode (Remote, Hybrid, Onsite), and Stipend.
   - **Calendar Feed**: Export opportunities and deadlines directly into iCalendar (`.ics`) format.

3. **📝 Application & Applicant Management**
   - Students can apply to opportunities with duplicate submission prevention.
   - **Applicant Tracking Dashboard**: Organizers, Faculty, and HODs/Admins can review applicants, change statuses (`Applied`, `Under Review`, `Shortlisted`, `Selected`, `Rejected`), leave internal remarks, and perform bulk status updates.
   - **CSV Export**: Export filtered applicant lists for offline processing or event planning.

4. **👤 Student & Faculty Profiles**
   - **Student Profiles**: Showcase roll number, batch year, department, bio, social links (GitHub, LinkedIn), and skills with proficiency ratings (`Beginner`, `Intermediate`, `Expert`).
   - **Faculty Office Hours**: Faculty can publish office hours; students can view and reserve consultation slots.

5. **🛡️ Grievance Redressal System**
   - Students can submit grievances under categories like Academic, Infrastructure, Hostel, etc.
   - Supports anonymous complaints and priority tagging (`Low`, `Medium`, `High`, `Urgent`).
   - HODs and Admins can track SLA deadlines, assign staff, post resolution notes, and inspect complete audit logs.

6. **🏛️ Department & Skill Catalogs**
   - Centralized management for university departments and academic/technical skill sets.

---

## 🛠️ Technology Stack

| Layer | Technology |
|---|---|
| **Framework** | ASP.NET Core MVC (.NET 10.0) |
| **Language** | C# 13 |
| **Authentication** | ASP.NET Core Identity (Cookie-based Auth + Session) |
| **ORM** | Entity Framework Core 10.0 (Code-First) |
| **Database** | SQLite (`campusconnect.db`) |
| **Design Pattern** | Repository Pattern + Service Layer + ViewModel Pattern |
| **Front-End** | Razor Views (`.cshtml`), Bootstrap 5, Vanilla CSS, Responsive UI |

---

## 🚀 Getting Started & Running the Project

### Prerequisites

Ensure you have the following installed on your machine:
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (or latest compatible .NET SDK)
- [Git](https://git-scm.com/)
- (Optional) EF Core CLI tools:
  ```powershell
  dotnet tool install --global dotnet-ef
  ```

---

### Step-by-Step Setup

#### 1. Navigate to the Project Directory
Open your terminal (PowerShell, Command Prompt, or Bash) in the project folder:
```powershell
cd CampusConnect-yashesh_213
```

#### 2. Restore Dependencies
Download and restore all required NuGet packages:
```powershell
dotnet restore
```

#### 3. Build the Solution
Verify that there are no compilation errors:
```powershell
dotnet build
```

#### 4. Database Setup & Auto-Seeding
> **Note:** You do **not** need to manually run migrations for first-time setup! 
The application includes an automated database initializer (`DbInitializer.cs`) that runs on startup in Development mode. It automatically:
- Applies all pending EF Core migrations.
- Seeds default departments and skills.
- Seeds pre-configured accounts for Admin, HOD, Faculty, and Students.
- Seeds sample opportunities, applications, and grievances.

If you ever wish to manually update the database schema:
```powershell
dotnet ef database update
```

#### 5. Run the Application
Start the development server:
```powershell
dotnet run
```
Or run with hot-reload during development:
```powershell
dotnet watch run
```

#### 6. Open in Browser
Once started, the console will display the local URL:
```text
Now listening on: http://localhost:5138
```
Open **[http://localhost:5138](http://localhost:5138)** in your web browser.

---

## 🔑 Default Test Accounts & Credentials

All seeded accounts share the same default password:

> **Universal Password:** `Password123!`

| Role | Email | Use Case / Permissions |
|---|---|---|
| **Admin** | `admin@campusconnect.edu` | Full system access, approve/reject opportunities, manage departments & skills, review grievances |
| **HOD (CSE)** | `hod.cse@campusconnect.edu` | Review/Approve department opportunities, review department grievances, view applicants |
| **Faculty** | `faculty.smith@campusconnect.edu` | Create opportunities, manage applicants, schedule office hours |
| **Faculty (Math)** | `faculty.jones@campusconnect.edu` | Secondary faculty profile and office hour listings |
| **Student (Alice)** | `student.alice@campusconnect.edu` | Browse opportunities, view skill match %, apply to opportunities, track applications, book office hours, submit grievances |
| **Student (Bob)** | `student.bob@campusconnect.edu` | Secondary student profile for testing peer applications and reviews |

---

## 🧪 Testing Roles & Workflows

### 1. As a Faculty / Organizer (`faculty.smith@campusconnect.edu`):
1. Log in and click **"Post Opportunity"** in the top navigation.
2. Fill out the opportunity details (Title, Category, Stipend, Required Skills, Deadline).
3. Submit the form. Notice the opportunity is saved with `PendingReview` status.
4. Go to **Organizer Dashboard** (`/Organizer/Dashboard`) to see posted opportunities and view applicants.

### 2. As an HOD or Admin (`hod.cse@campusconnect.edu` / `admin@campusconnect.edu`):
1. Log in and navigate to **"Pending Approvals"** (`/Opportunities/PendingApprovals`).
2. Review the submitted opportunity and click **"Approve"** or **"Reject"**.
3. Once approved, the opportunity becomes visible to all students on the public board.

### 3. As a Student (`student.alice@campusconnect.edu`):
1. Log in and navigate to **"Opportunities"** (`/Opportunities/Index`).
2. Note the **Skill Match % Badge** indicating how well your profile skills match each opportunity.
3. Click **"View Details"** on an approved opportunity and click **"Apply Now"**.
4. Check your submitted applications under **"My Applications"** (`/Applications/MyApplications`).

### 4. Managing Applicants (`faculty.smith@campusconnect.edu`):
1. Visit the **Organizer Dashboard** (`/Organizer/Dashboard`).
2. Click **"View Applicants"** for the opportunity.
3. Update an applicant's status to `Shortlisted` or `Selected`, add remarks, or click **"Export to CSV"**.

---

## 📁 Project Directory Structure

```text
CampusConnect-yashesh_213/
├── Areas/
│   └── Identity/              # ASP.NET Core Identity authentication views & pages
├── Controllers/
│   ├── ApplicationsController.cs    # Student application submission & tracking
│   ├── DepartmentsController.cs     # Department CRUD
│   ├── FacultyProfilesController.cs # Faculty directories & office hours
│   ├── GrievancesController.cs      # Grievance ticketing & resolution workflow
│   ├── HodController.cs             # HOD-specific dashboards
│   ├── HomeController.cs            # Home landing page & role routing
│   ├── OpportunitiesController.cs   # Opportunity catalog, discovery & approval
│   ├── OrganizerController.cs       # Applicant tracking, status updates & CSV export
│   ├── SkillsController.cs          # Skill catalog management
│   └── StudentProfilesController.cs # Student profiles & skill tagging
├── Data/
│   ├── AppDbContext.cs        # EF Core DbContext with Identity & entity mappings
│   └── DbInitializer.cs       # Seed data for users, roles, departments, opportunities
├── Migrations/                # Entity Framework Core schema migrations
├── Models/                    # Entity models, domain representations & enums
│   ├── Enums/                 # UserRole, OpportunityCategory, ApplicationStatus, etc.
│   └── ViewModels/            # Filter & form input ViewModels
├── Repositories/              # Interface and EF Core implementation repository classes
├── Services/                  # Business logic (SkillMatchingEngine, CalendarFeed, etc.)
├── Views/                     # Razor views for each module and layout templates
├── wwwroot/                   # Static CSS, JS, and asset files
├── appsettings.json           # SQLite connection string & configuration
└── Program.cs                 # Dependency injection, middleware & app pipeline
```

---

## 🔧 Troubleshooting & FAQs

### Port Already in Use
If port `5138` is busy or an old instance is running in the background, terminate existing processes:
```powershell
taskkill /IM CampusConnect.exe /F
```
Then start the application again with `dotnet run`.

### Reset Database
If you want to start fresh with seeded data:
1. Stop the application (`Ctrl + C`).
2. Delete the `campusconnect.db` file from the project directory:
   ```powershell
   Remove-Item campusconnect.db
   ```
3. Run `dotnet run` — the application will automatically recreate the database and re-seed all default data.

---

## 👥 Contributors
Developed as part of the **CampusConnect** Web Application Development Project.
