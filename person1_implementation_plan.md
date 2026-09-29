# Person 1 - Implementation Plan (Independent Module Ownership)

**Focus:** Identity, Profiles, & Recommendations
**Owned Requirements:** FR-1 (Auth/RBAC), FR-2 (Student/Faculty Profile & Skills), FR-5 (Matching/Skill Gap)

## Core Philosophy
This module is built entirely independently. It relies on its own database entities, controllers, services, and views. There is no blocking dependency on other team members until the final Week 6 integration. Any external data needed for testing (like opportunities for the matching engine) will be mocked via local seed data.

---

## Entity Ownership
- `users` (Identity foundation)
- `departments`
- `student_profiles`
- `faculty_profiles`
- `skills`
- `student_skills`

## Main Pages
- `/Account/Login`
- `/Account/Register`
- `/Profile/Edit`
- `/Dashboard/Student` (Standalone version)
- Matching & Recommendation Components

---

## Weekly Breakdown

### Week 1: Identity & Auth (Foundation)
**Goal:** Deliver a stable `users` table and role scaffold that other modules can reference.
- [ ] Configure ASP.NET Core Identity.
- [ ] Create `User` model and implement college email validation.
- [ ] Implement Registration, Login, and Logout flows.
- [ ] Implement Role assignment (Student, Faculty, Club Coordinator, Event Organizer, HOD, Admin).
- [ ] Set up Role-based authorization.
- [ ] **Deliverable:** Working registration/login/roles.

### Week 2: Student Profile & Skills
**Goal:** Allow users to build and manage their profiles.
- [ ] Build Profile creation and editing (department, batch, bio, links, resume).
- [ ] Implement Skills selection interface with proficiency levels.
- [ ] Develop Profile completion % calculation logic.
- [ ] Test own-profile-only editing and skill save/load.
- [ ] **Deliverable:** Demoable profile and skills module with seed data.

### Week 3: Matching & Recommendation Engine
**Goal:** Build the logic to match student skills against opportunities.
- [x] Develop Compatibility calculation (student skills vs required skills).
- [x] Display Match % and missing-skills.
- [x] **Crucial:** Test against locally seeded dummy opportunities (Do NOT wait for Person 2's live data).
- [x] Design the read-only interface that Person 2 will plug real opportunity data into later in Week 6.
- [x] **Deliverable:** Demoable matching engine working with seeded opportunities.

### Week 4: Faculty Profiles & Dashboard Foundation
**Goal:** Expand profiles to faculty and build the standalone dashboard shell.
- [x] Implement Faculty profile management.
- [x] Build Student dashboard shell (using own module data only: profile completion, skill gaps).
- [x] **Deliverable:** Feature-complete module.

### Week 5: Polish & Refinement
**Goal:** Finalize the module before integration.
- [x] Refine recommendation ranking (factored in proficiency levels).
- [x] Ensure own dashboard widgets are fully working against own data.
- [x] **Deliverable:** Module is fully complete and independently testable.

### Week 6: Integration, Testing, Security & Deployment
**Goal:** Connect with the rest of the application using read-only interfaces.
- [x] Wire real opportunity data (from Person 2) into the matching engine, replacing seeded fixtures.
- [x] Provide data to the shared unified calendar/dashboard/notification bell as needed.
- [x] Test authorization on owned pages.
- [x] Optimize queries (pagination, async, avoid N+1) aiming for a 2-second response target.
- [x] Deploy and run migrations together.
