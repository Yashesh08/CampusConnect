# CampusConnect — 6-Week Implementation Plan


**Team Size:** 3 Developers
**Architecture:** ASP.NET Core MVC + EF Core + SQL Server + Razor Views
**Development Model:** Vertical Feature Ownership
**Duration:** 6 Weeks
**Primary Goal:** Build a functional, integrated CampusConnect MVP while ensuring all three developers gain practical ASP.NET Core/.NET experience.


This plan follows the requirements, database entities, technology stack, and page-building sequence provided in the project specification.


---


# 1. Team Structure


## 👤 Person 1 — Identity, Profile & Recommendation


### Primary ownership


* FR-1 — User Management, Authentication & RBAC
* FR-2 — Student Profile & Skills
* FR-5 — Recommendations & Skill Gap Analysis
* Student Dashboard


### Main database entities


* [ ] `users`
* [ ] `departments`
* [ ] `student_profiles`
* [ ] `faculty_profiles`
* [ ] `skills`
* [ ] `student_skills`


### Main pages


* [ ] `/Account/Login`
* [ ] `/Account/Register`
* [ ] `/Profile/Edit`
* [ ] `/Dashboard/Student`
* [ ] Opportunity matching components


---


# 2. 👤 Person 2 — Opportunities & Applications


### Primary ownership


* FR-3 — Opportunity/Event/Internship Management
* FR-4 — Opportunity Discovery & Applications
* Organizer Dashboard


### Main database entities


* [ ] `opportunities`
* [ ] `applications`


### Main pages


* [ ] `/Opportunities/Create`
* [ ] `/Admin/Opportunities`
* [ ] `/Opportunities/Index`
* [ ] `/Opportunities/Details/{id}`
* [ ] `/Organizer/Dashboard`


---


# 3. 👤 Person 3 — Campus Services & Academic Interaction


### Primary ownership


* FR-7 — Grievance & Complaint Redressal
* FR-8 — Networking & Student Directory
* FR-9 — Faculty & Academic Interaction


### Main database entities


* [ ] `grievances`
* [ ] `grievance_logs`
* [ ] `connections`
* [ ] `messages`
* [ ] `groups`
* [ ] `group_members`
* [ ] `faculty_office_hours`
* [ ] `announcements`


### Main pages


* [ ] `/Grievances/Create`
* [ ] `/Grievances/Tracker`
* [ ] `/Directory/Index`
* [ ] `/Faculty/OfficeHours`


---


# 4. 🤝 Shared Responsibilities


Some functionality should **not belong exclusively to one person**, because it connects all modules.


All three developers should participate in:


* [ ] Git/GitHub workflow
* [ ] Code reviews
* [ ] Integration testing
* [ ] Security testing
* [ ] Database review
* [ ] Bug fixing
* [ ] Final deployment
* [ ] Documentation


Shared FR-10 functionality:


* [ ] Role-based dashboards
* [ ] Unified calendar
* [ ] Notifications
* [ ] Email notifications
* [ ] Analytics
* [ ] CSV/PDF reports


---


# 5. Development Architecture


Every developer must follow the same architecture.


```text
┌─────────────────────────┐
│       Razor View        │
└────────────┬────────────┘
             ↓
┌─────────────────────────┐
│       Controller        │
└────────────┬────────────┘
             ↓
┌─────────────────────────┐
│         Service         │
└────────────┬────────────┘
             ↓
┌─────────────────────────┐
│ Repository / EF Core    │
└────────────┬────────────┘
             ↓
┌─────────────────────────┐
│       SQL Server        │
└─────────────────────────┘
```


### Team architecture checklist


* [ ] Controllers contain request/response handling only
* [ ] Business logic goes into Services
* [ ] Database access goes through EF Core/Data Access
* [ ] ViewModels are used where appropriate
* [ ] No business logic directly inside Razor Views
* [ ] No raw SQL unless there is a documented reason
* [ ] Dependency Injection is used
* [ ] Async database operations are used
* [ ] Authorization is applied to protected actions
* [ ] Validation exists on user inputs


This directly supports the maintainability requirement in your NFRs.


---


# WEEK 1 — Project Foundation & Authentication


## 🎯 Goal


Get the common project architecture running and complete authentication/RBAC.


---


## 👤 Person 1 — Identity & Authentication


### Learn


