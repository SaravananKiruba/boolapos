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
    public class Workflow4_VendorPurchaseTests
    {
        private AppDbContext _dbContext;
        private Supplier _testVendor;

        [TestInitialize]
        public void TestInitialize()
        {
            // Set up in-memory database for testing
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "JewelryShopVendorTestDb_" + Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);

            // Create test vendor/supplier
            _testVendor = new Supplier
            {
                Name = "Gold Wholesalers Ltd.",
                ContactPerson = "John Smith",
                Phone = "9876543210",
                Email = "sales@goldwholesalers.com",
                Address = "123 Gold Street, Mumbai",
                GSTNumber = "22AAAAA0000A1Z5", // Valid format for India GST
                Notes = "Primary gold supplier"
            };

            _dbContext.Suppliers.Add(_testVendor);
            _dbContext.SaveChanges();
        }

        [TestMethod]
        public async Task AddVendorWithGST_ShouldCreateSupplier()
        {
            // Arrange
            var newVendor = new Supplier
            {
                Name = "Silver Wholesalers Pvt. Ltd.",
                ContactPerson = "Raj Mehta",
                Phone = "8765432109",
                Email = "raj@silverwholesalers.com",
                Address = "456 Silver Avenue, Delhi",
                GSTNumber = "07BBBBB0000B1Z6", // Valid format for India GST
                Notes = "Silver and platinum supplier"
            };

            // Act
            _dbContext.Suppliers.Add(newVendor);
            await _dbContext.SaveChangesAsync();

            // Assert
            var savedVendor = await _dbContext.Suppliers.FindAsync(newVendor.SupplierID);
            Assert.IsNotNull(savedVendor);
            Assert.AreEqual("Silver Wholesalers Pvt. Ltd.", savedVendor.Name);
            Assert.AreEqual("07BBBBB0000B1Z6", savedVendor.GSTNumber);
        }

        [TestMethod]
        public async Task CreatePurchaseEntry_ShouldRecordPurchaseAndUpdateInventory()
        {
            // Arrange - Create a stock item for purchase
            var stockItem = new Stock
            {
                MetalType = "Gold",
                Purity = "22K",
                Form = "Raw",
                Quantity = 100.0m, // Initial quantity in grams
                PurchaseDate = DateTime.Now.AddMonths(-1),
                PurchasePrice = 5000.00m, // Per gram
                SupplierID = _testVendor.SupplierID
            };

            _dbContext.Stocks.Add(stockItem);
            await _dbContext.SaveChangesAsync();

            // Act - Create new purchase
            var purchase = new Stock
            {
                MetalType = "Gold",
                Purity = "22K",
                Form = "Raw",
                Quantity = 200.0m, // Purchasing 200 grams
                PurchaseDate = DateTime.Now,
                PurchasePrice = 5200.00m, // Current price per gram
                SupplierID = _testVendor.SupplierID,
                InvoiceNumber = "INV-20250502-001",
                Notes = "Regular monthly gold purchase"
            };

            _dbContext.Stocks.Add(purchase);
            await _dbContext.SaveChangesAsync();

            // Assert - Check that inventory is updated
            var goldStock = await _dbContext.Stocks
                .Where(s => s.MetalType == "Gold" && s.Purity == "22K" && s.Form == "Raw")
                .ToListAsync();

            Assert.AreEqual(2, goldStock.Count);
            Assert.AreEqual(300.0m, goldStock.Sum(s => s.Quantity)); // 100g + 200g
        }

        [TestMethod]
        public async Task CheckVendorPaymentHistory_ShouldReturnAllPayments()
        {
            // Arrange - Create multiple purchases from the same vendor
            var purchases = new List<Stock>
            {
                new Stock
                {
                    MetalType = "Gold",
                    Purity = "24K",
                    Form = "Bar",
                    Quantity = 50.0m,
                    PurchaseDate = DateTime.Now.AddMonths(-3),
                    PurchasePrice = 5500.00m,
                    SupplierID = _testVendor.SupplierID,
                    InvoiceNumber = "INV-2025-001",
                    PaymentStatus = "Paid",
                    PaymentDate = DateTime.Now.AddMonths(-3)
                },
                new Stock
                {
                    MetalType = "Gold",
                    Purity = "22K",
                    Form = "Raw",
                    Quantity = 200.0m,
                    PurchaseDate = DateTime.Now.AddMonths(-2),
                    PurchasePrice = 5100.00m,
                    SupplierID = _testVendor.SupplierID,
                    InvoiceNumber = "INV-2025-015",
                    PaymentStatus = "Paid",
                    PaymentDate = DateTime.Now.AddMonths(-2)
                },
                new Stock
                {
                    MetalType = "Gold",
                    Purity = "18K",
                    Form = "Sheet",
                    Quantity = 150.0m,
                    PurchaseDate = DateTime.Now.AddDays(-15),
                    PurchasePrice = 4800.00m,
                    SupplierID = _testVendor.SupplierID,
                    InvoiceNumber = "INV-2025-042",
                    PaymentStatus = "Pending"
                }
            };

            foreach (var purchase in purchases)
            {
                _dbContext.Stocks.Add(purchase);
            }
            await _dbContext.SaveChangesAsync();

            // Also create a finance entry for tracking payments
            var financeEntries = new List<Finance>
            {
                new Finance
                {
                    TransactionDate = DateTime.Now.AddMonths(-3),
                    Amount = 50.0m * 5500.00m, // Quantity * Price
                    Type = "Expense",
                    Category = "Inventory Purchase",
                    Description = "24K Gold Bar purchase - INV-2025-001",
                    ReferenceID = "INV-2025-001",
                    PaymentMethod = "Bank Transfer"
                },
                new Finance
                {
                    TransactionDate = DateTime.Now.AddMonths(-2),
                    Amount = 200.0m * 5100.00m, // Quantity * Price
                    Type = "Expense",
                    Category = "Inventory Purchase",
                    Description = "22K Gold Raw purchase - INV-2025-015",
                    ReferenceID = "INV-2025-015",
                    PaymentMethod = "Check"
                }
            };

            foreach (var entry in financeEntries)
            {
                _dbContext.Finances.Add(entry);
            }
            await _dbContext.SaveChangesAsync();

            // Act
            var vendorPaymentHistory = await _dbContext.Stocks
                .Where(s => s.SupplierID == _testVendor.SupplierID)
                .OrderByDescending(s => s.PurchaseDate)
                .ToListAsync();

            var vendorFinances = await _dbContext.Finances
                .Where(f => f.Category == "Inventory Purchase" && f.Description.Contains("Gold"))
                .OrderByDescending(f => f.TransactionDate)
                .ToListAsync();

            // Assert
            Assert.AreEqual(3, vendorPaymentHistory.Count);
            Assert.AreEqual(2, vendorPaymentHistory.Count(p => p.PaymentStatus == "Paid"));
            Assert.AreEqual(1, vendorPaymentHistory.Count(p => p.PaymentStatus == "Pending"));
            
            Assert.AreEqual(2, vendorFinances.Count);
            Assert.AreEqual("Bank Transfer", vendorFinances[1].PaymentMethod);
            Assert.AreEqual("Check", vendorFinances[0].PaymentMethod);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}