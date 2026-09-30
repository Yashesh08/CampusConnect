using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CampusConnect.Models;
using CampusConnect.Models.Enums;
using CampusConnect.Services;
using CampusConnect.Repositories;
using Microsoft.EntityFrameworkCore;
using CampusConnect.Data;
using Xunit;

namespace CampusConnect.Tests
{
    public class SharedOpportunityFeedTests
    {
        [Fact]
        public async Task GetUpcomingOpportunitiesAsync_ReturnsOnlyApproved()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var context = new AppDbContext(options))
            {
                var repo = new OpportunityRepository(context);
                var feed = new SharedOpportunityFeed(repo);

                var organizer = new User { Id = Guid.NewGuid(), UserName = "test@test.com", Email = "test@test.com" };
                context.Users.Add(organizer);

                context.Opportunities.AddRange(
                    new Opportunity { OpportunityId = Guid.NewGuid(), Title = "Approved 1", ApprovalStatus = ApprovalStatus.Approved, OrganizerId = organizer.Id, Organizer = organizer, Capacity = 10 },
                    new Opportunity { OpportunityId = Guid.NewGuid(), Title = "Pending 1", ApprovalStatus = ApprovalStatus.PendingReview, OrganizerId = organizer.Id, Organizer = organizer, Capacity = 10 },
                    new Opportunity { OpportunityId = Guid.NewGuid(), Title = "Rejected 1", ApprovalStatus = ApprovalStatus.Rejected, OrganizerId = organizer.Id, Organizer = organizer, Capacity = 10 }
                );
                await context.SaveChangesAsync();

                // Act
                var result = await feed.GetUpcomingOpportunitiesAsync();

                // Assert
                Assert.Single(result);
                Assert.Equal("Approved 1", result.First().Title);
            }
        }
    }
}
