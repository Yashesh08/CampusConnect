f = "Controllers/OrganizerController.cs"
with open(f, "r") as file: content = file.read()
content = content.replace('var opportunity = await _opportunityRepository.GetByIdAsync(id);\n        var user = await _userManager.GetUserAsync(User);\n        if (opportunity?.OrganizerId != user?.Id && !User.IsInRole("Admin")) return Forbid();', '''var user = await _userManager.GetUserAsync(User);
        if (opportunity?.OrganizerId != user?.Id && !User.IsInRole("Admin")) return Forbid();''')
with open(f, "w") as file: file.write(content)

f = "Controllers/StudentProfilesController.cs"
with open(f, "r") as file: content = file.read()
content = content.replace('BatchYear = model.BatchYear,', 'BatchYear = model.BatchYear ?? 0,')
content = content.replace('profile.BatchYear = model.BatchYear;', 'profile.BatchYear = model.BatchYear ?? 0;')
with open(f, "w") as file: file.write(content)
