import os
import re

# 1. ApplicationsController
f = "Controllers/ApplicationsController.cs"
with open(f, "r") as file: content = file.read()
content = content.replace('await _applicationRepository.DeleteAsync(id);', '''
        var user = await _userManager.GetUserAsync(User);
        var studentProfile = user != null ? await _studentProfileRepository.GetByUserIdAsync(user.Id) : null;
        if (studentProfile == null) return Forbid();
        var application = await _applicationRepository.GetByIdAsync(id);
        if (application == null) return NotFound();
        if (application.StudentId != studentProfile.ProfileId) return Forbid();
        await _applicationRepository.DeleteAsync(id);
''')
with open(f, "w") as file: file.write(content)

# 2. OrganizerController
f = "Controllers/OrganizerController.cs"
with open(f, "r") as file: content = file.read()
content = content.replace('var applications = await _applicationRepository.GetByOpportunityIdAsync(id);', '''
        var opportunity = await _opportunityRepository.GetByIdAsync(id);
        var user = await _userManager.GetUserAsync(User);
        if (opportunity?.OrganizerId != user?.Id && !User.IsInRole("Admin")) return Forbid();
        var applications = await _applicationRepository.GetByOpportunityIdAsync(id);
''')
content = content.replace('await _applicationRepository.UpdateStatusAsync(applicationId, status, remarks);', '''
        var opportunity = await _opportunityRepository.GetByIdAsync(application.OpportunityId);
        var user = await _userManager.GetUserAsync(User);
        if (opportunity?.OrganizerId != user?.Id && !User.IsInRole("Admin")) return Forbid();
        await _applicationRepository.UpdateStatusAsync(applicationId, status, remarks);
''')
with open(f, "w") as file: file.write(content)

# 3. FacultyProfilesController
f = "Controllers/FacultyProfilesController.cs"
with open(f, "r") as file: content = file.read()
content = content.replace('if (profile == null || profile.FacultyProfileId != model.FacultyProfileId) return NotFound();', '''if (profile == null || profile.FacultyProfileId != model.FacultyProfileId) return Forbid();''')
with open(f, "w") as file: file.write(content)

# 4. StudentProfilesController
f = "Controllers/StudentProfilesController.cs"
with open(f, "r") as file: content = file.read()
content = content.replace('if (file.Length > 0)', '''if (file.Length > 0)
        {
            if (file.Length > 5 * 1024 * 1024) throw new InvalidOperationException("File size cannot exceed 5MB.");
            var ext = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".pdf" && ext != ".docx") throw new InvalidOperationException("Only PDF and DOCX files are allowed.");
''')
with open(f, "w") as file: file.write(content)

# 5. OpportunitiesController
f = "Controllers/OpportunitiesController.cs"
with open(f, "r") as file: content = file.read()
content = content.replace('await _opportunityRepository.UpdateStatusAsync(id, ApprovalStatus.Approved);', '''
        var opp = await _opportunityRepository.GetByIdAsync(id);
        if (User.IsInRole("Hod") && !User.IsInRole("Admin")) {
            var user = await _userManager.GetUserAsync(User);
            var facRepo = HttpContext.RequestServices.GetService<CampusConnect.Repositories.IFacultyProfileRepository>();
            var facProfile = facRepo != null ? await facRepo.GetByUserIdAsync(user.Id) : null;
            if (facProfile == null || (opp?.TargetDepartmentId != null && opp.TargetDepartmentId != facProfile.DepartmentId)) return Forbid();
        }
        await _opportunityRepository.UpdateStatusAsync(id, ApprovalStatus.Approved);
''')
content = content.replace('await _opportunityRepository.UpdateStatusAsync(id, ApprovalStatus.Rejected);', '''
        var opp = await _opportunityRepository.GetByIdAsync(id);
        if (User.IsInRole("Hod") && !User.IsInRole("Admin")) {
            var user = await _userManager.GetUserAsync(User);
            var facRepo = HttpContext.RequestServices.GetService<CampusConnect.Repositories.IFacultyProfileRepository>();
            var facProfile = facRepo != null ? await facRepo.GetByUserIdAsync(user.Id) : null;
            if (facProfile == null || (opp?.TargetDepartmentId != null && opp.TargetDepartmentId != facProfile.DepartmentId)) return Forbid();
        }
        await _opportunityRepository.UpdateStatusAsync(id, ApprovalStatus.Rejected);
''')
with open(f, "w") as file: file.write(content)

# 6. DepartmentsController and SkillsController
for f in ["Controllers/DepartmentsController.cs", "Controllers/SkillsController.cs"]:
    with open(f, "r") as file: content = file.read()
    content = content.replace('[Authorize(Roles = "Hod,Admin")]', '[Authorize]')
    content = content.replace('public IActionResult Create() => View();', '[Authorize(Roles = "Admin")]\n    public IActionResult Create() => View();')
    content = content.replace('public async Task<IActionResult> Create(', '[Authorize(Roles = "Admin")]\n    public async Task<IActionResult> Create(')
    content = content.replace('public async Task<IActionResult> Edit(int id)', '[Authorize(Roles = "Admin")]\n    public async Task<IActionResult> Edit(int id)')
    content = content.replace('public async Task<IActionResult> Edit(', '[Authorize(Roles = "Admin")]\n    public async Task<IActionResult> Edit(')
    content = content.replace('public async Task<IActionResult> Delete(int id)', '[Authorize(Roles = "Admin")]\n    public async Task<IActionResult> Delete(int id)')
    content = content.replace('public async Task<IActionResult> DeleteConfirmed(', '[Authorize(Roles = "Admin")]\n    public async Task<IActionResult> DeleteConfirmed(')
    with open(f, "w") as file: file.write(content)

# 7. StudentProfileViewModel
f = "Models/ViewModels/StudentProfileViewModel.cs"
with open(f, "r") as file: content = file.read()
content = content.replace('public int BatchYear { get; set; }', '[Range(2020, 2030)]\n    public int? BatchYear { get; set; }')
content = content.replace('public string? GitHubUrl { get; set; }', '[RegularExpression(@"^https:\\/\\/(www\\.)?github\\.com\\/.*")]\n    public string? GitHubUrl { get; set; }')
content = content.replace('public string? LinkedInUrl { get; set; }', '[RegularExpression(@"^https:\\/\\/(www\\.)?linkedin\\.com\\/.*")]\n    public string? LinkedInUrl { get; set; }')
with open(f, "w") as file: file.write(content)

# 8. Program.cs
f = "Program.cs"
with open(f, "r") as file: content = file.read()
content = content.replace('builder.Services.AddControllersWithViews();', 'builder.Services.AddControllersWithViews(options => options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));')
with open(f, "w") as file: file.write(content)

print("Patched part A!")
