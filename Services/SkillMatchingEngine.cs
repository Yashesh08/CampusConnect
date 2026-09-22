using CampusConnect.Models;

namespace CampusConnect.Services;

public class SkillMatchingEngine : ISkillMatchingEngine
{
    public MatchResult CalculateMatch(StudentProfile student, Opportunity opportunity)
    {
        var result = new MatchResult();
        
        if (opportunity.RequiredSkills == null || !opportunity.RequiredSkills.Any())
        {
            // If no skills are required, it's technically a 100% match.
            result.MatchPercentage = 100.0;
            return result;
        }

        var studentSkillIds = student.StudentSkills?.Select(ss => ss.SkillId).ToHashSet() ?? new HashSet<int>();
        var requiredSkillsCount = opportunity.RequiredSkills.Count;
        int matchingCount = 0;

        foreach (var reqSkill in opportunity.RequiredSkills)
        {
            if (reqSkill.Skill == null) continue; // Safety check

            if (studentSkillIds.Contains(reqSkill.SkillId))
            {
                matchingCount++;
                result.MatchingSkills.Add(reqSkill.Skill);
            }
            else
            {
                result.MissingSkills.Add(reqSkill.Skill);
            }
        }

        result.MatchPercentage = Math.Round((double)matchingCount / requiredSkillsCount * 100.0, 1);
        return result;
    }
}
