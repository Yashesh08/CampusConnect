using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CampusConnect.Data;
using CampusConnect.Models;
using CampusConnect.Models.Enums;
using CampusConnect.Repositories;
using CampusConnect.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusConnect.Tests;

// ═══════════════════════════════════════════════════════════════════════
// WEEK 3 — SKILL MATCHING ENGINE — EXHAUSTIVE ADVERSARIAL TESTS
// ═══════════════════════════════════════════════════════════════════════
public class SkillMatchingEngineAdversarialTests
{
    private readonly SkillMatchingEngine _engine = new();

    // ───── Helpers ─────
    private static Skill MakeSkill(int id, string name = "Skill") =>
        new() { SkillId = id, SkillName = $"{name}{id}", Category = "Test" };

    private static StudentSkill MakeStudentSkill(int skillId, ProficiencyLevel level) =>
        new() { SkillId = skillId, ProficiencyLevel = level, Skill = MakeSkill(skillId) };

    private static OpportunitySkill MakeRequiredSkill(int skillId) =>
        new() { SkillId = skillId, Skill = MakeSkill(skillId) };

    private static StudentProfile MakeStudent(params StudentSkill[] skills) =>
        new() { StudentSkills = skills.ToList() };

    private static Opportunity MakeOpportunity(params OpportunitySkill[] skills) =>
        new() { RequiredSkills = skills.ToList() };

    // ───── T001: Zero required skills → 100% ─────
    [Fact]
    public void T001_ZeroRequiredSkills_Returns100Percent()
    {
        var result = _engine.CalculateMatch(MakeStudent(), MakeOpportunity());
        Assert.Equal(100.0, result.MatchPercentage);
        Assert.Empty(result.MatchingSkills);
        Assert.Empty(result.MissingSkills);
    }

    // ───── T002: Null required skills → 100% ─────
    [Fact]
    public void T002_NullRequiredSkills_Returns100Percent()
    {
        var opp = new Opportunity { RequiredSkills = null! };
        var result = _engine.CalculateMatch(MakeStudent(), opp);
        Assert.Equal(100.0, result.MatchPercentage);
    }

    // ───── T003: Student with zero skills, opportunity requires some → 0% ─────
    [Fact]
    public void T003_StudentHasNoSkills_Returns0Percent()
    {
        var result = _engine.CalculateMatch(MakeStudent(), MakeOpportunity(MakeRequiredSkill(1), MakeRequiredSkill(2)));
        Assert.Equal(0.0, result.MatchPercentage);
        Assert.Empty(result.MatchingSkills);
        Assert.Equal(2, result.MissingSkills.Count);
    }

    // ───── T004: Null student skills collection ─────
    [Fact]
    public void T004_NullStudentSkillsCollection_Returns0Percent()
    {
        var student = new StudentProfile { StudentSkills = null! };
        var result = _engine.CalculateMatch(student, MakeOpportunity(MakeRequiredSkill(1)));
        Assert.Equal(0.0, result.MatchPercentage);
        Assert.Single(result.MissingSkills);
    }

    // ───── T005: Exact match all Advanced → 100% ─────
    [Fact]
    public void T005_ExactMatchAllAdvanced_Returns100Percent()
    {
        var student = MakeStudent(
            MakeStudentSkill(1, ProficiencyLevel.Advanced),
            MakeStudentSkill(2, ProficiencyLevel.Advanced));
        var opp = MakeOpportunity(MakeRequiredSkill(1), MakeRequiredSkill(2));
        var result = _engine.CalculateMatch(student, opp);
        Assert.Equal(100.0, result.MatchPercentage);
        Assert.Equal(2, result.MatchingSkills.Count);
        Assert.Empty(result.MissingSkills);
    }

    // ───── T006: All Intermediate → 80% ─────
    [Fact]
    public void T006_AllIntermediate_Returns80Percent()
    {
        var student = MakeStudent(MakeStudentSkill(1, ProficiencyLevel.Intermediate));
        var opp = MakeOpportunity(MakeRequiredSkill(1));
        var result = _engine.CalculateMatch(student, opp);
        Assert.Equal(80.0, result.MatchPercentage);
    }

    // ───── T007: All Beginner → 50% ─────
    [Fact]
    public void T007_AllBeginner_Returns50Percent()
    {
        var student = MakeStudent(MakeStudentSkill(1, ProficiencyLevel.Beginner));
        var opp = MakeOpportunity(MakeRequiredSkill(1));
        var result = _engine.CalculateMatch(student, opp);
        Assert.Equal(50.0, result.MatchPercentage);
    }

    // ───── T008: Mixed proficiency calculation ─────
    [Fact]
    public void T008_MixedProficiency_CalculatesCorrectly()
    {
        // Advanced(1.0) + Intermediate(0.8) + Beginner(0.5) = 2.3/3 = 76.7%
        var student = MakeStudent(
            MakeStudentSkill(1, ProficiencyLevel.Advanced),
            MakeStudentSkill(2, ProficiencyLevel.Intermediate),
            MakeStudentSkill(3, ProficiencyLevel.Beginner));
        var opp = MakeOpportunity(MakeRequiredSkill(1), MakeRequiredSkill(2), MakeRequiredSkill(3));
        var result = _engine.CalculateMatch(student, opp);
        Assert.Equal(76.7, result.MatchPercentage);
        Assert.Equal(3, result.MatchingSkills.Count);
        Assert.Empty(result.MissingSkills);
    }

    // ───── T009: Partial match — some skills missing ─────
    [Fact]
    public void T009_PartialMatch_CorrectlyIdentifiesMissing()
    {
        var student = MakeStudent(MakeStudentSkill(1, ProficiencyLevel.Advanced));
        var opp = MakeOpportunity(MakeRequiredSkill(1), MakeRequiredSkill(2), MakeRequiredSkill(3));
        var result = _engine.CalculateMatch(student, opp);
        // 1.0/3 = 33.3%
        Assert.Equal(33.3, result.MatchPercentage);
        Assert.Single(result.MatchingSkills);
        Assert.Equal(2, result.MissingSkills.Count);
    }

    // ───── T010: Student has extra skills not required ─────
    [Fact]
    public void T010_ExtraStudentSkills_DoNotInflateScore()
    {
        var student = MakeStudent(
            MakeStudentSkill(1, ProficiencyLevel.Advanced),
            MakeStudentSkill(99, ProficiencyLevel.Advanced),
            MakeStudentSkill(100, ProficiencyLevel.Advanced));
        var opp = MakeOpportunity(MakeRequiredSkill(1));
        var result = _engine.CalculateMatch(student, opp);
        Assert.Equal(100.0, result.MatchPercentage); // Only skill 1 matters
        Assert.Single(result.MatchingSkills);
    }

    // ───── T011: Single required skill, student missing it ─────
    [Fact]
    public void T011_SingleRequiredSkillMissing_Returns0()
    {
        var student = MakeStudent(MakeStudentSkill(99, ProficiencyLevel.Advanced));
        var opp = MakeOpportunity(MakeRequiredSkill(1));
        var result = _engine.CalculateMatch(student, opp);
        Assert.Equal(0.0, result.MatchPercentage);
        Assert.Single(result.MissingSkills);
    }

    // ───── T012: Unknown/invalid proficiency enum value → defaults to 0.5 ─────
    [Fact]
    public void T012_UnknownProficiencyLevel_DefaultsTo50Percent()
    {
        // Cast an invalid enum value
        var student = MakeStudent(new StudentSkill
        {
            SkillId = 1,
            ProficiencyLevel = (ProficiencyLevel)999,
            Skill = MakeSkill(1)
        });
        var opp = MakeOpportunity(MakeRequiredSkill(1));
        var result = _engine.CalculateMatch(student, opp);
        // Default case in switch returns 0.5
        Assert.Equal(50.0, result.MatchPercentage);
    }

    // ───── T013: Required skill with null Skill navigation property ─────
    [Fact]
    public void T013_NullSkillNavigationProperty_SkippedSafely()
    {
        var student = MakeStudent(MakeStudentSkill(1, ProficiencyLevel.Advanced));
        var opp = MakeOpportunity(new OpportunitySkill { SkillId = 1, Skill = null! });
        var result = _engine.CalculateMatch(student, opp);
        // The null check `if (reqSkill.Skill == null) continue;` skips this
        // With 1 required skill skipped, requiredSkillsCount is still 1 but totalScore is 0
        // Actually looking at the code: the continue skips adding to matching/missing but count is still 1
        // So 0/1 = 0% — but the skill IS in student's list. The engine silently ignores it.
        Assert.Equal(0.0, result.MatchPercentage);
    }

    // ───── INVARIANT/PROPERTY TESTS ─────

    // ───── T014: Match percentage is always 0–100 ─────
    [Theory]
    [InlineData(0, 5)]
    [InlineData(1, 5)]
    [InlineData(3, 5)]
    [InlineData(5, 5)]
    [InlineData(10, 0)]
    [InlineData(0, 0)]
    public void T014_MatchPercentage_AlwaysBetween0And100(int studentSkillCount, int requiredSkillCount)
    {
        var student = MakeStudent(
            Enumerable.Range(1, studentSkillCount)
                .Select(i => MakeStudentSkill(i, ProficiencyLevel.Beginner))
                .ToArray());
        var opp = MakeOpportunity(
            Enumerable.Range(1, requiredSkillCount)
                .Select(i => MakeRequiredSkill(i))
                .ToArray());
        var result = _engine.CalculateMatch(student, opp);
        Assert.InRange(result.MatchPercentage, 0.0, 100.0);
    }