* [ ] ASP.NET Core MVC fundamentals
* [ ] Controllers
* [ ] Razor Views
* [ ] Dependency Injection
* [ ] ASP.NET Core Identity
* [ ] Authentication
* [ ] Authorization
* [ ] Roles
* [ ] Cookies/session handling


### Implement


* [ ] Configure ASP.NET Core Identity
* [ ] Create User model
* [ ] Configure college email validation
* [ ] Registration
* [ ] Login
* [ ] Logout
* [ ] Password hashing through Identity
* [ ] Role assignment
* [ ] Role-based authorization
* [ ] Account status handling


### Roles


* [ ] Student
* [ ] Faculty
* [ ] Club Coordinator
* [ ] Event Organizer
* [ ] HOD
* [ ] Admin


---


## 👤 Person 2 — Database & EF Core Foundation


### Learn


* [ ] EF Core basics
* [ ] DbContext
* [ ] DbSet
* [ ] Code-first development
* [ ] Migrations
* [ ] Primary/foreign keys
* [ ] One-to-one relationships
* [ ] One-to-many relationships
* [ ] Many-to-many relationships
* [ ] LINQ


### Implement


* [ ] Configure SQL Server
* [ ] Create `ApplicationDbContext`
* [ ] Create initial entities
* [ ] Configure relationships
* [ ] Create initial migration
* [ ] Apply migration
* [ ] Create seed data
* [ ] Seed departments
* [ ] Seed skills
* [ ] Seed roles


---


## 👤 Person 3 — Shared UI & Project Structure


### Learn


* [ ] Razor layout
* [ ] Partial Views
* [ ] ViewModels
* [ ] Bootstrap 5 or Tailwind
* [ ] JavaScript basics
* [ ] Form validation
* [ ] AJAX basics


### Implement


* [ ] Create shared `_Layout`
* [ ] Navbar
* [ ] Sidebar
* [ ] Footer
* [ ] Login/Register styling
* [ ] Alert components
* [ ] Form components
* [ ] Table components
* [ ] Dashboard card components
* [ ] Error page
* [ ] Responsive layout


---


## 🤝 Week 1 Integration


* [ ] Everyone clones/runs the same project
* [ ] Database works locally
* [ ] Migration works
* [ ] Registration works
* [ ] Login works
* [ ] Logout works
* [ ] Roles work
* [ ] Unauthorized users are blocked
* [ ] Shared layout works
* [ ] Git branching strategy established


### ✅ Week 1 Deliverable


> A user can register, log in, receive a role, and access the appropriate role-protected pages.


---


# WEEK 2 — Student Profiles & Opportunity Creation


## 🎯 Goal


Start building the actual CampusConnect functionality.


---


# 👤 Person 1 — Student Profile & Skills


### Implement


* [ ] Student profile creation
* [ ] Profile editing
* [ ] Department selection
* [ ] Batch/year
* [ ] Bio
* [ ] GitHub URL
* [ ] LinkedIn URL
* [ ] Resume URL/upload mechanism
* [ ] Skills selection
* [ ] Technical skills
* [ ] Soft skills
* [ ] Tool proficiencies
* [ ] Beginner/Intermediate/Advanced proficiency


### Profile completion


* [ ] Define completion calculation
* [ ] Calculate percentage
* [ ] Display completion percentage
* [ ] Identify missing fields
* [ ] Prompt student to complete profile


### Testing


* [ ] Student can edit own profile
* [ ] Student cannot edit another student's profile
* [ ] Skills save correctly
* [ ] Proficiency saves correctly


---


# 👤 Person 2 — Opportunity Creation


### Implement


* [ ] Opportunity model
* [ ] Create opportunity form
* [ ] Category
* [ ] Description
* [ ] Department eligibility
* [ ] Work mode
* [ ] Stipend/salary
* [ ] Registration deadline
* [ ] Event date
* [ ] Capacity
* [ ] Organizer association
* [ ] Approval status


### Approval workflow


```text
Pending Review
      ↓
Admin/HOD
   ↙     ↘
Approve  Reject
   ↓
Published
```


Implement:


* [ ] Pending opportunity list
* [ ] Admin/HOD approval
* [ ] Admin/HOD rejection
* [ ] Rejection reason if required
* [ ] Approved opportunities become visible


---


# 👤 Person 3 — Shared UI & Student Dashboard Foundation


### Implement


* [ ] Student dashboard layout
* [ ] Dashboard cards
* [ ] Profile completion widget
* [ ] Upcoming opportunities section
* [ ] Upcoming deadlines section
* [ ] Saved opportunities section placeholder
* [ ] Application section placeholder
* [ ] Notification section placeholder


