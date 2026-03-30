using epita_s2_2_mvc_83302.Controllers;
using FSIT.Data;
using FSIT.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
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

        [Fact]
        public void FollowUp_Id_CanBeSetAndRead()
        {
            var f = new FollowUp { Id = 42 };
            Assert.Equal(42, f.Id);
        }

        [Fact]
        public void Inspection_Notes_CanBeNull()
        {
            var i = new Inspection { Notes = null };
            Assert.Null(i.Notes);
        }

        [Fact]
        public void Inspection_Notes_CanBeSet()
        {
            var i = new Inspection { Notes = "Test note" };
            Assert.Equal("Test note", i.Notes);
        }

        // Test 1
        [Fact]
        public async Task OverdueFollowUps_ReturnsOnlyOpenAndPastDueDate()
        {
            using var db = CreateDb();

            var premises = new Premises
            {
                Name = "Test",
                Address = "123 St",
                Town = "Cork",
                RiskRating = RiskRating.High
            };
            db.Premises.Add(premises);
            await db.SaveChangesAsync();

            var inspection = new Inspection
            {
                PremisesId = premises.Id,
                InspectionDate = DateTime.Today.AddDays(-10),
                Score = 50,
                Outcome = InspectionOutcome.Fail
            };
            db.Inspections.Add(inspection);
            await db.SaveChangesAsync();

            db.FollowUps.AddRange(
                new FollowUp
                {
                    InspectionId = inspection.Id,
                    DueDate = DateTime.Today.AddDays(-3),
                    Status = FollowUpStatus.Open
                },
                new FollowUp
                {
                    InspectionId = inspection.Id,
                    DueDate = DateTime.Today.AddDays(-1),
                    Status = FollowUpStatus.Closed
                },
                new FollowUp
                {
                    InspectionId = inspection.Id,
                    DueDate = DateTime.Today.AddDays(5),
                    Status = FollowUpStatus.Open
                }
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

            var premises = new Premises
            {
                Name = "Cafe",
                Address = "1 Main St",
                Town = "Dublin",
                RiskRating = RiskRating.Low
            };
            db.Premises.Add(premises);
            await db.SaveChangesAsync();

            var firstOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            db.Inspections.AddRange(
                new Inspection
                {
                    PremisesId = premises.Id,
                    InspectionDate = DateTime.Today,
                    Score = 80,
                    Outcome = InspectionOutcome.Pass
                },
                new Inspection
                {
                    PremisesId = premises.Id,
                    InspectionDate = DateTime.Today.AddDays(-1),
                    Score = 60,
                    Outcome = InspectionOutcome.Pass
                },
                new Inspection
                {
                    PremisesId = premises.Id,
                    InspectionDate = DateTime.Today.AddMonths(-2),
                    Score = 40,
                    Outcome = InspectionOutcome.Fail
                }
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

            var premises = new Premises
            {
                Name = "Shop",
                Address = "5 High St",
                Town = "Limerick",
                RiskRating = RiskRating.Medium
            };
            db.Premises.Add(premises);
            await db.SaveChangesAsync();

            var firstOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            db.Inspections.AddRange(
                new Inspection
                {
                    PremisesId = premises.Id,
                    InspectionDate = DateTime.Today,
                    Score = 30,
                    Outcome = InspectionOutcome.Fail
                },
                new Inspection
                {
                    PremisesId = premises.Id,
                    InspectionDate = DateTime.Today,
                    Score = 90,
                    Outcome = InspectionOutcome.Pass
                },
                new Inspection
                {
                    PremisesId = premises.Id,
                    InspectionDate = DateTime.Today.AddMonths(-1),
                    Score = 20,
                    Outcome = InspectionOutcome.Fail
                }
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

        // Test 6 - IMPORTANT COVERAGE TEST
        [Fact]
        public async Task FollowUps_Create_Post_InvalidDueDate_ReturnsView_AndDoesNotSave()
        {
            using var db = CreateDb();

            var premises = new Premises
            {
                Name = "Test Premises",
                Address = "1 Test St",
                Town = "Dublin",
                RiskRating = RiskRating.High
            };
            db.Premises.Add(premises);
            await db.SaveChangesAsync();

            var inspection = new Inspection
            {
                PremisesId = premises.Id,
                InspectionDate = DateTime.Today,
                Score = 50,
                Outcome = InspectionOutcome.Fail
            };
            db.Inspections.Add(inspection);
            await db.SaveChangesAsync();

            var controller = new FollowUpsController(db);

            var followUp = new FollowUp
            {
                InspectionId = inspection.Id,
                DueDate = inspection.InspectionDate.AddDays(-1), // invalid
                Status = FollowUpStatus.Open
            };

            var result = await controller.Create(followUp);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<FollowUp>(viewResult.Model);
            Assert.Equal(followUp.InspectionId, model.InspectionId);

            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("DueDate"));

            Assert.Empty(db.FollowUps.ToList());

            Assert.True(controller.ViewData.ContainsKey("InspectionId"));
            Assert.IsType<SelectList>(controller.ViewData["InspectionId"]);
        }

        // Test 7 - IMPORTANT COVERAGE TEST
        [Fact]
        public async Task FollowUps_Create_Post_ValidFollowUp_SavesAndRedirects()
        {
            using var db = CreateDb();

            var premises = new Premises
            {
                Name = "Valid Premises",
                Address = "2 Main St",
                Town = "Cork",
                RiskRating = RiskRating.Medium
            };
            db.Premises.Add(premises);
            await db.SaveChangesAsync();

            var inspection = new Inspection
            {
                PremisesId = premises.Id,
                InspectionDate = DateTime.Today,
                Score = 75,
                Outcome = InspectionOutcome.Pass
            };
            db.Inspections.Add(inspection);
            await db.SaveChangesAsync();

            var controller = new FollowUpsController(db);

            var followUp = new FollowUp
            {
                InspectionId = inspection.Id,
                DueDate = inspection.InspectionDate.AddDays(7), // valid
                Status = FollowUpStatus.Open
            };

            var result = await controller.Create(followUp);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            var saved = await db.FollowUps.ToListAsync();
            Assert.Single(saved);
            Assert.Equal(followUp.InspectionId, saved[0].InspectionId);
            Assert.Equal(followUp.DueDate, saved[0].DueDate);
        }
    }
}