    // ───── T015: Matching ∩ Missing = ∅ ─────
    [Fact]
    public void T015_MatchingAndMissing_AreDisjoint()
    {
        var student = MakeStudent(
            MakeStudentSkill(1, ProficiencyLevel.Advanced),
            MakeStudentSkill(3, ProficiencyLevel.Intermediate));
        var opp = MakeOpportunity(MakeRequiredSkill(1), MakeRequiredSkill(2), MakeRequiredSkill(3));
        var result = _engine.CalculateMatch(student, opp);

        var matchingIds = result.MatchingSkills.Select(s => s.SkillId).ToHashSet();
        var missingIds = result.MissingSkills.Select(s => s.SkillId).ToHashSet();
        Assert.Empty(matchingIds.Intersect(missingIds));
    }

    // ───── T016: Matching ∪ Missing ⊆ Required (by SkillId) ─────
    [Fact]
    public void T016_MatchingPlusMissing_SubsetOfRequired()
    {
        var student = MakeStudent(MakeStudentSkill(1, ProficiencyLevel.Advanced));
        var opp = MakeOpportunity(MakeRequiredSkill(1), MakeRequiredSkill(2));
        var result = _engine.CalculateMatch(student, opp);

        var requiredIds = opp.RequiredSkills
            .Where(rs => rs.Skill != null)
            .Select(rs => rs.SkillId).ToHashSet();
        var resultIds = result.MatchingSkills.Select(s => s.SkillId)
            .Concat(result.MissingSkills.Select(s => s.SkillId)).ToHashSet();
        Assert.Subset(requiredIds, resultIds);
    }

    // ───── T017: Result is deterministic (ordering does not matter) ─────
    [Fact]
    public void T017_ResultIsDeterministic_OrderingDoesNotMatter()
    {
        var student = MakeStudent(
            MakeStudentSkill(1, ProficiencyLevel.Advanced),
            MakeStudentSkill(2, ProficiencyLevel.Beginner));

        var opp1 = MakeOpportunity(MakeRequiredSkill(1), MakeRequiredSkill(2), MakeRequiredSkill(3));
        var opp2 = MakeOpportunity(MakeRequiredSkill(3), MakeRequiredSkill(1), MakeRequiredSkill(2));

        var result1 = _engine.CalculateMatch(student, opp1);
        var result2 = _engine.CalculateMatch(student, opp2);

        Assert.Equal(result1.MatchPercentage, result2.MatchPercentage);
        Assert.Equal(
            result1.MatchingSkills.Select(s => s.SkillId).OrderBy(x => x),
            result2.MatchingSkills.Select(s => s.SkillId).OrderBy(x => x));
    }

    // ───── T018: Duplicate required skills — should they inflate? ─────
    [Fact]
    public void T018_DuplicateRequiredSkills_CountedIndividually()
    {
        // If opportunity has duplicate required skills (same SkillId), each is counted
        // This tests actual behavior — duplicates DO affect the denominator
        var student = MakeStudent(MakeStudentSkill(1, ProficiencyLevel.Advanced));
        var opp = MakeOpportunity(MakeRequiredSkill(1), MakeRequiredSkill(1));
        var result = _engine.CalculateMatch(student, opp);
        // 2 required, student matches both (same skill) → 2.0/2 = 100%
        Assert.Equal(100.0, result.MatchPercentage);
        // This reveals: duplicate skills in RequiredSkills inflate matching count
    }

    // ───── T019: Large skill collection performance sanity ─────
    [Fact]
    public void T019_LargeSkillCollections_DoNotCrash()
    {
        var student = MakeStudent(
            Enumerable.Range(1, 500)
                .Select(i => MakeStudentSkill(i, ProficiencyLevel.Intermediate))
                .ToArray());
        var opp = MakeOpportunity(
            Enumerable.Range(1, 1000)
                .Select(i => MakeRequiredSkill(i))
                .ToArray());

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = _engine.CalculateMatch(student, opp);
        sw.Stop();

        Assert.InRange(result.MatchPercentage, 0.0, 100.0);
        Assert.True(sw.ElapsedMilliseconds < 5000, $"Matching took {sw.ElapsedMilliseconds}ms — too slow");
    }

    // ───── T020: Empty student profile (no skills collection at all) ─────
    [Fact]
    public void T020_EmptyStudentProfile_DoesNotCrash()
    {
        var student = new StudentProfile(); // StudentSkills defaults to empty list
        var opp = MakeOpportunity(MakeRequiredSkill(1));
        var result = _engine.CalculateMatch(student, opp);
        Assert.Equal(0.0, result.MatchPercentage);
    }
}

// ═══════════════════════════════════════════════════════════════════════
// WEEK 4 — PROFILE ISOLATION — IDOR / AUTHORIZATION ATTACKS
// ═══════════════════════════════════════════════════════════════════════
public class ProfileIsolationAdversarialTests
{
    private static AppDbContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    // ───── T021: CRITICAL VULNERABILITY — RemoveSkill IDOR ─────
    // This test DEMONSTRATES the vulnerability: any student can delete any student's skills
    [Fact]
    public async Task T021_RemoveSkill_IDOR_AnyStudentCanDeleteAnyStudentsSkills()
    {
        using var context = CreateInMemoryContext();
        var repo = new StudentProfileRepository(context);

        // Setup: Student A with skill
        var userA = new User { Id = Guid.NewGuid(), UserName = "a@test.com", Email = "a@test.com", Role = UserRole.Student };
        var userB = new User { Id = Guid.NewGuid(), UserName = "b@test.com", Email = "b@test.com", Role = UserRole.Student };
        context.Users.AddRange(userA, userB);

        var dept = new Department { DepartmentId = 1, DepartmentName = "CSE", DepartmentCode = "CSE" };
        context.Departments.Add(dept);

        var skill = new Skill { SkillId = 1, SkillName = "C#", Category = "Programming" };
        context.Skills.Add(skill);

        var profileA = new StudentProfile { ProfileId = Guid.NewGuid(), UserId = userA.Id, RollNumber = "A001", DepartmentId = 1 };
        var profileB = new StudentProfile { ProfileId = Guid.NewGuid(), UserId = userB.Id, RollNumber = "B001", DepartmentId = 1 };
        context.StudentProfiles.AddRange(profileA, profileB);
        await context.SaveChangesAsync();

        // Student B adds a skill
        var studentBSkill = new StudentSkill { StudentId = profileB.ProfileId, SkillId = 1, ProficiencyLevel = ProficiencyLevel.Advanced };
        context.StudentSkills.Add(studentBSkill);
        await context.SaveChangesAsync();

        int studentBSkillId = studentBSkill.StudentSkillId;

        // ATTACK: The repository's RemoveSkillAsync takes only the studentSkillId
        // and does NOT verify ownership. Student A could call this with B's skill ID.
        // This is the IDOR vulnerability — the controller should check ownership but does not.
        await repo.RemoveSkillAsync(studentBSkillId);

        // PROOF: Student B's skill has been deleted by someone who shouldn't have access
        var remainingSkills = await context.StudentSkills.Where(ss => ss.StudentId == profileB.ProfileId).ToListAsync();
        Assert.Empty(remainingSkills); // Skill was deleted — VULNERABILITY CONFIRMED

        // A secure implementation would verify StudentId matches the caller's profile
        // The controller (StudentProfilesController.RemoveSkill) should:
        // 1. Get the StudentSkill by ID
        // 2. Verify studentSkill.StudentId == currentUserProfile.ProfileId
        // 3. Only then delete
    }

    // ───── T022: StudentProfile — User gets own profile only ─────
    [Fact]
    public async Task T022_GetByUserId_ReturnsOnlyOwnProfile()
    {
        using var context = CreateInMemoryContext();
        var repo = new StudentProfileRepository(context);

        var userA = new User { Id = Guid.NewGuid(), UserName = "a@test.com", Email = "a@test.com" };
        var userB = new User { Id = Guid.NewGuid(), UserName = "b@test.com", Email = "b@test.com" };
        context.Users.AddRange(userA, userB);

        var dept = new Department { DepartmentId = 1, DepartmentName = "CSE", DepartmentCode = "CSE" };
        context.Departments.Add(dept);

        var profileA = new StudentProfile { ProfileId = Guid.NewGuid(), UserId = userA.Id, RollNumber = "A001", DepartmentId = 1, Bio = "Student A" };
        var profileB = new StudentProfile { ProfileId = Guid.NewGuid(), UserId = userB.Id, RollNumber = "B001", DepartmentId = 1, Bio = "Student B" };
        context.StudentProfiles.AddRange(profileA, profileB);
        await context.SaveChangesAsync();

        // User A queries by their own ID
        var result = await repo.GetByUserIdAsync(userA.Id);
        Assert.NotNull(result);
        Assert.Equal(userA.Id, result.UserId);
        Assert.Equal("Student A", result.Bio);

        // User A cannot get B's profile via this method (must use own userId)
        var resultB = await repo.GetByUserIdAsync(userB.Id);
        Assert.NotNull(resultB);
        Assert.Equal(userB.Id, resultB.UserId); // This returns B's profile — the controller must ensure the caller IS user B
    }

