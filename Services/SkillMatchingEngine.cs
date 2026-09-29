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

        var studentSkillsDict = student.StudentSkills?.ToDictionary(ss => ss.SkillId, ss => ss.ProficiencyLevel) 
                                ?? new Dictionary<int, CampusConnect.Models.Enums.ProficiencyLevel>();
        var requiredSkillsCount = opportunity.RequiredSkills.Count;
        double totalScore = 0;

        foreach (var reqSkill in opportunity.RequiredSkills)
        {
            if (reqSkill.Skill == null) continue; // Safety check

            if (studentSkillsDict.TryGetValue(reqSkill.SkillId, out var proficiency))
            {
                // Assign weight based on proficiency level
                double weight = proficiency switch
                {
                    CampusConnect.Models.Enums.ProficiencyLevel.Advanced => 1.0,
                    CampusConnect.Models.Enums.ProficiencyLevel.Intermediate => 0.8,
                    CampusConnect.Models.Enums.ProficiencyLevel.Beginner => 0.5,
                    _ => 0.5
                };
                
                totalScore += weight;
                result.MatchingSkills.Add(reqSkill.Skill);
            }
            else
            {
                result.MissingSkills.Add(reqSkill.Skill);
            }
        }

        // Match Percentage can be up to 100% based on cumulative weights
        result.MatchPercentage = Math.Round((totalScore / requiredSkillsCount) * 100.0, 1);
        return result;
    }
}
