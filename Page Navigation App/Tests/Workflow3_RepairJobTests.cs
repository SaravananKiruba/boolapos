using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using Page_Navigation_App.Services;
using Page_Navigation_App.ViewModel;

namespace Tests
{
    [TestClass]
    public class Workflow3_RepairJobTests
    {
        private AppDbContext _dbContext;
        private Customer _testCustomer;

        [TestInitialize]
        public void TestInitialize()
        {
            // Set up in-memory database for testing
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "JewelryShopRepairTestDb_" + Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);

            // Create test customer
            _testCustomer = new Customer
            {
                Name = "Repair Test Customer",
                Mobile = "9988776655",
                Email = "repairtest@example.com",
                Address = "Repair Test Address",
                CreditLimit = 25000,
                CustomerType = "Retail"
            };

            _dbContext.Customers.Add(_testCustomer);
            _dbContext.SaveChanges();
        }

        [TestMethod]
        public async Task CreateRepairJob_ShouldRegisterNewJob()
        {
            // Arrange
            var repairJob = new RepairJob
            {
                CustomerID = _testCustomer.CustomerID,
                ItemDescription = "Gold Ring with Diamond",
                ItemPhotoUrl = "repair_images/ring_123.jpg", // Path to photo
                WorkType = "Ring Resizing",
                EstimatedCost = 2500.00m,
                ReceiptDate = DateTime.Now,
                EstimatedDeliveryDate = DateTime.Now.AddDays(5),
                Status = "Pending",
                Notes = "Customer wants ring size changed from 7 to 9"
            };

            // Act
            _dbContext.RepairJobs.Add(repairJob);
            await _dbContext.SaveChangesAsync();

            // Assert
            var savedJob = await _dbContext.RepairJobs.FindAsync(repairJob.RepairJobID);
            Assert.IsNotNull(savedJob);
            Assert.AreEqual("Gold Ring with Diamond", savedJob.ItemDescription);
            Assert.AreEqual("Pending", savedJob.Status);
            Assert.AreEqual(_testCustomer.CustomerID, savedJob.CustomerID);
        }

        [TestMethod]
        public async Task UpdateRepairJobStatus_ShouldChangeStatus()
        {
            // Arrange
            var repairJob = new RepairJob
            {
                CustomerID = _testCustomer.CustomerID,
                ItemDescription = "Silver Bracelet with Broken Clasp",
                ItemPhotoUrl = "repair_images/bracelet_456.jpg",
                WorkType = "Clasp Repair",
                EstimatedCost = 1200.00m,
                ReceiptDate = DateTime.Now,
                EstimatedDeliveryDate = DateTime.Now.AddDays(3),
                Status = "Pending",
                Notes = "Replace with stronger clasp"
            };

            _dbContext.RepairJobs.Add(repairJob);
            await _dbContext.SaveChangesAsync();

            // Act - Update status to In Process
            var job = await _dbContext.RepairJobs.FindAsync(repairJob.RepairJobID);
            job.Status = "In Process";
            job.Notes += "\nWork started on " + DateTime.Now.ToString("yyyy-MM-dd");
            await _dbContext.SaveChangesAsync();

            // Assert
            var updatedJob = await _dbContext.RepairJobs.FindAsync(repairJob.RepairJobID);
            Assert.AreEqual("In Process", updatedJob.Status);
            Assert.IsTrue(updatedJob.Notes.Contains("Work started on"));

            // Act - Update status to Delivered
            updatedJob.Status = "Delivered";
            updatedJob.DeliveryDate = DateTime.Now.AddDays(2);
            updatedJob.FinalCost = 1100.00m; // Actual cost less than estimate
            updatedJob.PaymentMethod = "Cash";
            updatedJob.Notes += "\nDelivered to customer on " + DateTime.Now.AddDays(2).ToString("yyyy-MM-dd");
            await _dbContext.SaveChangesAsync();

            // Assert
            var completedJob = await _dbContext.RepairJobs.FindAsync(repairJob.RepairJobID);
            Assert.AreEqual("Delivered", completedJob.Status);
            Assert.IsNotNull(completedJob.DeliveryDate);
            Assert.AreEqual(1100.00m, completedJob.FinalCost);
            Assert.AreEqual("Cash", completedJob.PaymentMethod);
        }