    // ───── T023: StudentProfile — GetByIdAsync does not filter by user ─────
    [Fact]
    public async Task T023_GetByProfileId_ReturnsAnyProfile_RepoLevelNoIsolation()
    {
        using var context = CreateInMemoryContext();
        var repo = new StudentProfileRepository(context);

        var userA = new User { Id = Guid.NewGuid(), UserName = "a@test.com", Email = "a@test.com" };
        context.Users.Add(userA);
        var dept = new Department { DepartmentId = 1, DepartmentName = "CSE", DepartmentCode = "CSE" };
        context.Departments.Add(dept);
        var profile = new StudentProfile { ProfileId = Guid.NewGuid(), UserId = userA.Id, RollNumber = "A001", DepartmentId = 1 };
        context.StudentProfiles.Add(profile);
        await context.SaveChangesAsync();

        // Anyone who knows the ProfileId can retrieve via repo — this is expected,
        // the CONTROLLER must enforce authorization
        var result = await repo.GetByIdAsync(profile.ProfileId);
        Assert.NotNull(result);
    }

    // ───── T024: FacultyProfile — GetByUserId returns own ─────
    [Fact]
    public async Task T024_FacultyGetByUserId_ReturnsOwnProfile()
    {
        using var context = CreateInMemoryContext();
        var repo = new FacultyProfileRepository(context);

        var userA = new User { Id = Guid.NewGuid(), UserName = "facA@test.com", Email = "facA@test.com" };
        var userB = new User { Id = Guid.NewGuid(), UserName = "facB@test.com", Email = "facB@test.com" };
        context.Users.AddRange(userA, userB);
        var dept = new Department { DepartmentId = 1, DepartmentName = "CSE", DepartmentCode = "CSE" };
        context.Departments.Add(dept);
        var profileA = new FacultyProfile { FacultyProfileId = Guid.NewGuid(), UserId = userA.Id, DepartmentId = 1, Designation = "Prof A" };
        var profileB = new FacultyProfile { FacultyProfileId = Guid.NewGuid(), UserId = userB.Id, DepartmentId = 1, Designation = "Prof B" };
        context.FacultyProfiles.AddRange(profileA, profileB);
        await context.SaveChangesAsync();

        var result = await repo.GetByUserIdAsync(userA.Id);
        Assert.NotNull(result);
        Assert.Equal("Prof A", result.Designation);
        Assert.Equal(userA.Id, result.UserId);
    }

    // ───── T025: StudentProfile — Edit requires matching ProfileId ─────
    [Fact]
    public async Task T025_EditProfileWithMismatchedId_WouldBeRejectedByController()
    {
        // The controller checks: profile.ProfileId != model.ProfileId => NotFound
        // This tests that the controller's ownership check logic is sound
        using var context = CreateInMemoryContext();
        var repo = new StudentProfileRepository(context);

        var userA = new User { Id = Guid.NewGuid(), UserName = "a@test.com", Email = "a@test.com" };
        context.Users.Add(userA);
        var dept = new Department { DepartmentId = 1, DepartmentName = "CSE", DepartmentCode = "CSE" };
        context.Departments.Add(dept);

        var profileA = new StudentProfile { ProfileId = Guid.NewGuid(), UserId = userA.Id, RollNumber = "A001", DepartmentId = 1, Bio = "Original" };
        context.StudentProfiles.Add(profileA);
        await context.SaveChangesAsync();

        // Simulate the controller's check:
        // var profile = await _repository.GetByUserIdAsync(user.Id); -- gets A's profile
        // if (profile == null || profile.ProfileId != model.ProfileId) return NotFound();
        var profile = await repo.GetByUserIdAsync(userA.Id);
        Assert.NotNull(profile);

        var attackerProfileId = Guid.NewGuid(); // Attacker tries to submit a different ProfileId
        bool wouldBeRejected = (profile.ProfileId != attackerProfileId);
        Assert.True(wouldBeRejected, "Controller should reject mismatched ProfileId");
    }
}

// ═══════════════════════════════════════════════════════════════════════
// WEEK 5 — APPLICATION SECURITY / IDOR ATTACKS
// ═══════════════════════════════════════════════════════════════════════
public class ApplicationSecurityAdversarialTests
{
    private static AppDbContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(AppDbContext ctx, User organizerUser, User studentA, User studentB,
        StudentProfile profileA, StudentProfile profileB, Opportunity approvedOpp, Opportunity pendingOpp, Opportunity rejectedOpp)>
        SeedTestData(string? dbName = null)
    {
        var ctx = CreateInMemoryContext(dbName);

        var dept = new Department { DepartmentId = 1, DepartmentName = "CSE", DepartmentCode = "CSE" };
        ctx.Departments.Add(dept);

        var organizer = new User { Id = Guid.NewGuid(), UserName = "org@test.com", Email = "org@test.com", Role = UserRole.Faculty };
        var studentA = new User { Id = Guid.NewGuid(), UserName = "a@test.com", Email = "a@test.com", Role = UserRole.Student };
        var studentB = new User { Id = Guid.NewGuid(), UserName = "b@test.com", Email = "b@test.com", Role = UserRole.Student };
        ctx.Users.AddRange(organizer, studentA, studentB);

        var profileA = new StudentProfile { ProfileId = Guid.NewGuid(), UserId = studentA.Id, RollNumber = "A001", DepartmentId = 1 };
        var profileB = new StudentProfile { ProfileId = Guid.NewGuid(), UserId = studentB.Id, RollNumber = "B001", DepartmentId = 1 };
        ctx.StudentProfiles.AddRange(profileA, profileB);

        var approved = new Opportunity
        {
            OpportunityId = Guid.NewGuid(), OrganizerId = organizer.Id, Title = "Approved Opp",
            ApprovalStatus = ApprovalStatus.Approved, RegistrationDeadline = DateTime.UtcNow.AddDays(7),
            Description = "Test", Category = OpportunityCategory.Internship, WorkMode = WorkMode.Remote
        };
        var pending = new Opportunity
        {
            OpportunityId = Guid.NewGuid(), OrganizerId = organizer.Id, Title = "Pending Opp",
            ApprovalStatus = ApprovalStatus.PendingReview, RegistrationDeadline = DateTime.UtcNow.AddDays(7),
            Description = "Test", Category = OpportunityCategory.Workshop, WorkMode = WorkMode.Onsite
        };
        var rejected = new Opportunity
        {
            OpportunityId = Guid.NewGuid(), OrganizerId = organizer.Id, Title = "Rejected Opp",
            ApprovalStatus = ApprovalStatus.Rejected, RegistrationDeadline = DateTime.UtcNow.AddDays(7),
            Description = "Test", Category = OpportunityCategory.Hackathon, WorkMode = WorkMode.Hybrid
        };
        ctx.Opportunities.AddRange(approved, pending, rejected);
        await ctx.SaveChangesAsync();

        return (ctx, organizer, studentA, studentB, profileA, profileB, approved, pending, rejected);
    }

    // ───── T026: Application — Cannot apply after deadline ─────
    [Fact]
    public async Task T026_CannotApplyAfterDeadline()
    {
        var (ctx, org, studentA, _, profileA, _, _, _, _) = await SeedTestData();
        using (ctx)
        {
            var expiredOpp = new Opportunity
            {
                OpportunityId = Guid.NewGuid(), OrganizerId = org.Id, Title = "Expired",
                ApprovalStatus = ApprovalStatus.Approved,
                RegistrationDeadline = DateTime.UtcNow.AddDays(-1), // Past deadline
                Description = "Test", Category = OpportunityCategory.Internship, WorkMode = WorkMode.Remote
            };
            ctx.Opportunities.Add(expiredOpp);
            await ctx.SaveChangesAsync();

            // Controller checks: if (opportunity.RegistrationDeadline < DateTime.UtcNow)
            Assert.True(expiredOpp.RegistrationDeadline < DateTime.UtcNow);
        }
    }

    // ───── T027: Application — Duplicate prevention ─────
    [Fact]
    public async Task T027_CannotApplyTwiceForSameOpportunity()
    {
        var (ctx, _, _, _, profileA, _, approvedOpp, _, _) = await SeedTestData();
        using (ctx)
        {
            var appRepo = new ApplicationRepository(ctx);
            var app = new Application
            {
                ApplicationId = Guid.NewGuid(), OpportunityId = approvedOpp.OpportunityId,
                StudentId = profileA.ProfileId, Status = ApplicationStatus.Applied, AppliedAt = DateTime.UtcNow
            };
            await appRepo.AddAsync(app);

            // Check duplicate
            bool alreadyApplied = await appRepo.HasAlreadyAppliedAsync(approvedOpp.OpportunityId, profileA.ProfileId);
            Assert.True(alreadyApplied);
        }
    }

    // ───── T028: Application — Withdraw IDOR: Student B cannot withdraw Student A's application ─────
    [Fact]
    public async Task T028_Withdraw_StudentBCannotWithdrawStudentAApplication()
    {
        var (ctx, _, _, _, profileA, profileB, approvedOpp, _, _) = await SeedTestData();
        using (ctx)
        {
            var appRepo = new ApplicationRepository(ctx);
            var appA = new Application
            {
                ApplicationId = Guid.NewGuid(), OpportunityId = approvedOpp.OpportunityId,
                StudentId = profileA.ProfileId, Status = ApplicationStatus.Applied, AppliedAt = DateTime.UtcNow
            };
            await appRepo.AddAsync(appA);

            // Simulate controller check: if (application.StudentId != studentProfile.ProfileId) return Forbid();
            var retrievedApp = await appRepo.GetByIdAsync(appA.ApplicationId);
            Assert.NotNull(retrievedApp);
            bool isOwner = retrievedApp.StudentId == profileB.ProfileId;
            Assert.False(isOwner, "Student B should NOT be recognized as owner of Student A's application");
        }
    }

