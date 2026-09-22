using CampusConnect.Models;

namespace CampusConnect.Services;

public interface ISkillMatchingEngine
{
    MatchResult CalculateMatch(StudentProfile student, Opportunity opportunity);
}

public class MatchResult
{
    public double MatchPercentage { get; set; }
    public List<Skill> MatchingSkills { get; set; } = new List<Skill>();
    public List<Skill> MissingSkills { get; set; } = new List<Skill>();
}