### Reusable components


* [ ] Opportunity card
* [ ] Status badge
* [ ] Modal
* [ ] Search box
* [ ] Filter dropdown
* [ ] Pagination


---


## 🤝 Week 2 Integration


* [ ] Student profile connects to logged-in user
* [ ] Skills connect to student profile
* [ ] Organizer can create opportunity
* [ ] Admin/HOD can approve opportunity
* [ ] Approved opportunity appears in system
* [ ] Dashboard displays real database information


### ✅ Week 2 Deliverable


> Authentication + Profiles + Skills + Opportunity Creation + Approval are functional.


---


# WEEK 3 — Discovery, Applications & Matching


## 🎯 Goal


Build the core CampusConnect opportunity workflow.


This is the week where the project should become demonstrably usable.


---


# 👤 Person 1 — Matching & Skill Gap Engine


### Implement


* [ ] Define compatibility calculation
* [ ] Compare student skills with opportunity requirements
* [ ] Calculate match percentage
* [ ] Display match percentage
* [ ] Identify missing skills
* [ ] Display skill gap
* [ ] Connect matching engine to opportunity details
* [ ] Prepare recommendation query


Example:


```text
Student:
C#
SQL
HTML


Required:
C#
SQL
React


Match:
2 / 3 = 66.7%


Missing:
React
```


### Testing


* [ ] 100% matching case
* [ ] Partial matching case
* [ ] 0% matching case
* [ ] Student with no skills
* [ ] Opportunity with no required skills


---


# 👤 Person 2 — Discovery & Applications


## Opportunity Discovery


* [ ] Opportunity listing
* [ ] Search
* [ ] Category filtering
* [ ] Department filtering
* [ ] Stipend filtering
* [ ] Location/work-mode filtering
* [ ] Deadline filtering
* [ ] Skill filtering
* [ ] Sorting
* [ ] Pagination


## Opportunity Details


* [ ] Full opportunity information
* [ ] Organizer
* [ ] Deadline
* [ ] Capacity
* [ ] Required skills
* [ ] Match percentage
* [ ] Missing skills
* [ ] Apply button
* [ ] Save/bookmark button


## Applications


* [ ] Create application
* [ ] Prevent duplicate applications
* [ ] Store applied timestamp
* [ ] Application status
* [ ] Student application history


### Application workflow


```text
Applied
   ↓
Under Review
   ↓
Shortlisted
   ↓
Selected / Rejected
```


---


# 👤 Person 3 — Organizer Dashboard


### Implement


* [ ] Organizer dashboard
* [ ] Opportunity list
* [ ] Applicant list
* [ ] Applicant profile view
* [ ] Applicant skill information
* [ ] Filter applicants
* [ ] View application status
* [ ] Update application status
* [ ] Add organizer remarks
* [ ] Bulk status update
* [ ] CSV applicant export


---


## 🤝 Week 3 Integration Test


Run this complete scenario:


* [ ] Student registers
* [ ] Student creates profile
* [ ] Student adds skills
* [ ] Organizer creates opportunity
* [ ] Admin approves opportunity
* [ ] Student searches opportunity
* [ ] Student sees match %
* [ ] Student sees missing skills
* [ ] Student saves opportunity
* [ ] Student applies
* [ ] Organizer sees applicant
* [ ] Organizer changes status
* [ ] Student sees updated status


### 🚨 Major Milestone


At the end of Week 3 you should have your **first complete end-to-end CampusConnect workflow**.


---


# WEEK 4 — Grievances, Networking & Faculty Interaction


## 🎯 Goal


Complete the major campus-service modules.


---


# 👤 Person 1 — Grievance Module


### Submission


* [ ] Grievance creation form
* [ ] Category
* [ ] Description
* [ ] Priority
* [ ] Anonymous option
* [ ] Department selection/routing
* [ ] Ticket creation
* [ ] SLA due time calculation


### Categories


* [ ] Infrastructure
* [ ] Academic/Evaluation
* [ ] Hostel/Mess
* [ ] Harassment/Ragging
* [ ] Canteen
* [ ] Administration


### Workflow


```text
Submitted
    ↓
Acknowledged
    ↓
In Progress
    ↓
Resolved
```


Alternative:


```text
In Progress
    ↓
SLA exceeded
    ↓
Escalated
```


### Audit trail