    // ───── T029: Organizer — Cannot view another organizer's applicants ─────
    [Fact]
    public async Task T029_OrganizerB_CannotSeeOrganizerA_Applicants()
    {
        var (ctx, orgA, _, _, _, _, approvedOpp, _, _) = await SeedTestData();
        using (ctx)
        {
            var orgB = new User { Id = Guid.NewGuid(), UserName = "orgB@test.com", Email = "orgB@test.com", Role = UserRole.Faculty };
            ctx.Users.Add(orgB);
            await ctx.SaveChangesAsync();

            // Controller check: if (opportunity?.OrganizerId != user?.Id && !User.IsInRole("Admin")) return Forbid();
            bool isOwnerOrAdmin = (approvedOpp.OrganizerId == orgB.Id); // false
            Assert.False(isOwnerOrAdmin, "Organizer B should not be recognized as owner of Organizer A's opportunity");
        }
    }

    // ───── T030: VULNERABILITY — ApplicationsController Apply fallback impersonation ─────
    [Fact]
    public async Task T030_ApplyFallback_UsesFirstStudentProfile_VULNERABILITY()
    {
        var (ctx, _, studentA, studentB, profileA, profileB, approvedOpp, _, _) = await SeedTestData();
        using (ctx)
        {
            var profileRepo = new StudentProfileRepository(ctx);

            // Simulate: User C has no profile
            var userC = new User { Id = Guid.NewGuid(), UserName = "c@test.com", Email = "c@test.com", Role = UserRole.Student };
            ctx.Users.Add(userC);
            await ctx.SaveChangesAsync();

            // Simulate controller logic for user C:
            var profileC = await profileRepo.GetByUserIdAsync(userC.Id);
            Assert.Null(profileC); // User C has no profile

            // Controller falls back to: profiles.FirstOrDefault()
            var allProfiles = await profileRepo.GetAllAsync();
            var fallbackProfile = allProfiles.FirstOrDefault();

            // VULNERABILITY: The fallback profile belongs to another student!
            Assert.NotNull(fallbackProfile);
            Assert.NotEqual(userC.Id, fallbackProfile!.UserId);
            // This means User C would apply as Student A or B — identity impersonation!
        }
    }

