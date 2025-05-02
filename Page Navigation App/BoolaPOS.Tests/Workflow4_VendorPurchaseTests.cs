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
        private Product _testProduct;

        [TestInitialize]
        public void TestInitialize()
        {
            // Set up in-memory database for testing
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "JewelryShopVendorTestDb_" + Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);

            // Create test vendor
            _testVendor = new Supplier
            {
                Name = "Gold Supplies Ltd.",
                ContactPerson = "John Supplier",
                Mobile = "9988776655",
                Email = "supplier@goldsupplies.com",
                Address = "123 Supplier Street, Mumbai",
                GSTNumber = "27AABCS1234Z1Z5",
                IsActive = true
            };

            _dbContext.Suppliers.Add(_testVendor);
            _dbContext.SaveChanges();
            
            // Create test product (with zero initial stock)
            _testProduct = new Product
            {
                Name = "22K Gold Sheet",
                NetWeight = 0, // Will be incremented during purchase
                GrossWeight = 0,
                BasePrice = 5800.00m, // per gram price
                MakingCharges = 0,
                WastagePercentage = 0,
                CategoryID = 1, // Gold category
                SubcategoryID = 8, // Raw materials subcategory
                PurityLevel = "22K",
                StockQuantity = 0,
                FinalPrice = 5800.00m, // Same as base price for raw materials
                IsActive = true
            };

            _dbContext.Products.Add(_testProduct);
            _dbContext.SaveChanges();
        }

        [TestMethod]
        public async Task AddVendorWithGST_ShouldCreateNewVendor()
        {
            // Arrange
            var newVendor = new Supplier
            {
                Name = "Silver Palace Wholesale",
                ContactPerson = "Mary Vendor",
                Mobile = "8877665544",
                Email = "info@silverpalace.com",
                Address = "456 Silver Street, Delhi",
                GSTNumber = "07AABCS9876Z1Z5",
                IsActive = true
            };

            // Act
            _dbContext.Suppliers.Add(newVendor);
            await _dbContext.SaveChangesAsync();

            // Assert
            var savedVendor = await _dbContext.Suppliers.FindAsync(newVendor.SupplierID);
            Assert.IsNotNull(savedVendor);
            Assert.AreEqual("Silver Palace Wholesale", savedVendor.Name);
            Assert.AreEqual("07AABCS9876Z1Z5", savedVendor.GSTNumber);
            Assert.AreEqual("8877665544", savedVendor.Mobile);
        }

        [TestMethod]
        public async Task CreatePurchaseEntry_ShouldAddToInventory()
        {
            // Arrange
            decimal purchaseQuantity = 500.0m; // 500 grams of gold
            decimal purchaseRate = 5800.00m; // Rate per gram
            decimal totalAmount = purchaseQuantity * purchaseRate;
            
            var purchaseOrder = new Stock
            {
                SupplierID = _testVendor.SupplierID,
                ProductID = _testProduct.ProductID,
                PurchaseDate = DateTime.Now,
                QuantityPurchased = purchaseQuantity,
                PurchaseRate = purchaseRate,
                TotalAmount = totalAmount,
                InvoiceNumber = "GS-2025-0001",
                PaymentStatus = "Paid",
                Notes = "Bulk purchase of 22K gold"
            };

            // Act - Create purchase and update inventory
            _dbContext.Stocks.Add(purchaseOrder);
            
            // Update the product stock
            var product = await _dbContext.Products.FindAsync(_testProduct.ProductID);
            product.StockQuantity += purchaseQuantity;
            product.NetWeight = purchaseQuantity; // Set net weight for raw material
            product.GrossWeight = purchaseQuantity; // Same as net weight for raw materials
            
            // Record purchase in stock ledger
            var ledgerEntry = new StockLedger
            {
                ProductID = product.ProductID,
                TransactionDate = DateTime.Now,
                TransactionType = "Purchase",
                Quantity = purchaseQuantity,
                UnitPrice = purchaseRate,
                TotalAmount = totalAmount,
                ReferenceID = purchaseOrder.StockID.ToString(),
                Notes = $"Purchased from {_testVendor.Name}"
            };
            
            _dbContext.StockLedgers.Add(ledgerEntry);
            await _dbContext.SaveChangesAsync();

            // Assert
            var updatedProduct = await _dbContext.Products.FindAsync(_testProduct.ProductID);
            Assert.AreEqual(purchaseQuantity, updatedProduct.StockQuantity);
            Assert.AreEqual(purchaseQuantity, updatedProduct.NetWeight);
            
            var savedPurchase = await _dbContext.Stocks
                .Include(s => s.Supplier)
                .Include(s => s.Product)
                .FirstOrDefaultAsync(s => s.ProductID == _testProduct.ProductID);
                
            Assert.IsNotNull(savedPurchase);
            Assert.AreEqual(_testVendor.SupplierID, savedPurchase.SupplierID);
            Assert.AreEqual(purchaseQuantity, savedPurchase.QuantityPurchased);
            Assert.AreEqual(totalAmount, savedPurchase.TotalAmount);
            
            // Verify ledger entry
            var ledgerEntries = await _dbContext.StockLedgers
                .Where(l => l.ProductID == _testProduct.ProductID)
                .ToListAsync();
                
            Assert.AreEqual(1, ledgerEntries.Count);
            Assert.AreEqual("Purchase", ledgerEntries[0].TransactionType);
            Assert.AreEqual(purchaseQuantity, ledgerEntries[0].Quantity);
        }

        [TestMethod]
        public async Task CheckVendorPaymentHistory_ShouldShowPurchaseRecords()
        {
            // Arrange - Create multiple purchases from the same vendor
            var purchaseOrders = new List<Stock>
            {
                new Stock
                {
                    SupplierID = _testVendor.SupplierID,
                    ProductID = _testProduct.ProductID,
                    PurchaseDate = DateTime.Now.AddDays(-30),
                    QuantityPurchased = 200.0m,
                    PurchaseRate = 5700.00m,
                    TotalAmount = 200.0m * 5700.00m,
                    InvoiceNumber = "GS-2025-0001",
                    PaymentStatus = "Paid",
                    Notes = "Regular monthly purchase"
                },
                new Stock
                {
                    SupplierID = _testVendor.SupplierID,
                    ProductID = _testProduct.ProductID,
                    PurchaseDate = DateTime.Now.AddDays(-15),
                    QuantityPurchased = 300.0m,
                    PurchaseRate = 5750.00m,
                    TotalAmount = 300.0m * 5750.00m,
                    InvoiceNumber = "GS-2025-0002",
                    PaymentStatus = "Paid",
                    Notes = "Mid-month purchase"
                },
                new Stock
                {
                    SupplierID = _testVendor.SupplierID,
                    ProductID = _testProduct.ProductID,
                    PurchaseDate = DateTime.Now,
                    QuantityPurchased = 400.0m,
                    PurchaseRate = 5800.00m,
                    TotalAmount = 400.0m * 5800.00m,
                    InvoiceNumber = "GS-2025-0003",
                    PaymentStatus = "Pending",
                    Notes = "Latest purchase, payment pending"
                }
            };

            // Add purchases to database
            foreach (var purchase in purchaseOrders)
            {
                _dbContext.Stocks.Add(purchase);
                
                // Add ledger entries
                var ledgerEntry = new StockLedger
                {
                    ProductID = purchase.ProductID,
                    TransactionDate = purchase.PurchaseDate,
                    TransactionType = "Purchase",
                    Quantity = purchase.QuantityPurchased,
                    UnitPrice = purchase.PurchaseRate,
                    TotalAmount = purchase.TotalAmount,
                    ReferenceID = purchase.StockID.ToString(),
                    Notes = $"Purchased from {_testVendor.Name}"
                };
                
                _dbContext.StockLedgers.Add(ledgerEntry);
            }
            
            // Update product stock
            var product = await _dbContext.Products.FindAsync(_testProduct.ProductID);
            product.StockQuantity = purchaseOrders.Sum(p => p.QuantityPurchased);
            product.NetWeight = product.StockQuantity;
            product.GrossWeight = product.StockQuantity;
            
            await _dbContext.SaveChangesAsync();

            // Act - Get payment history for the vendor
            var vendorPurchaseHistory = await _dbContext.Stocks
                .Where(s => s.SupplierID == _testVendor.SupplierID)
                .OrderByDescending(s => s.PurchaseDate)
                .ToListAsync();
                
            // Calculate payment status
            decimal totalPaid = vendorPurchaseHistory
                .Where(p => p.PaymentStatus == "Paid")
                .Sum(p => p.TotalAmount);
                
            decimal totalPending = vendorPurchaseHistory
                .Where(p => p.PaymentStatus == "Pending")
                .Sum(p => p.TotalAmount);
                
            decimal totalPurchases = vendorPurchaseHistory
                .Sum(p => p.TotalAmount);

            // Assert
            Assert.AreEqual(3, vendorPurchaseHistory.Count);
            
            // Check most recent purchase is first
            Assert.AreEqual("GS-2025-0003", vendorPurchaseHistory[0].InvoiceNumber);
            Assert.AreEqual("Pending", vendorPurchaseHistory[0].PaymentStatus);
            
            // Verify total calculations
            decimal expectedTotalPaid = (200.0m * 5700.00m) + (300.0m * 5750.00m); // First two purchases
            decimal expectedTotalPending = 400.0m * 5800.00m; // Latest purchase
            
            Assert.AreEqual(expectedTotalPaid, totalPaid);
            Assert.AreEqual(expectedTotalPending, totalPending);
            Assert.AreEqual(expectedTotalPaid + expectedTotalPending, totalPurchases);
            
            // Check product stock is updated
            var updatedProduct = await _dbContext.Products.FindAsync(_testProduct.ProductID);
            decimal totalPurchased = 200.0m + 300.0m + 400.0m;
            Assert.AreEqual(totalPurchased, updatedProduct.StockQuantity);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}