* [ ] Create `grievance_logs`
* [ ] Record status changes
* [ ] Record user who made change
* [ ] Record resolution note
* [ ] Record timestamp


### Tracker


* [ ] Student can track own grievance
* [ ] Officer can view assigned grievances
* [ ] Officer can update status
* [ ] Officer can add resolution notes
* [ ] HOD/Admin can view escalated grievances


---


# 👤 Person 2 — Directory & Networking


## Directory


* [ ] Student directory
* [ ] Faculty directory
* [ ] Department filtering
* [ ] Stream filtering
* [ ] Batch filtering
* [ ] Domain/interest filtering
* [ ] Search


## Connections


* [ ] Send connection request
* [ ] Accept request
* [ ] Decline request
* [ ] View connections
* [ ] Prevent duplicate requests


## Messaging


* [ ] Create message
* [ ] View conversation
* [ ] Read/unread status
* [ ] Basic message validation


---


# 👤 Person 3 — Faculty Interaction


## Office Hours


* [ ] Faculty creates slots
* [ ] Faculty edits slots
* [ ] Faculty deletes slots
* [ ] Student sees available slots
* [ ] Student books slot
* [ ] Prevent double booking
* [ ] Show booked/unavailable state


## Announcements


* [ ] Faculty creates announcement
* [ ] HOD creates announcement
* [ ] Department-specific announcement
* [ ] College-wide announcement
* [ ] Student views announcements
* [ ] Read tracking


---


## 🤝 Week 4 Integration


* [ ] Grievance submission works
* [ ] Anonymous grievance works
* [ ] Grievance routing works
* [ ] Status tracking works
* [ ] Audit log works
* [ ] Directory search works
* [ ] Connection requests work
* [ ] Messaging works
* [ ] Faculty slots work
* [ ] Booking works
* [ ] Announcements work


### ✅ Week 4 Deliverable


> Core campus-service functionality is operational.


---


# WEEK 5 — Dashboards, Calendar & Notifications


## 🎯 Goal


Connect all modules into a unified CampusConnect experience.


---


# 👤 Person 1 — Student Dashboard & Recommendations


### Dashboard


* [ ] Upcoming deadlines
* [ ] Active applications
* [ ] Saved opportunities
* [ ] Recommended opportunities
* [ ] Grievance status
* [ ] Upcoming events
* [ ] Profile completion
* [ ] Skill gaps


### Recommendations


* [ ] Query opportunities based on profile
* [ ] Rank by compatibility percentage
* [ ] Show recommended opportunities
* [ ] Display missing skills
* [ ] Display relevant learning/workshop suggestions where available


---


# 👤 Person 2 — Unified Calendar


Use FullCalendar.js as specified in your technology stack.


### Implement


* [ ] Calendar page
* [ ] Academic events
* [ ] Opportunity deadlines
* [ ] Campus events
* [ ] Faculty office hours
* [ ] Event details
* [ ] Date filtering
* [ ] Category filtering
* [ ] Department/stream filtering


### Calendar categories


* [ ] Academic
* [ ] Opportunity
* [ ] Event
* [ ] Office Hours
* [ ] Announcement


---


# 👤 Person 3 — Notifications & Email


## In-app notifications


* [ ] Notification entity
* [ ] Create notification service
* [ ] Notification bell
* [ ] Unread count
* [ ] Mark as read
* [ ] Notification history


### Notification events


* [ ] Application status changed
* [ ] Grievance updated
* [ ] Opportunity deadline approaching
* [ ] New announcement
* [ ] Connection request
* [ ] New message


## Email


Implement selected email solution:


* [ ] Configure SMTP/provider
* [ ] Email service
* [ ] Verification email
* [ ] Application notification
* [ ] Grievance update notification
* [ ] Deadline notification


Your supplied architecture specifies MailKit/FluentEmail + SMTP or SendGrid as the email layer.


---


# 🤝 Week 5 Shared Dashboard Work


### Admin Dashboard


* [ ] User statistics
* [ ] Opportunity statistics
* [ ] Application statistics
* [ ] Grievance statistics
* [ ] SLA compliance
* [ ] Placement/application conversion statistics
* [ ] Basic charts


### HOD Dashboard


* [ ] Department opportunities
* [ ] Department grievances
* [ ] Department applications
* [ ] Department announcements


### Organizer/Faculty Dashboard


* [ ] Office-hour bookings
* [ ] Event/RSVP information
* [ ] Applicant metrics