        [TestMethod]
        public async Task SendNotification_WhenRepairCompleted_ShouldRecordNotification()
        {
            // Arrange
            var repairJob = new RepairJob
            {
                CustomerID = _testCustomer.CustomerID,
                ItemDescription = "Gold Chain Repair",
                WorkType = "Fixing Broken Links",
                EstimatedCost = 1800.00m,
                ReceiptDate = DateTime.Now.AddDays(-5),
                EstimatedDeliveryDate = DateTime.Now,
                Status = "In Process",
                Notes = "Reinforcing weak links"
            };

            _dbContext.RepairJobs.Add(repairJob);
            await _dbContext.SaveChangesAsync();

            // Act - Update repair status and send notification
            var job = await _dbContext.RepairJobs.FindAsync(repairJob.RepairJobID);
            job.Status = "Completed";
            job.CompletionDate = DateTime.Now;
            job.FinalCost = 1800.00m;

            // Create notification record
            var notification = new NotificationLog
            {
                CustomerID = _testCustomer.CustomerID,
                NotificationType = "SMS",
                NotificationContent = $"Your repair for {job.ItemDescription} is now complete. You can collect it from our store.",
                SentDate = DateTime.Now,
                Status = "Sent",
                ReferenceID = job.RepairJobID.ToString(),
                ReferenceType = "RepairJob"
            };

            _dbContext.NotificationLogs.Add(notification);
            await _dbContext.SaveChangesAsync();

            // Assert
            var updatedJob = await _dbContext.RepairJobs.FindAsync(repairJob.RepairJobID);
            Assert.AreEqual("Completed", updatedJob.Status);
            
            var sentNotification = await _dbContext.NotificationLogs
                .FirstOrDefaultAsync(n => n.ReferenceID == job.RepairJobID.ToString());
                
            Assert.IsNotNull(sentNotification);
            Assert.AreEqual("SMS", sentNotification.NotificationType);
            Assert.AreEqual("Sent", sentNotification.Status);
            Assert.AreEqual(_testCustomer.CustomerID, sentNotification.CustomerID);
        }

        [TestMethod]
        public async Task ViewRepairJobHistory_ForCustomer_ShouldReturnAllJobs()
        {
            // Arrange - Create multiple repair jobs for the same customer
            var jobs = new List<RepairJob>
            {
                new RepairJob
                {
                    CustomerID = _testCustomer.CustomerID,
                    ItemDescription = "Diamond Earrings Repair",
                    WorkType = "Replace Missing Stone",
                    EstimatedCost = 5000.00m,
                    ReceiptDate = DateTime.Now.AddMonths(-2),
                    DeliveryDate = DateTime.Now.AddMonths(-2).AddDays(7),
                    Status = "Delivered",
                    FinalCost = 4800.00m,
                    PaymentMethod = "Credit Card"
                },
                new RepairJob
                {
                    CustomerID = _testCustomer.CustomerID,
                    ItemDescription = "Gold Watch Repair",
                    WorkType = "Battery Replacement and Cleaning",
                    EstimatedCost = 1500.00m,
                    ReceiptDate = DateTime.Now.AddMonths(-1),
                    DeliveryDate = DateTime.Now.AddMonths(-1).AddDays(3),
                    Status = "Delivered",
                    FinalCost = 1500.00m,
                    PaymentMethod = "Cash"
                },
                new RepairJob
                {
                    CustomerID = _testCustomer.CustomerID,
                    ItemDescription = "Platinum Ring Engraving",
                    WorkType = "Custom Engraving",
                    EstimatedCost = 2000.00m,
                    ReceiptDate = DateTime.Now.AddDays(-3),
                    EstimatedDeliveryDate = DateTime.Now.AddDays(2),
                    Status = "In Process"
                }
            };

            foreach (var job in jobs)
            {
                _dbContext.RepairJobs.Add(job);
            }
            await _dbContext.SaveChangesAsync();

            // Act
            var customerRepairHistory = await _dbContext.RepairJobs
                .Where(r => r.CustomerID == _testCustomer.CustomerID)
                .OrderByDescending(r => r.ReceiptDate)
                .ToListAsync();

            // Assert
            Assert.AreEqual(3, customerRepairHistory.Count);
            Assert.AreEqual("Platinum Ring Engraving", customerRepairHistory[0].ItemDescription);
            Assert.AreEqual("In Process", customerRepairHistory[0].Status);
            Assert.AreEqual("Delivered", customerRepairHistory[1].Status);
            Assert.AreEqual("Delivered", customerRepairHistory[2].Status);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}