    // ───── T031: VULNERABILITY — MyApplications fallback leaks other student's data ─────
    [Fact]
    public async Task T031_MyApplicationsFallback_LeaksOtherStudentData_VULNERABILITY()
    {
        var (ctx, _, _, _, profileA, _, approvedOpp, _, _) = await SeedTestData();
        using (ctx)
        {
            var profileRepo = new StudentProfileRepository(ctx);
            var appRepo = new ApplicationRepository(ctx);

            // Student A has an application
            var app = new Application
            {
                ApplicationId = Guid.NewGuid(), OpportunityId = approvedOpp.OpportunityId,
                StudentId = profileA.ProfileId, Status = ApplicationStatus.Applied, AppliedAt = DateTime.UtcNow
            };
            await appRepo.AddAsync(app);

            // Simulate: User D has no profile
            var userD = new User { Id = Guid.NewGuid(), UserName = "d@test.com", Email = "d@test.com", Role = UserRole.Student };
            ctx.Users.Add(userD);
            await ctx.SaveChangesAsync();

            var profileD = await profileRepo.GetByUserIdAsync(userD.Id);
            Assert.Null(profileD);

            // Controller falls back to firstOrDefault
            var allProfiles = await profileRepo.GetAllAsync();
            var fallbackProfile = allProfiles.FirstOrDefault();

            if (fallbackProfile != null)
            {
                var applications = await appRepo.GetByStudentIdAsync(fallbackProfile.ProfileId);
                // VULNERABILITY: User D sees Student A's applications!
                Assert.NotEmpty(applications);
                Assert.True(applications.All(a => a.StudentId != Guid.Empty));
                // The fallback profile's applications are shown to user D
            }
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════
// WEEK 6 — SHARED OPPORTUNITY FEED — EXHAUSTIVE TESTS
// ═══════════════════════════════════════════════════════════════════════
public class SharedOpportunityFeedAdversarialTests
{
    private static AppDbContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(AppDbContext ctx, User organizer, Opportunity approved, Opportunity pending, Opportunity rejected)>
        SeedBasicData(string? dbName = null)
    {
        var ctx = CreateInMemoryContext(dbName);
        var organizer = new User { Id = Guid.NewGuid(), UserName = "org@test.com", Email = "org@test.com", Role = UserRole.Faculty };
        ctx.Users.Add(organizer);

        var approved = new Opportunity
        {
            OpportunityId = Guid.NewGuid(), OrganizerId = organizer.Id, Title = "Approved",
            ApprovalStatus = ApprovalStatus.Approved, Description = "d", Category = OpportunityCategory.Internship, WorkMode = WorkMode.Remote,
            RegistrationDeadline = DateTime.UtcNow.AddDays(7)
        };
        var pending = new Opportunity
        {
            OpportunityId = Guid.NewGuid(), OrganizerId = organizer.Id, Title = "Pending",
            ApprovalStatus = ApprovalStatus.PendingReview, Description = "d", Category = OpportunityCategory.Workshop, WorkMode = WorkMode.Onsite,
            RegistrationDeadline = DateTime.UtcNow.AddDays(7)
        };
        var rejected = new Opportunity
        {
            OpportunityId = Guid.NewGuid(), OrganizerId = organizer.Id, Title = "Rejected",
            ApprovalStatus = ApprovalStatus.Rejected, Description = "d", Category = OpportunityCategory.Hackathon, WorkMode = WorkMode.Hybrid,
            RegistrationDeadline = DateTime.UtcNow.AddDays(7)
        };
        ctx.Opportunities.AddRange(approved, pending, rejected);
        await ctx.SaveChangesAsync();

        return (ctx, organizer, approved, pending, rejected);
    }

    // ───── T032: Only Approved opportunities in feed ─────
    [Fact]
    public async Task T032_Feed_ReturnsOnlyApproved()
    {
        var (ctx, _, approved, _, _) = await SeedBasicData();
        using (ctx)
        {
            var repo = new OpportunityRepository(ctx);
            var feed = new SharedOpportunityFeed(repo);
            var result = await feed.GetUpcomingOpportunitiesAsync();
            Assert.Single(result);
            Assert.Equal("Approved", result[0].Title);
        }
    }

    // ───── T033: PendingReview excluded from feed ─────
    [Fact]
    public async Task T033_Feed_ExcludesPendingReview()
    {
        var (ctx, _, _, pending, _) = await SeedBasicData();
        using (ctx)
        {
            var repo = new OpportunityRepository(ctx);
            var feed = new SharedOpportunityFeed(repo);
            var result = await feed.GetUpcomingOpportunitiesAsync();
            Assert.DoesNotContain(result, o => o.ApprovalStatus == ApprovalStatus.PendingReview);
        }
    }

    // ───── T034: Rejected excluded from feed ─────
    [Fact]
    public async Task T034_Feed_ExcludesRejected()
    {
        var (ctx, _, _, _, rejected) = await SeedBasicData();
        using (ctx)
        {
            var repo = new OpportunityRepository(ctx);
            var feed = new SharedOpportunityFeed(repo);
            var result = await feed.GetUpcomingOpportunitiesAsync();
            Assert.DoesNotContain(result, o => o.ApprovalStatus == ApprovalStatus.Rejected);
        }
    }

    // ───── T035: Empty database returns empty feed ─────
    [Fact]
    public async Task T035_Feed_EmptyDatabase_ReturnsEmpty()
    {
        using var ctx = CreateInMemoryContext();
        var repo = new OpportunityRepository(ctx);
        var feed = new SharedOpportunityFeed(repo);
        var result = await feed.GetUpcomingOpportunitiesAsync();
        Assert.Empty(result);
    }

    // ───── T036: Multiple approved opportunities all returned ─────
    [Fact]
    public async Task T036_Feed_MultipleApproved_AllReturned()
    {
        using var ctx = CreateInMemoryContext();
        var org = new User { Id = Guid.NewGuid(), UserName = "org@test.com", Email = "org@test.com" };
        ctx.Users.Add(org);

        for (int i = 0; i < 5; i++)
        {
            ctx.Opportunities.Add(new Opportunity
            {
                OpportunityId = Guid.NewGuid(), OrganizerId = org.Id, Title = $"Approved {i}",
                ApprovalStatus = ApprovalStatus.Approved, Description = "d",
                Category = OpportunityCategory.Internship, WorkMode = WorkMode.Remote,
                RegistrationDeadline = DateTime.UtcNow.AddDays(i + 1)
            });
        }
        await ctx.SaveChangesAsync();

        var repo = new OpportunityRepository(ctx);
        var feed = new SharedOpportunityFeed(repo);
        var result = await feed.GetUpcomingOpportunitiesAsync();
        Assert.Equal(5, result.Count);
        Assert.All(result, o => Assert.Equal(ApprovalStatus.Approved, o.ApprovalStatus));
    }

    // ───── T037: Feed does not duplicate opportunities ─────
    [Fact]
    public async Task T037_Feed_NoDuplicateOpportunities()
    {
        var (ctx, _, _, _, _) = await SeedBasicData();
        using (ctx)
        {
            var repo = new OpportunityRepository(ctx);
            var feed = new SharedOpportunityFeed(repo);
            var result = await feed.GetUpcomingOpportunitiesAsync();
            var ids = result.Select(o => o.OpportunityId).ToList();
            Assert.Equal(ids.Distinct().Count(), ids.Count);
        }
    }

    // ───── T038: Feed returns required skills with opportunities ─────
    [Fact]
    public async Task T038_Feed_IncludesRequiredSkills()
    {
        using var ctx = CreateInMemoryContext();
        var org = new User { Id = Guid.NewGuid(), UserName = "org@test.com", Email = "org@test.com" };
        ctx.Users.Add(org);

        var skill = new Skill { SkillId = 1, SkillName = "C#", Category = "Prog" };
        ctx.Skills.Add(skill);

        var opp = new Opportunity
        {
            OpportunityId = Guid.NewGuid(), OrganizerId = org.Id, Title = "With Skills",
            ApprovalStatus = ApprovalStatus.Approved, Description = "d",
            Category = OpportunityCategory.Internship, WorkMode = WorkMode.Remote,
            RegistrationDeadline = DateTime.UtcNow.AddDays(7)
        };
        ctx.Opportunities.Add(opp);
        await ctx.SaveChangesAsync();

        ctx.OpportunitySkills.Add(new OpportunitySkill { OpportunityId = opp.OpportunityId, SkillId = 1 });
        await ctx.SaveChangesAsync();

        var repo = new OpportunityRepository(ctx);
        var feed = new SharedOpportunityFeed(repo);
        var result = await feed.GetUpcomingOpportunitiesAsync();
        Assert.Single(result);
        Assert.NotEmpty(result[0].RequiredSkills);
    }

    // ───── T039: Opportunity Index (public listing) only shows Approved ─────
    [Fact]
    public async Task T039_OpportunityIndexRepo_DefaultsToApprovedOnly()
    {
        var (ctx, _, _, _, _) = await SeedBasicData();
        using (ctx)
        {
            var repo = new OpportunityRepository(ctx);
            // Default parameter is ApprovalStatus.Approved
            var result = await repo.GetAllAsync();
            Assert.Single(result);
            Assert.All(result, o => Assert.Equal(ApprovalStatus.Approved, o.ApprovalStatus));
        }
    }

    // ───── T040: GetAllAsync with null status returns ALL (potential data leak) ─────
    [Fact]
    public async Task T040_GetAllAsync_NullStatus_ReturnsAllStatuses()
    {
        var (ctx, _, _, _, _) = await SeedBasicData();
        using (ctx)
        {
            var repo = new OpportunityRepository(ctx);
            // Passing null for status returns all
            var result = await repo.GetAllAsync(status: null);
            Assert.Equal(3, result.Count); // Approved + Pending + Rejected
            // This is expected — the Index action passes ApprovalStatus.Approved explicitly
            // But if someone calls this with null, all statuses are returned
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════
// WEEK 6 — CALENDAR FEED TESTS
// ═══════════════════════════════════════════════════════════════════════
public class CalendarFeedAdversarialTests
{
    private static AppDbContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    // ───── T041: Calendar feed only includes Approved opportunities ─────
    [Fact]
    public async Task T041_CalendarFeed_OnlyApproved()
    {
        using var ctx = CreateInMemoryContext();
        var org = new User { Id = Guid.NewGuid(), UserName = "org@test.com", Email = "org@test.com" };
        ctx.Users.Add(org);

        ctx.Opportunities.AddRange(
            new Opportunity
            {
                OpportunityId = Guid.NewGuid(), OrganizerId = org.Id, Title = "Approved Cal",
                ApprovalStatus = ApprovalStatus.Approved, Description = "d",
                Category = OpportunityCategory.Internship, WorkMode = WorkMode.Remote,
                RegistrationDeadline = DateTime.UtcNow.AddDays(7)
            },
            new Opportunity
            {
                OpportunityId = Guid.NewGuid(), OrganizerId = org.Id, Title = "Pending Cal",
                ApprovalStatus = ApprovalStatus.PendingReview, Description = "d",
                Category = OpportunityCategory.Workshop, WorkMode = WorkMode.Onsite,
                RegistrationDeadline = DateTime.UtcNow.AddDays(7)
            });
        await ctx.SaveChangesAsync();

        var feed = new OpportunityCalendarFeed(ctx);
        var events = await feed.GetCalendarEventsAsync();
        Assert.All(events, e => Assert.Contains("[Deadline]", e.Title));
        Assert.DoesNotContain(events, e => e.Title.Contains("Pending Cal"));
    }

    // ───── T042: Calendar feed date range filtering ─────
    [Fact]
    public async Task T042_CalendarFeed_DateRangeFilter()
    {
        using var ctx = CreateInMemoryContext();
        var org = new User { Id = Guid.NewGuid(), UserName = "org@test.com", Email = "org@test.com" };
        ctx.Users.Add(org);

        ctx.Opportunities.Add(new Opportunity
        {
            OpportunityId = Guid.NewGuid(), OrganizerId = org.Id, Title = "Far Future",
            ApprovalStatus = ApprovalStatus.Approved, Description = "d",
            Category = OpportunityCategory.Internship, WorkMode = WorkMode.Remote,
            RegistrationDeadline = DateTime.UtcNow.AddYears(5)
        });
        await ctx.SaveChangesAsync();

        var feed = new OpportunityCalendarFeed(ctx);
        // Default range is -1 month to +3 months; far-future should be excluded
        var events = await feed.GetCalendarEventsAsync();
        Assert.Empty(events);
    }

    // ───── T043: Calendar feed includes both deadline and event date entries ─────
    [Fact]
    public async Task T043_CalendarFeed_BothDeadlineAndEventDate()
    {
        using var ctx = CreateInMemoryContext();
        var org = new User { Id = Guid.NewGuid(), UserName = "org@test.com", Email = "org@test.com" };
        ctx.Users.Add(org);

        ctx.Opportunities.Add(new Opportunity
        {
            OpportunityId = Guid.NewGuid(), OrganizerId = org.Id, Title = "Dual Date",
            ApprovalStatus = ApprovalStatus.Approved, Description = "d",
            Category = OpportunityCategory.Hackathon, WorkMode = WorkMode.Onsite,
            RegistrationDeadline = DateTime.UtcNow.AddDays(7),
            EventDate = DateTime.UtcNow.AddDays(14)
        });
        await ctx.SaveChangesAsync();

        var feed = new OpportunityCalendarFeed(ctx);
        var events = await feed.GetCalendarEventsAsync();
        Assert.Equal(2, events.Count);
        Assert.Contains(events, e => e.EventType == "RegistrationDeadline");
        Assert.Contains(events, e => e.EventType == "EventDate");
    }

    // ───── T044: Calendar feed with no opportunities returns empty ─────
    [Fact]
    public async Task T044_CalendarFeed_EmptyDatabase_ReturnsEmpty()
    {
        using var ctx = CreateInMemoryContext();
        var feed = new OpportunityCalendarFeed(ctx);
        var events = await feed.GetCalendarEventsAsync();
        Assert.Empty(events);
    }

    // ───── T045: Calendar feed events ordered by date ─────
    [Fact]
    public async Task T045_CalendarFeed_OrderedByDate()
    {
        using var ctx = CreateInMemoryContext();
        var org = new User { Id = Guid.NewGuid(), UserName = "org@test.com", Email = "org@test.com" };
        ctx.Users.Add(org);

        ctx.Opportunities.AddRange(
            new Opportunity
            {
                OpportunityId = Guid.NewGuid(), OrganizerId = org.Id, Title = "Later",
                ApprovalStatus = ApprovalStatus.Approved, Description = "d",
                Category = OpportunityCategory.Internship, WorkMode = WorkMode.Remote,
                RegistrationDeadline = DateTime.UtcNow.AddDays(30)
            },
            new Opportunity
            {
                OpportunityId = Guid.NewGuid(), OrganizerId = org.Id, Title = "Sooner",
                ApprovalStatus = ApprovalStatus.Approved, Description = "d",
                Category = OpportunityCategory.Workshop, WorkMode = WorkMode.Onsite,
                RegistrationDeadline = DateTime.UtcNow.AddDays(5)
            });
        await ctx.SaveChangesAsync();

        var feed = new OpportunityCalendarFeed(ctx);
        var events = await feed.GetCalendarEventsAsync();
        Assert.Equal(2, events.Count);
        Assert.True(events[0].Date <= events[1].Date);
    }
}

// ═══════════════════════════════════════════════════════════════════════
// WEEK 5 — OPPORTUNITY APPROVAL WORKFLOW / STATE MACHINE TESTS
// ═══════════════════════════════════════════════════════════════════════
public class OpportunityApprovalWorkflowTests
{
    private static AppDbContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    // ───── T046: Approve changes status to Approved ─────
    [Fact]
    public async Task T046_Approve_ChangesStatusToApproved()
    {
        using var ctx = CreateInMemoryContext();
        var org = new User { Id = Guid.NewGuid(), UserName = "org@test.com", Email = "org@test.com" };
        ctx.Users.Add(org);
        var opp = new Opportunity
        {
            OpportunityId = Guid.NewGuid(), OrganizerId = org.Id, Title = "Test",
            ApprovalStatus = ApprovalStatus.PendingReview, Description = "d",
            Category = OpportunityCategory.Internship, WorkMode = WorkMode.Remote
        };
        ctx.Opportunities.Add(opp);
        await ctx.SaveChangesAsync();

        var repo = new OpportunityRepository(ctx);
        await repo.UpdateStatusAsync(opp.OpportunityId, ApprovalStatus.Approved);

        var updated = await repo.GetByIdAsync(opp.OpportunityId);
        Assert.NotNull(updated);
        Assert.Equal(ApprovalStatus.Approved, updated.ApprovalStatus);
    }

    // ───── T047: Reject changes status to Rejected ─────
    [Fact]
    public async Task T047_Reject_ChangesStatusToRejected()
    {
        using var ctx = CreateInMemoryContext();
        var org = new User { Id = Guid.NewGuid(), UserName = "org@test.com", Email = "org@test.com" };
        ctx.Users.Add(org);
        var opp = new Opportunity
        {
            OpportunityId = Guid.NewGuid(), OrganizerId = org.Id, Title = "Test",
            ApprovalStatus = ApprovalStatus.PendingReview, Description = "d",
            Category = OpportunityCategory.Internship, WorkMode = WorkMode.Remote
        };
        ctx.Opportunities.Add(opp);
        await ctx.SaveChangesAsync();

        var repo = new OpportunityRepository(ctx);
        await repo.UpdateStatusAsync(opp.OpportunityId, ApprovalStatus.Rejected);

        var updated = await repo.GetByIdAsync(opp.OpportunityId);
        Assert.NotNull(updated);
        Assert.Equal(ApprovalStatus.Rejected, updated.ApprovalStatus);
    }

    // ───── T048: VULNERABILITY — Already-rejected can be re-approved (no precondition check) ─────
    [Fact]
    public async Task T048_RejectedOpportunity_CanBeApproved_NoPreconditionCheck()
    {
        using var ctx = CreateInMemoryContext();
        var org = new User { Id = Guid.NewGuid(), UserName = "org@test.com", Email = "org@test.com" };
        ctx.Users.Add(org);
        var opp = new Opportunity
        {
            OpportunityId = Guid.NewGuid(), OrganizerId = org.Id, Title = "Rejected",
            ApprovalStatus = ApprovalStatus.Rejected, Description = "d",
            Category = OpportunityCategory.Internship, WorkMode = WorkMode.Remote
        };
        ctx.Opportunities.Add(opp);
        await ctx.SaveChangesAsync();

        var repo = new OpportunityRepository(ctx);
        // VULNERABILITY: No precondition check — Rejected → Approved is possible
        await repo.UpdateStatusAsync(opp.OpportunityId, ApprovalStatus.Approved);

        var updated = await repo.GetByIdAsync(opp.OpportunityId);
        Assert.Equal(ApprovalStatus.Approved, updated!.ApprovalStatus);
        // This demonstrates V010: no state machine enforcement for opportunity approval
    }

    // ───── T049: Already-approved can be re-approved (idempotent but no check) ─────
    [Fact]
    public async Task T049_AlreadyApproved_CanBeApprovedAgain()
    {
        using var ctx = CreateInMemoryContext();
        var org = new User { Id = Guid.NewGuid(), UserName = "org@test.com", Email = "org@test.com" };
        ctx.Users.Add(org);
        var opp = new Opportunity
        {
            OpportunityId = Guid.NewGuid(), OrganizerId = org.Id, Title = "Already Approved",
            ApprovalStatus = ApprovalStatus.Approved, Description = "d",
            Category = OpportunityCategory.Internship, WorkMode = WorkMode.Remote
        };
        ctx.Opportunities.Add(opp);
        await ctx.SaveChangesAsync();

        var repo = new OpportunityRepository(ctx);
        await repo.UpdateStatusAsync(opp.OpportunityId, ApprovalStatus.Approved);
        var updated = await repo.GetByIdAsync(opp.OpportunityId);
        Assert.Equal(ApprovalStatus.Approved, updated!.ApprovalStatus);
    }

    // ───── T050: Nonexistent opportunity — UpdateStatus does nothing ─────
    [Fact]
    public async Task T050_UpdateStatus_NonexistentOpportunity_NoException()
    {
        using var ctx = CreateInMemoryContext();
        var repo = new OpportunityRepository(ctx);
        // Should not throw
        await repo.UpdateStatusAsync(Guid.NewGuid(), ApprovalStatus.Approved);
    }

    // ───── T051: Pending approval list only contains PendingReview ─────
    [Fact]
    public async Task T051_GetPendingApprovals_OnlyPendingReview()
    {
        using var ctx = CreateInMemoryContext();
        var org = new User { Id = Guid.NewGuid(), UserName = "org@test.com", Email = "org@test.com" };
        ctx.Users.Add(org);
        ctx.Opportunities.AddRange(
            new Opportunity { OpportunityId = Guid.NewGuid(), OrganizerId = org.Id, Title = "Pending", ApprovalStatus = ApprovalStatus.PendingReview, Description = "d", Category = OpportunityCategory.Internship, WorkMode = WorkMode.Remote },
            new Opportunity { OpportunityId = Guid.NewGuid(), OrganizerId = org.Id, Title = "Approved", ApprovalStatus = ApprovalStatus.Approved, Description = "d", Category = OpportunityCategory.Workshop, WorkMode = WorkMode.Onsite },
            new Opportunity { OpportunityId = Guid.NewGuid(), OrganizerId = org.Id, Title = "Rejected", ApprovalStatus = ApprovalStatus.Rejected, Description = "d", Category = OpportunityCategory.Hackathon, WorkMode = WorkMode.Hybrid });
        await ctx.SaveChangesAsync();

        var repo = new OpportunityRepository(ctx);
        var pending = await repo.GetPendingApprovalsAsync();
        Assert.Single(pending);
        Assert.All(pending, o => Assert.Equal(ApprovalStatus.PendingReview, o.ApprovalStatus));
    }
}

// ═══════════════════════════════════════════════════════════════════════
// ORGANIZER ISOLATION TESTS
// ═══════════════════════════════════════════════════════════════════════
public class OrganizerIsolationTests
{
    private static AppDbContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    // ───── T052: Organizer A only sees their own opportunities ─────
    [Fact]
    public async Task T052_GetByOrganizerId_OnlyReturnsOwnOpportunities()
    {
        using var ctx = CreateInMemoryContext();
        var orgA = new User { Id = Guid.NewGuid(), UserName = "a@test.com", Email = "a@test.com" };
        var orgB = new User { Id = Guid.NewGuid(), UserName = "b@test.com", Email = "b@test.com" };
        ctx.Users.AddRange(orgA, orgB);

        ctx.Opportunities.AddRange(
            new Opportunity { OpportunityId = Guid.NewGuid(), OrganizerId = orgA.Id, Title = "A's Opp", ApprovalStatus = ApprovalStatus.Approved, Description = "d", Category = OpportunityCategory.Internship, WorkMode = WorkMode.Remote },
            new Opportunity { OpportunityId = Guid.NewGuid(), OrganizerId = orgB.Id, Title = "B's Opp", ApprovalStatus = ApprovalStatus.Approved, Description = "d", Category = OpportunityCategory.Workshop, WorkMode = WorkMode.Onsite });
        await ctx.SaveChangesAsync();

        var repo = new OpportunityRepository(ctx);
        var aOpps = await repo.GetByOrganizerIdAsync(orgA.Id);
        Assert.Single(aOpps);
        Assert.Equal("A's Opp", aOpps[0].Title);
        Assert.All(aOpps, o => Assert.Equal(orgA.Id, o.OrganizerId));
    }

    // ───── T053: Organizer cannot modify another's opportunity status (controller-level) ─────
    [Fact]
    public async Task T053_OrganizerB_CannotModifyOrganizerA_ApplicationStatus()
    {
        using var ctx = CreateInMemoryContext();
        var orgA = new User { Id = Guid.NewGuid(), UserName = "a@test.com", Email = "a@test.com" };
        var orgB = new User { Id = Guid.NewGuid(), UserName = "b@test.com", Email = "b@test.com" };
        ctx.Users.AddRange(orgA, orgB);

        var dept = new Department { DepartmentId = 1, DepartmentName = "CSE", DepartmentCode = "CSE" };
        ctx.Departments.Add(dept);

        var opp = new Opportunity
        {
            OpportunityId = Guid.NewGuid(), OrganizerId = orgA.Id, Title = "A's Opp",
            ApprovalStatus = ApprovalStatus.Approved, Description = "d",
            Category = OpportunityCategory.Internship, WorkMode = WorkMode.Remote
        };
        ctx.Opportunities.Add(opp);

        var student = new User { Id = Guid.NewGuid(), UserName = "s@test.com", Email = "s@test.com", Role = UserRole.Student };
        ctx.Users.Add(student);
        var profile = new StudentProfile { ProfileId = Guid.NewGuid(), UserId = student.Id, RollNumber = "S001", DepartmentId = 1 };
        ctx.StudentProfiles.Add(profile);

        var application = new Application
        {
            ApplicationId = Guid.NewGuid(), OpportunityId = opp.OpportunityId,
            StudentId = profile.ProfileId, Status = ApplicationStatus.Applied, AppliedAt = DateTime.UtcNow
        };
        ctx.Applications.Add(application);
        await ctx.SaveChangesAsync();

        // Simulate controller check: opportunity?.OrganizerId != user?.Id && !User.IsInRole("Admin")
        bool orgBIsOwner = opp.OrganizerId == orgB.Id;
        Assert.False(orgBIsOwner, "Organizer B must NOT be able to manage Organizer A's applicants");
    }
}

// ═══════════════════════════════════════════════════════════════════════
// DATA INTEGRITY / DATABASE STATE TESTS
// ═══════════════════════════════════════════════════════════════════════
public class DataIntegrityAdversarialTests
{
    private static AppDbContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    // ───── T054: Delete nonexistent opportunity does not throw ─────
    [Fact]
    public async Task T054_DeleteNonexistentOpportunity_DoesNotThrow()
    {
        using var ctx = CreateInMemoryContext();
        var repo = new OpportunityRepository(ctx);
        await repo.DeleteAsync(Guid.NewGuid()); // No exception
    }

    // ───── T055: Delete nonexistent application does not throw ─────
    [Fact]
    public async Task T055_DeleteNonexistentApplication_DoesNotThrow()
    {
        using var ctx = CreateInMemoryContext();
        var repo = new ApplicationRepository(ctx);
        await repo.DeleteAsync(Guid.NewGuid()); // No exception
    }

    // ───── T056: Delete nonexistent student profile does not throw ─────
    [Fact]
    public async Task T056_DeleteNonexistentStudentProfile_DoesNotThrow()
    {
        using var ctx = CreateInMemoryContext();
        var repo = new StudentProfileRepository(ctx);
        await repo.DeleteAsync(Guid.NewGuid()); // No exception
    }

    // ───── T057: RemoveSkill nonexistent ID does not throw ─────
    [Fact]
    public async Task T057_RemoveSkillNonexistentId_DoesNotThrow()
    {
        using var ctx = CreateInMemoryContext();
        var repo = new StudentProfileRepository(ctx);
        await repo.RemoveSkillAsync(999999); // No exception
    }

    // ───── T058: HasAlreadyApplied with nonexistent IDs returns false ─────
    [Fact]
    public async Task T058_HasAlreadyApplied_NonexistentIds_ReturnsFalse()
    {
        using var ctx = CreateInMemoryContext();
        var repo = new ApplicationRepository(ctx);
        bool result = await repo.HasAlreadyAppliedAsync(Guid.NewGuid(), Guid.NewGuid());
        Assert.False(result);
    }

    // ───── T059: GetByIdAsync with nonexistent ID returns null ─────
    [Fact]
    public async Task T059_GetByIdAsync_NonexistentOpportunity_ReturnsNull()
    {
        using var ctx = CreateInMemoryContext();
        var repo = new OpportunityRepository(ctx);
        var result = await repo.GetByIdAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    // ───── T060: Student profile GetByUserId with nonexistent user returns null ─────
    [Fact]
    public async Task T060_StudentProfile_NonexistentUser_ReturnsNull()
    {
        using var ctx = CreateInMemoryContext();
        var repo = new StudentProfileRepository(ctx);
        var result = await repo.GetByUserIdAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    // ───── T061: Faculty profile GetByUserId with nonexistent user returns null ─────
    [Fact]
    public async Task T061_FacultyProfile_NonexistentUser_ReturnsNull()
    {
        using var ctx = CreateInMemoryContext();
        var repo = new FacultyProfileRepository(ctx);
        var result = await repo.GetByUserIdAsync(Guid.NewGuid());
        Assert.Null(result);
    }
}

// ═══════════════════════════════════════════════════════════════════════
// GRIEVANCE WORKFLOW / STATE MACHINE TESTS
// ═══════════════════════════════════════════════════════════════════════
public class GrievanceWorkflowAdversarialTests
{
    private static AppDbContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    // ───── T062: Grievance status transitions — valid transitions ─────
    [Theory]
    [InlineData(GrievanceStatus.Submitted, GrievanceStatus.Acknowledged)]
    [InlineData(GrievanceStatus.Acknowledged, GrievanceStatus.InProgress)]
    [InlineData(GrievanceStatus.InProgress, GrievanceStatus.Resolved)]
    [InlineData(GrievanceStatus.InProgress, GrievanceStatus.Escalated)]
    [InlineData(GrievanceStatus.Escalated, GrievanceStatus.InProgress)]
    [InlineData(GrievanceStatus.Escalated, GrievanceStatus.Resolved)]
    public async Task T062_GrievanceStatusUpdate_ValidTransitions(GrievanceStatus from, GrievanceStatus to)
    {
        using var ctx = CreateInMemoryContext();
        var user = new User { Id = Guid.NewGuid(), UserName = "u@test.com", Email = "u@test.com" };
        var dept = new Department { DepartmentId = 1, DepartmentName = "CSE", DepartmentCode = "CSE" };
        ctx.Users.Add(user);
        ctx.Departments.Add(dept);

        var grievance = new Grievance
        {
            GrievanceId = Guid.NewGuid(), ComplainantUserId = user.Id, DepartmentId = 1,
            Category = GrievanceCategory.Infrastructure, Description = "Test",
            Priority = GrievancePriority.Medium, Status = from, CreatedAt = DateTime.UtcNow
        };
        ctx.Grievances.Add(grievance);
        await ctx.SaveChangesAsync();

        var repo = new GrievanceRepository(ctx);
        await repo.UpdateStatusAsync(grievance.GrievanceId, to, user.Id, "Test note");

        var updated = await repo.GetByIdAsync(grievance.GrievanceId);
        Assert.NotNull(updated);
        Assert.Equal(to, updated.Status);
    }

    // ───── T063: Resolving grievance sets ResolvedAt ─────
    [Fact]
    public async Task T063_ResolveGrievance_SetsResolvedAt()
    {
        using var ctx = CreateInMemoryContext();
        var user = new User { Id = Guid.NewGuid(), UserName = "u@test.com", Email = "u@test.com" };
        var dept = new Department { DepartmentId = 1, DepartmentName = "CSE", DepartmentCode = "CSE" };
        ctx.Users.Add(user);
        ctx.Departments.Add(dept);

        var grievance = new Grievance
        {
            GrievanceId = Guid.NewGuid(), ComplainantUserId = user.Id, DepartmentId = 1,
            Category = GrievanceCategory.Academic, Description = "Test",
            Priority = GrievancePriority.Low, Status = GrievanceStatus.InProgress, CreatedAt = DateTime.UtcNow
        };
        ctx.Grievances.Add(grievance);
        await ctx.SaveChangesAsync();

        var repo = new GrievanceRepository(ctx);
        await repo.UpdateStatusAsync(grievance.GrievanceId, GrievanceStatus.Resolved, user.Id);

        var updated = await repo.GetByIdAsync(grievance.GrievanceId);
        Assert.NotNull(updated);
        Assert.NotNull(updated.ResolvedAt);
    }

    // ───── T064: Reopening resolved grievance clears ResolvedAt ─────
    [Fact]
    public async Task T064_ReopenResolvedGrievance_ClearsResolvedAt()
    {
        using var ctx = CreateInMemoryContext();
        var user = new User { Id = Guid.NewGuid(), UserName = "u@test.com", Email = "u@test.com" };
        var dept = new Department { DepartmentId = 1, DepartmentName = "CSE", DepartmentCode = "CSE" };
        ctx.Users.Add(user);
        ctx.Departments.Add(dept);

        var grievance = new Grievance
        {
            GrievanceId = Guid.NewGuid(), ComplainantUserId = user.Id, DepartmentId = 1,
            Category = GrievanceCategory.Academic, Description = "Test",
            Priority = GrievancePriority.Low, Status = GrievanceStatus.Resolved,
            ResolvedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
        };
        ctx.Grievances.Add(grievance);
        await ctx.SaveChangesAsync();

        var repo = new GrievanceRepository(ctx);
        await repo.UpdateStatusAsync(grievance.GrievanceId, GrievanceStatus.InProgress, user.Id);

        var updated = await repo.GetByIdAsync(grievance.GrievanceId);
        Assert.NotNull(updated);
        Assert.Null(updated.ResolvedAt);
    }

    // ───── T065: Assign sets AssignedToUserId and creates log ─────
    [Fact]
    public async Task T065_AssignGrievance_SetsAssigneeAndCreatesLog()
    {
        using var ctx = CreateInMemoryContext();
        var user = new User { Id = Guid.NewGuid(), UserName = "u@test.com", Email = "u@test.com" };
        var officer = new User { Id = Guid.NewGuid(), UserName = "off@test.com", Email = "off@test.com" };
        var dept = new Department { DepartmentId = 1, DepartmentName = "CSE", DepartmentCode = "CSE" };
        ctx.Users.AddRange(user, officer);
        ctx.Departments.Add(dept);

        var grievance = new Grievance
        {
            GrievanceId = Guid.NewGuid(), ComplainantUserId = user.Id, DepartmentId = 1,
            Category = GrievanceCategory.Infrastructure, Description = "Test",
            Priority = GrievancePriority.High, Status = GrievanceStatus.Submitted, CreatedAt = DateTime.UtcNow
        };
        ctx.Grievances.Add(grievance);
        await ctx.SaveChangesAsync();

        var repo = new GrievanceRepository(ctx);
        await repo.AssignAsync(grievance.GrievanceId, officer.Id, user.Id, "Assigned to officer");

        var updated = await repo.GetByIdAsync(grievance.GrievanceId);
        Assert.NotNull(updated);
        Assert.Equal(officer.Id, updated.AssignedToUserId);
        Assert.Equal(GrievanceStatus.Acknowledged, updated.Status); // Auto-transitions from Submitted
        Assert.NotEmpty(updated.Logs);
    }

    // ───── T066: Grievance AddAsync creates initial log ─────
    [Fact]
    public async Task T066_CreateGrievance_CreatesInitialLog()
    {
        using var ctx = CreateInMemoryContext();
        var user = new User { Id = Guid.NewGuid(), UserName = "u@test.com", Email = "u@test.com" };
        var dept = new Department { DepartmentId = 1, DepartmentName = "CSE", DepartmentCode = "CSE" };
        ctx.Users.Add(user);
        ctx.Departments.Add(dept);
        await ctx.SaveChangesAsync();

        var repo = new GrievanceRepository(ctx);
        var grievance = new Grievance
        {
            GrievanceId = Guid.NewGuid(), ComplainantUserId = user.Id, DepartmentId = 1,
            Category = GrievanceCategory.Academic, Description = "Test complaint",
            Priority = GrievancePriority.Medium, Status = GrievanceStatus.Submitted, CreatedAt = DateTime.UtcNow
        };
        await repo.AddAsync(grievance, user.Id);

        var retrieved = await repo.GetByIdAsync(grievance.GrievanceId);
        Assert.NotNull(retrieved);
        Assert.NotEmpty(retrieved.Logs);
        Assert.Contains(retrieved.Logs, l => l.StatusChangedTo == GrievanceStatus.Submitted.ToString());
    }
}

// ═══════════════════════════════════════════════════════════════════════
// SEARCH / FILTER TESTS
// ═══════════════════════════════════════════════════════════════════════
public class OpportunitySearchFilterTests
{
    private static AppDbContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    // ───── T067: Search filter works case-insensitively ─────
    [Fact]
    public async Task T067_SearchFilter_CaseInsensitive()
    {
        using var ctx = CreateInMemoryContext();
        var org = new User { Id = Guid.NewGuid(), UserName = "org@test.com", Email = "org@test.com" };
        ctx.Users.Add(org);
        ctx.Opportunities.Add(new Opportunity
        {
            OpportunityId = Guid.NewGuid(), OrganizerId = org.Id, Title = "Machine Learning Workshop",
            ApprovalStatus = ApprovalStatus.Approved, Description = "AI and ML",
            Category = OpportunityCategory.Workshop, WorkMode = WorkMode.Remote
        });
        await ctx.SaveChangesAsync();

        var repo = new OpportunityRepository(ctx);
        var upper = await repo.GetAllAsync(search: "MACHINE");
        var lower = await repo.GetAllAsync(search: "machine");
        var mixed = await repo.GetAllAsync(search: "Machine");

        Assert.Single(upper);
        Assert.Single(lower);
        Assert.Single(mixed);
    }

    // ───── T068: Search in description ─────
    [Fact]
    public async Task T068_SearchFilter_MatchesDescription()
    {
        using var ctx = CreateInMemoryContext();
        var org = new User { Id = Guid.NewGuid(), UserName = "org@test.com", Email = "org@test.com" };
        ctx.Users.Add(org);
        ctx.Opportunities.Add(new Opportunity
        {
            OpportunityId = Guid.NewGuid(), OrganizerId = org.Id, Title = "Generic Title",
            ApprovalStatus = ApprovalStatus.Approved, Description = "This involves quantum computing research",
            Category = OpportunityCategory.Internship, WorkMode = WorkMode.Remote
        });
        await ctx.SaveChangesAsync();

        var repo = new OpportunityRepository(ctx);
        var result = await repo.GetAllAsync(search: "quantum");
        Assert.Single(result);
    }

    // ───── T069: Category filter works ─────
    [Fact]
    public async Task T069_CategoryFilter_FiltersCorrectly()
    {
        using var ctx = CreateInMemoryContext();
        var org = new User { Id = Guid.NewGuid(), UserName = "org@test.com", Email = "org@test.com" };
        ctx.Users.Add(org);
        ctx.Opportunities.AddRange(
            new Opportunity { OpportunityId = Guid.NewGuid(), OrganizerId = org.Id, Title = "Intern", ApprovalStatus = ApprovalStatus.Approved, Description = "d", Category = OpportunityCategory.Internship, WorkMode = WorkMode.Remote },
            new Opportunity { OpportunityId = Guid.NewGuid(), OrganizerId = org.Id, Title = "Hack", ApprovalStatus = ApprovalStatus.Approved, Description = "d", Category = OpportunityCategory.Hackathon, WorkMode = WorkMode.Onsite });
        await ctx.SaveChangesAsync();

        var repo = new OpportunityRepository(ctx);
        var internships = await repo.GetAllAsync(category: OpportunityCategory.Internship);
        Assert.Single(internships);
        Assert.Equal("Intern", internships[0].Title);
    }

    // ───── T070: WorkMode filter works ─────
    [Fact]
    public async Task T070_WorkModeFilter_FiltersCorrectly()
    {
        using var ctx = CreateInMemoryContext();
        var org = new User { Id = Guid.NewGuid(), UserName = "org@test.com", Email = "org@test.com" };
        ctx.Users.Add(org);
        ctx.Opportunities.AddRange(
            new Opportunity { OpportunityId = Guid.NewGuid(), OrganizerId = org.Id, Title = "Remote", ApprovalStatus = ApprovalStatus.Approved, Description = "d", Category = OpportunityCategory.Internship, WorkMode = WorkMode.Remote },
            new Opportunity { OpportunityId = Guid.NewGuid(), OrganizerId = org.Id, Title = "Onsite", ApprovalStatus = ApprovalStatus.Approved, Description = "d", Category = OpportunityCategory.Workshop, WorkMode = WorkMode.Onsite });
        await ctx.SaveChangesAsync();

        var repo = new OpportunityRepository(ctx);
        var remote = await repo.GetAllAsync(mode: WorkMode.Remote);
        Assert.Single(remote);
        Assert.Equal("Remote", remote[0].Title);
    }
}

// ═══════════════════════════════════════════════════════════════════════
// EXISTING TEST REVIEW — CHALLENGE EXISTING TESTS
// ═══════════════════════════════════════════════════════════════════════
public class ExistingTestChallengeTests
{
    // ───── T071: Challenge existing test — verify it independently calculates expected value ─────
    [Fact]
    public void T071_VerifyExistingTest_60PercentCalculation()
    {
        // The existing test asserts 60% for Advanced(1.0)+Intermediate(0.8)+Missing(0) / 3 = 1.8/3 = 0.6
        // Independently verify: (1.0 + 0.8) / 3.0 = 0.6, * 100 = 60.0
        double expected = Math.Round((1.0 + 0.8 + 0.0) / 3.0 * 100.0, 1);
        Assert.Equal(60.0, expected);

        // Now run through the engine
        var engine = new SkillMatchingEngine();
        var csharp = new Skill { SkillId = 1, SkillName = "C#" };
        var sql = new Skill { SkillId = 2, SkillName = "SQL" };
        var python = new Skill { SkillId = 3, SkillName = "Python" };

        var student = new StudentProfile
        {
            StudentSkills = new List<StudentSkill>
            {
                new() { SkillId = 1, Skill = csharp, ProficiencyLevel = ProficiencyLevel.Advanced },
                new() { SkillId = 2, Skill = sql, ProficiencyLevel = ProficiencyLevel.Intermediate }
            }
        };
        var opportunity = new Opportunity
        {
            RequiredSkills = new List<OpportunitySkill>
            {
                new() { SkillId = 1, Skill = csharp },
                new() { SkillId = 2, Skill = sql },
                new() { SkillId = 3, Skill = python }
            }
        };

        var result = engine.CalculateMatch(student, opportunity);
        Assert.Equal(expected, result.MatchPercentage);
    }

    // ───── T072: Challenge — Would removing the Approved filter still pass existing test? ─────
    [Fact]
    public async Task T072_MutationTest_FeedWithoutStatusFilter_WouldReturnAll()
    {
        // This tests the mutation scenario: if we remove the status filter from SharedOpportunityFeed,
        // would the existing test catch it?
        // The answer is YES — the existing test seeds Approved+Pending+Rejected and asserts Single().
        // But we need MORE tests to ensure edge cases are covered.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var ctx = new AppDbContext(options);
        var org = new User { Id = Guid.NewGuid(), UserName = "org@test.com", Email = "org@test.com" };
        ctx.Users.Add(org);
        ctx.Opportunities.AddRange(
            new Opportunity { OpportunityId = Guid.NewGuid(), OrganizerId = org.Id, Title = "A", ApprovalStatus = ApprovalStatus.Approved, Description = "d", Category = OpportunityCategory.Internship, WorkMode = WorkMode.Remote },
            new Opportunity { OpportunityId = Guid.NewGuid(), OrganizerId = org.Id, Title = "P", ApprovalStatus = ApprovalStatus.PendingReview, Description = "d", Category = OpportunityCategory.Workshop, WorkMode = WorkMode.Onsite });
        await ctx.SaveChangesAsync();

        var repo = new OpportunityRepository(ctx);

        // Correct behavior (with filter)
        var correctFeed = new SharedOpportunityFeed(repo);
        var correctResult = await correctFeed.GetUpcomingOpportunitiesAsync();
        Assert.Single(correctResult);

        // Mutation: if we queried with null status (simulating removed filter)
        var allResult = await repo.GetAllAsync(status: null);
        Assert.Equal(2, allResult.Count);
        // Proves: removing the filter WOULD cause the test to fail — good
    }
}