---


# WEEK 6 — Testing, Security, Performance & Deployment


# 🚨 No major new features in Week 6


The purpose of Week 6 is to make the existing application stable.


---


# 👤 Person 1 — Security & Authorization


### Authentication


* [ ] Test password hashing
* [ ] Test login
* [ ] Test logout
* [ ] Test session timeout
* [ ] Verify secure HTTP-only cookies
* [ ] Test password recovery


### RBAC


Test every role:


* [ ] Student
* [ ] Faculty
* [ ] Club Coordinator
* [ ] Event Organizer
* [ ] HOD
* [ ] Admin


### Authorization scenarios


* [ ] Student cannot access admin pages
* [ ] Student cannot approve opportunities
* [ ] Organizer cannot access unrelated admin functionality
* [ ] Faculty cannot modify another faculty member's data
* [ ] HOD access is restricted appropriately
* [ ] Anonymous grievance data is protected


Your NFR specifically requires Identity-based password protection, parameterized EF Core queries, secure sessions and appropriate access control.


---


# 👤 Person 2 — Database & Performance


### Database


* [ ] Review all relationships
* [ ] Review foreign keys
* [ ] Add required indexes
* [ ] Check duplicate records
* [ ] Check orphaned records
* [ ] Review cascade behavior
* [ ] Review migration history


### Performance


Test:


* [ ] Opportunity search
* [ ] Opportunity filtering
* [ ] Directory filtering
* [ ] Student dashboard
* [ ] Organizer dashboard
* [ ] Grievance tracker


### Optimize


* [ ] Use pagination
* [ ] Use async queries
* [ ] Avoid unnecessary `Include()`
* [ ] Avoid N+1 queries
* [ ] Select only required columns where appropriate
* [ ] Check generated SQL
* [ ] Verify core queries meet the 2-second requirement under normal institutional loads


The 2-second response target is explicitly part of your NFRs.


---


# 👤 Person 3 — Testing & Deployment


### Functional testing


* [ ] Registration
* [ ] Login
* [ ] RBAC
* [ ] Profile
* [ ] Skills
* [ ] Opportunities
* [ ] Applications
* [ ] Matching
* [ ] Grievances
* [ ] Directory
* [ ] Connections
* [ ] Messaging
* [ ] Office hours
* [ ] Announcements
* [ ] Notifications
* [ ] Calendar
* [ ] Dashboards


### UI testing


* [ ] Desktop
* [ ] Tablet
* [ ] Mobile
* [ ] Forms
* [ ] Tables
* [ ] Modals
* [ ] Navigation
* [ ] Error messages


### Deployment


* [ ] Production configuration
* [ ] Production connection string
* [ ] Environment variables/secrets
* [ ] Database migration
* [ ] Azure SQL setup
* [ ] Azure App Service setup
* [ ] Deploy application
* [ ] Verify production login
* [ ] Verify production database
* [ ] Test major workflows


Your architecture proposes Azure App Service + Azure SQL Database for production deployment.


---


# 6. Team Git Workflow


Use this from **Day 1**.


## Branch structure


```text
main
│
└── develop
     │
     ├── feature/identity
     ├── feature/profile
     ├── feature/opportunities
     ├── feature/applications
     ├── feature/grievances
     ├── feature/directory
     ├── feature/office-hours
     └── feature/notifications
```


## For every feature


* [ ] Create feature branch
* [ ] Implement feature
* [ ] Test locally
* [ ] Commit changes
* [ ] Push branch
* [ ] Create Pull Request
* [ ] Get teammate review
* [ ] Fix review comments
* [ ] Merge into `develop`
* [ ] Test integration


### Important


**Nobody directly pushes feature code to `main`.**


---


# 7. Team Rules


These rules will keep the project manageable.


### Rule 1 — Everyone writes .NET


Each person must independently implement:


* [ ] Controller
* [ ] Service
* [ ] EF Core queries
* [ ] ViewModel
* [ ] Razor View
* [ ] Validation
* [ ] Authorization


---


### Rule 2 — Don't create giant controllers


Avoid:


```text
OpportunityController
    ├── 2000 lines
    ├── business logic
    ├── SQL queries
    └── email code
```


Prefer:


```text
OpportunityController
       ↓
OpportunityService
       ↓
EF Core
```


---


### Rule 3 — Database changes must be communicated


Before modifying a shared entity:


* [ ] Tell the team
* [ ] Discuss relationship changes
* [ ] Create migration
* [ ] Test migration
* [ ] Inform everyone before pulling


---


### Rule 4 — Code review


Every feature requires:


* [ ] Another teammate reviews PR
* [ ] Reviewer understands the implementation
* [ ] Reviewer tests the feature
* [ ] Comments resolved before merge


Rotate reviewers:


```text
Week 1:
P1 → P2
P2 → P3
P3 → P1


Week 2:
P1 → P3
P2 → P1
P3 → P2
```


This preserves module ownership while preventing knowledge silos.


---


# 8. Definition of Done


A feature isn't considered complete just because the page works.


Every feature must satisfy:


* [ ] Database entity completed
* [ ] EF Core relationship configured
* [ ] Migration applied
* [ ] Service implemented
* [ ] Controller implemented
* [ ] ViewModel implemented
* [ ] Razor View implemented
* [ ] Validation implemented
* [ ] Authorization implemented
* [ ] Error handling implemented
* [ ] Success/failure messages implemented
* [ ] Tested with valid data
* [ ] Tested with invalid data
* [ ] Tested with unauthorized user
* [ ] Code reviewed
* [ ] Merged into `develop`


---


# 9. Six-Week Milestone Tracker


## 🟢 Milestone 1 — End of Week 1


* [ ] ASP.NET Core project running
* [ ] SQL Server connected
* [ ] EF Core configured
* [ ] Identity configured
* [ ] Roles created
* [ ] Login/Register working
* [ ] Shared UI working


---


## 🟢 Milestone 2 — End of Week 2


* [ ] Student profiles working
* [ ] Skills working
* [ ] Profile completion working
* [ ] Opportunity creation working
* [ ] Admin/HOD approval working
* [ ] Student dashboard foundation working


---


## 🟢 Milestone 3 — End of Week 3


* [ ] Opportunity discovery working
* [ ] Search/filter working
* [ ] Match percentage working
* [ ] Skill gaps working
* [ ] Bookmarking working
* [ ] Applications working
* [ ] Organizer dashboard working


### ⭐ Core MVP should be demonstrable here.


---


## 🟢 Milestone 4 — End of Week 4


* [ ] Grievance system working
* [ ] SLA/escalation working
* [ ] Audit logs working
* [ ] Directory working
* [ ] Connections working
* [ ] Messaging working
* [ ] Office hours working
* [ ] Announcements working


---


## 🟢 Milestone 5 — End of Week 5


* [ ] Student dashboard complete
* [ ] Admin dashboard complete
* [ ] HOD dashboard complete
* [ ] Calendar working
* [ ] Notifications working
* [ ] Email working
* [ ] Recommendations working


---


## 🔴 Milestone 6 — End of Week 6


* [ ] Security testing complete
* [ ] RBAC testing complete
* [ ] Database optimized
* [ ] Performance tested
* [ ] Functional testing complete
* [ ] UI testing complete
* [ ] Bugs fixed
* [ ] Production database configured
* [ ] Azure deployment complete
* [ ] Final end-to-end test complete
* [ ] Project documentation complete
* [ ] Final demo ready


---


# 10. Final Team Responsibility Map


```text
                    CAMPUSCONNECT
                         │
        ┌────────────────┼────────────────┐
        │                │                │
        ▼                ▼                ▼
      P1                 P2               P3
        │                │                │
   IDENTITY         OPPORTUNITIES     CAMPUS SERVICES
   PROFILE          APPLICATIONS      GRIEVANCES
   SKILLS           DISCOVERY         DIRECTORY
   MATCHING         ORGANIZER         CONNECTIONS
   STUDENT          CALENDAR          MESSAGING
   DASHBOARD                         OFFICE HOURS
                                     ANNOUNCEMENTS
        │                │                │
        └────────────────┼────────────────┘
                         │
                         ▼
                 SHARED FR-10
                         │
             ┌───────────┼───────────┐
             ▼           ▼           ▼
         Dashboard   Notifications  Analytics
             │           │           │
             └───────────┼───────────┘
                         ▼
                     EF CORE
                         │
                         ▼
                     SQL SERVER
                         │
                         ▼
                AZURE APP SERVICE
```


## 🎯 The most important team principle


**Own your module completely, but don't become the only person who understands it.**


Each of you should be able to say:


> "I can build an ASP.NET Core feature from database to Razor UI."


By the end of six weeks, that should be true for **all three members**.



