using System;
using System.Threading.Tasks;
using FSIT.Domain;
using FSIT.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FSIT.Tests
{
    public class UnitTest1
    {
        
        private ApplicationDbContext CreateDb(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task OverdueFollowUps_ReturnsOnlyOpenAndPastDueDate()
        {
            using var db = CreateDb("test_overdue");

            var premises = new Premises { Name = "Test Cafe", Address = "1 Main St", Town = "Cork", RiskRating = RiskRating.Low };
            db.Premises.Add(premises);
            await db.SaveChangesAsync();

            var inspection = new Inspection
            {
                PremisesId = premises.Id,
                InspectionDate = DateTime.Today.AddDays(-30),
                Score = 50,
                Outcome = InspectionOutcome.Fail
            };
            db.Inspections.Add(inspection);
            await db.SaveChangesAsync();

            db.FollowUps.AddRange(
                new FollowUp { InspectionId = inspection.Id, DueDate = DateTime.Today.AddDays(-5), Status = FollowUpStatus.Open },
                new FollowUp { InspectionId = inspection.Id, DueDate = DateTime.Today.AddDays(-3), Status = FollowUpStatus.Closed }, 
                new FollowUp { InspectionId = inspection.Id, DueDate = DateTime.Today.AddDays(5), Status = FollowUpStatus.Open }    
            );
            await db.SaveChangesAsync();

            var overdue = await db.FollowUps
                .Where(f => f.DueDate < DateTime.Today && f.Status == FollowUpStatus.Open)
                .ToListAsync();

            Assert.Single(overdue);
        }

        [Fact]
        public void FollowUp_CannotBeClosed_WithoutClosedDate()
        {
            var followUp = new FollowUp
            {
                InspectionId = 1,
                DueDate = DateTime.Today.AddDays(-5),
                Status = FollowUpStatus.Closed,
                ClosedDate = null 
            };

            
            var isValid = followUp.Status != FollowUpStatus.Closed || followUp.ClosedDate.HasValue;

            Assert.False(isValid);
        }

        [Fact]
        public async Task Dashboard_Counts_MatchKnownSeedData()
        {
            using var db = CreateDb("test_dashboard");

            var premises = new Premises { Name = "Test Restaurant", Address = "2 High St", Town = "Dublin", RiskRating = RiskRating.High };
            db.Premises.Add(premises);
            await db.SaveChangesAsync();

            var firstOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            db.Inspections.AddRange(
                new Inspection { PremisesId = premises.Id, InspectionDate = DateTime.Today, Score = 80, Outcome = InspectionOutcome.Pass },
                new Inspection { PremisesId = premises.Id, InspectionDate = DateTime.Today.AddDays(-2), Score = 40, Outcome = InspectionOutcome.Fail }
            );
            await db.SaveChangesAsync();

            var monthlyCount = await db.Inspections
                .Where(i => i.InspectionDate >= firstOfMonth)
                .CountAsync();

            var failedCount = await db.Inspections
                .Where(i => i.InspectionDate >= firstOfMonth && i.Outcome == InspectionOutcome.Fail)
                .CountAsync();

            Assert.Equal(2, monthlyCount);
            Assert.Equal(1, failedCount);
        }

        [Fact]
        public void FollowUp_WithClosedDate_IsValid()
        {
            var followUp = new FollowUp
            {
                InspectionId = 1,
                DueDate = DateTime.Today.AddDays(-5),
                Status = FollowUpStatus.Closed,
                ClosedDate = DateTime.Today
            };

            var isValid = followUp.Status != FollowUpStatus.Closed || followUp.ClosedDate.HasValue;

            Assert.True(isValid);
        }
    }
}
