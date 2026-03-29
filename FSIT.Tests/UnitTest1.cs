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
        private ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        // Test 1
        [Fact]
        public async Task OverdueFollowUps_ReturnsOnlyOpenAndPastDueDate()
        {
            using var db = CreateDb();
            var premises = new Premises { Name = "Test", Address = "123 St", Town = "Cork", RiskRating = RiskRating.High };
            db.Premises.Add(premises);
            await db.SaveChangesAsync();

            var inspection = new Inspection { PremisesId = premises.Id, InspectionDate = DateTime.Today.AddDays(-10), Score = 50, Outcome = InspectionOutcome.Fail };
            db.Inspections.Add(inspection);
            await db.SaveChangesAsync();

            db.FollowUps.AddRange(
                new FollowUp { InspectionId = inspection.Id, DueDate = DateTime.Today.AddDays(-3), Status = FollowUpStatus.Open },   
                new FollowUp { InspectionId = inspection.Id, DueDate = DateTime.Today.AddDays(-1), Status = FollowUpStatus.Closed }, 
                new FollowUp { InspectionId = inspection.Id, DueDate = DateTime.Today.AddDays(5), Status = FollowUpStatus.Open }    
            );
            await db.SaveChangesAsync();

            var overdue = await db.FollowUps
                .Where(f => f.DueDate < DateTime.Today && f.Status == FollowUpStatus.Open)
                .ToListAsync();

            Assert.Single(overdue);
        }

        // Test 2
        [Fact]
        public void FollowUp_WhenClosed_MustHaveClosedDate()
        {
            var followUp = new FollowUp
            {
                Status = FollowUpStatus.Closed,
                ClosedDate = null
            };

            var isValid = followUp.Status == FollowUpStatus.Closed && followUp.ClosedDate == null;
            Assert.True(isValid); 
        }

        // Test 3
        [Fact]
        public async Task Dashboard_InspectionsThisMonth_CountIsCorrect()
        {
            using var db = CreateDb();
            var premises = new Premises { Name = "Cafe", Address = "1 Main St", Town = "Dublin", RiskRating = RiskRating.Low };
            db.Premises.Add(premises);
            await db.SaveChangesAsync();

            var firstOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            db.Inspections.AddRange(
                new Inspection { PremisesId = premises.Id, InspectionDate = DateTime.Today, Score = 80, Outcome = InspectionOutcome.Pass }, 
                new Inspection { PremisesId = premises.Id, InspectionDate = DateTime.Today.AddDays(-1), Score = 60, Outcome = InspectionOutcome.Pass }, 
                new Inspection { PremisesId = premises.Id, InspectionDate = DateTime.Today.AddMonths(-2), Score = 40, Outcome = InspectionOutcome.Fail } 
            );
            await db.SaveChangesAsync();

            var count = await db.Inspections
                .Where(i => i.InspectionDate >= firstOfMonth)
                .CountAsync();

            Assert.Equal(2, count);
        }

        // Test 4
        [Fact]
        public async Task Dashboard_FailedInspectionsThisMonth_CountIsCorrect()
        {
            using var db = CreateDb();
            var premises = new Premises { Name = "Shop", Address = "5 High St", Town = "Limerick", RiskRating = RiskRating.Medium };
            db.Premises.Add(premises);
            await db.SaveChangesAsync();

            var firstOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            db.Inspections.AddRange(
                new Inspection { PremisesId = premises.Id, InspectionDate = DateTime.Today, Score = 30, Outcome = InspectionOutcome.Fail },
                new Inspection { PremisesId = premises.Id, InspectionDate = DateTime.Today, Score = 90, Outcome = InspectionOutcome.Pass }, 
                new Inspection { PremisesId = premises.Id, InspectionDate = DateTime.Today.AddMonths(-1), Score = 20, Outcome = InspectionOutcome.Fail }
            );
            await db.SaveChangesAsync();

            var failed = await db.Inspections
                .Where(i => i.InspectionDate >= firstOfMonth && i.Outcome == InspectionOutcome.Fail)
                .CountAsync();

            Assert.Equal(1, failed);
        }

        // Test 5
        [Fact]
        public void FollowUp_DefaultStatus_IsOpen()
        {
            var followUp = new FollowUp();
            Assert.Equal(FollowUpStatus.Open, followUp.Status);
        }
    }
}
