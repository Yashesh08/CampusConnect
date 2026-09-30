using System;
using System.Collections.Generic;
using CampusConnect.Models;
using CampusConnect.Models.Enums;
using CampusConnect.Services;
using Xunit;

namespace CampusConnect.Tests
{
    public class SkillMatchingEngineTests
    {
        [Fact]
        public void CalculateMatch_Returns100_WhenNoRequiredSkills()
        {
            // Arrange
            var engine = new SkillMatchingEngine();
            var student = new StudentProfile();
            var opportunity = new Opportunity { RequiredSkills = new List<OpportunitySkill>() };

            // Act
            var result = engine.CalculateMatch(student, opportunity);

            // Assert
            Assert.Equal(100.0, result.MatchPercentage);
            Assert.Empty(result.MissingSkills);
            Assert.Empty(result.MatchingSkills);
        }

        [Fact]
        public void CalculateMatch_CalculatesCorrectPercentage_BasedOnProficiency()
        {
            // Arrange
            var engine = new SkillMatchingEngine();
            var csharp = new Skill { SkillId = 1, SkillName = "C#" };
            var sql = new Skill { SkillId = 2, SkillName = "SQL" };
            var python = new Skill { SkillId = 3, SkillName = "Python" };

            var student = new StudentProfile
            {
                StudentSkills = new List<StudentSkill>
                {
                    new StudentSkill { SkillId = 1, Skill = csharp, ProficiencyLevel = ProficiencyLevel.Advanced }, // weight 1.0
                    new StudentSkill { SkillId = 2, Skill = sql, ProficiencyLevel = ProficiencyLevel.Intermediate } // weight 0.8
                    // Missing Python
                }
            };

            var opportunity = new Opportunity
            {
                RequiredSkills = new List<OpportunitySkill>
                {
                    new OpportunitySkill { SkillId = 1, Skill = csharp },
                    new OpportunitySkill { SkillId = 2, Skill = sql },
                    new OpportunitySkill { SkillId = 3, Skill = python }
                }
            };

            // Act
            var result = engine.CalculateMatch(student, opportunity);

            // Assert
            // 3 required skills. Score = (1.0 + 0.8 + 0.0) / 3 = 1.8 / 3 = 0.6 = 60.0%
            Assert.Equal(60.0, result.MatchPercentage);
            Assert.Contains(python, result.MissingSkills);
            Assert.Contains(csharp, result.MatchingSkills);
            Assert.Contains(sql, result.MatchingSkills);
        }
    }
}
