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
    public class Workflow5_DayStartCloseTests
    {
        private AppDbContext _dbContext;
        private Customer _testCustomer;
        private Product _testProduct;
        private decimal _initialCashBalance = 10000.00m;

        [TestInitialize]
        public void TestInitialize()
        {
            // Set up in-memory database for testing
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "JewelryShopDayOpsTestDb_" + Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);

            // Create test customer
            _testCustomer = new Customer
            {
                Name = "Day Operations Test Customer",
                Mobile = "7766554433",
                Email = "daytest@example.com",
                Address = "Day Test Address",
                CreditLimit = 100000,
                CustomerType = "Retail"
            };

            // Create test product
            _testProduct = new Product
            {
                Name = "Gold Earrings",
                Barcode = "GE001",
                NetWeight = 5.5m,
                GrossWeight = 6.0m,
                BasePrice = 5000.00m,
                MakingCharges = 3000.00m,
                WastagePercentage = 7.0m,
                CategoryID = 1,
                SubcategoryID = 2,
                PurityLevel = "22K",
                StockQuantity = 10,
                FinalPrice = 35000.00m
            };

            _dbContext.Customers.Add(_testCustomer);
            _dbContext.Products.Add(_testProduct);
            _dbContext.SaveChanges();

            // Create initial day start entry with opening balance
            var dayStart = new Finance
            {
                TransactionDate = DateTime.Now.Date,
                Amount = _initialCashBalance,
                Type = "System",
                Category = "Day Start",
                Description = "Opening cash balance",
                PaymentMethod = "Cash",
                RecordedBy = "System",
                Notes = "Day start operation"
            };

            _dbContext.Finances.Add(dayStart);
            _dbContext.SaveChanges();
        }

        [TestMethod]
        public async Task OpenDailyCashRegister_ShouldRecordOpeningBalance()
        {
            // Arrange - The initial setup is done in TestInitialize

            // Act
            var dayStartRecord = await _dbContext.Finances
                .Where(f => f.TransactionDate.Date == DateTime.Now.Date && f.Category == "Day Start")
                .FirstOrDefaultAsync();

            // Assert
            Assert.IsNotNull(dayStartRecord);
            Assert.AreEqual(_initialCashBalance, dayStartRecord.Amount);
            Assert.AreEqual("System", dayStartRecord.Type);
            Assert.AreEqual("Cash", dayStartRecord.PaymentMethod);
        }

        [TestMethod]
        public async Task RecordMultipleTransactions_ShouldUpdateFinancialRecords()
        {
            // Arrange - Create a sale
            var order = new Order
            {
                CustomerID = _testCustomer.CustomerID,
                OrderDate = DateTime.Now,
                Status = "Completed",
                PaymentMethod = "Cash",
                TotalItems = 1,
                TotalAmount = _testProduct.FinalPrice,
                CGST = _testProduct.FinalPrice * 0.015m,
                SGST = _testProduct.FinalPrice * 0.015m,
                GrandTotal = _testProduct.FinalPrice + (_testProduct.FinalPrice * 0.03m)
            };

            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync();

            var orderDetail = new OrderDetail
            {
                OrderID = order.OrderID,
                ProductID = _testProduct.ProductID,
                Quantity = 1,
                UnitPrice = _testProduct.FinalPrice,
                TotalAmount = _testProduct.FinalPrice,
                NetWeight = _testProduct.NetWeight,
                GrossWeight = _testProduct.GrossWeight,
                MetalRate = _testProduct.BasePrice,
                MakingCharges = _testProduct.MakingCharges,
                WastagePercentage = _testProduct.WastagePercentage
            };

            _dbContext.OrderDetails.Add(orderDetail);

            // Update inventory
            var product = await _dbContext.Products.FindAsync(_testProduct.ProductID);
            product.StockQuantity -= 1;

            // Record finance entry for the sale
            var saleTxn = new Finance
            {
                TransactionDate = DateTime.Now,
                Amount = order.GrandTotal,
                Type = "Income",
                Category = "Sales",
                Description = $"Sale of {_testProduct.Name} to {_testCustomer.Name}",
                PaymentMethod = "Cash",
                ReferenceID = order.OrderID.ToString(),
                RecordedBy = "Test User"
            };

            _dbContext.Finances.Add(saleTxn);

            // Record an expense transaction
            var expenseTxn = new Finance
            {
                TransactionDate = DateTime.Now,
                Amount = 2000.00m,
                Type = "Expense",
                Category = "Utilities",
                Description = "Electricity bill payment",
                PaymentMethod = "Cash",
                ReferenceID = "BILL-2025-05-001",
                RecordedBy = "Test User"
            };

            _dbContext.Finances.Add(expenseTxn);
            await _dbContext.SaveChangesAsync();

            // Act - Get today's transactions
            var dayTransactions = await _dbContext.Finances
                .Where(f => f.TransactionDate.Date == DateTime.Now.Date)
                .OrderBy(f => f.TransactionDate)
                .ToListAsync();

            // Assert
            Assert.AreEqual(3, dayTransactions.Count); // Opening balance + sale + expense
            Assert.AreEqual(1, dayTransactions.Count(t => t.Type == "Income"));
            Assert.AreEqual(1, dayTransactions.Count(t => t.Type == "Expense"));
            Assert.AreEqual(1, dayTransactions.Count(t => t.Type == "System" && t.Category == "Day Start"));

            // Calculate expected closing balance
            decimal expectedClosingBalance = _initialCashBalance + saleTxn.Amount - expenseTxn.Amount;
            decimal actualBalance = dayTransactions.Where(t => t.PaymentMethod == "Cash").Sum(t => 
                t.Type == "Income" || t.Type == "System" ? t.Amount : -t.Amount);
            
            Assert.AreEqual(expectedClosingBalance, actualBalance);
        }

        [TestMethod]
        public async Task CloseDayOperation_ShouldCalculateAndRecordClosingBalance()
        {
            // Arrange - Create some financial transactions
            var transactions = new List<Finance>
            {
                new Finance
                {
                    TransactionDate = DateTime.Now,
                    Amount = 50000.00m,
                    Type = "Income",
                    Category = "Sales",
                    Description = "Gold chain sale",
                    PaymentMethod = "Cash",
                    RecordedBy = "Test User"
                },
                new Finance
                {
                    TransactionDate = DateTime.Now,
                    Amount = 25000.00m,
                    Type = "Income",
                    Category = "Sales",
                    Description = "Silver items sale",
                    PaymentMethod = "Credit Card",
                    RecordedBy = "Test User"
                },
                new Finance
                {
                    TransactionDate = DateTime.Now,
                    Amount = 5000.00m,
                    Type = "Expense",
                    Category = "Supplies",
                    Description = "Packaging materials",
                    PaymentMethod = "Cash",
                    RecordedBy = "Test User"
                }
            };

            foreach (var txn in transactions)
            {
                _dbContext.Finances.Add(txn);
            }
            await _dbContext.SaveChangesAsync();

            // Act - Close the day
            // Calculate closing balance
            var dayTransactions = await _dbContext.Finances
                .Where(f => f.TransactionDate.Date == DateTime.Now.Date && f.PaymentMethod == "Cash")
                .ToListAsync();

            decimal closingBalance = _initialCashBalance; // Starting balance
            foreach (var txn in dayTransactions.Where(t => t.Category != "Day Start" && t.Category != "Day Close"))
            {
                if (txn.Type == "Income" || txn.Type == "System")
                    closingBalance += txn.Amount;
                else if (txn.Type == "Expense")
                    closingBalance -= txn.Amount;
            }

            // Record day close entry
            var dayClose = new Finance
            {
                TransactionDate = DateTime.Now,
                Amount = closingBalance,
                Type = "System",
                Category = "Day Close",
                Description = "Closing cash balance",
                PaymentMethod = "Cash",
                RecordedBy = "System",
                Notes = "Day end operation"
            };

            _dbContext.Finances.Add(dayClose);
            await _dbContext.SaveChangesAsync();

            // Assert
            var dayCloseRecord = await _dbContext.Finances
                .Where(f => f.Category == "Day Close" && f.TransactionDate.Date == DateTime.Now.Date)
                .FirstOrDefaultAsync();

            Assert.IsNotNull(dayCloseRecord);
            Assert.AreEqual(closingBalance, dayCloseRecord.Amount);
            
            // Expected closing balance calculation
            decimal expectedClosingBalance = _initialCashBalance + 50000.00m - 5000.00m; // Opening + cash sales - cash expenses
            Assert.AreEqual(expectedClosingBalance, closingBalance);
            
            // Non-cash transactions shouldn't affect cash balance
            Assert.AreEqual(1, dayTransactions.Count(t => t.PaymentMethod == "Cash" && t.Type == "Income"));
        }

        [TestMethod]
        public async Task PrintCashRegisterSummary_ShouldProvideAccurateReport()
        {
            // Arrange - Create various transactions
            var transactions = new List<Finance>
            {
                new Finance
                {
                    TransactionDate = DateTime.Now,
                    Amount = 75000.00m,
                    Type = "Income",
                    Category = "Sales",
                    Description = "Gold necklace sale",
                    PaymentMethod = "Cash",
                    RecordedBy = "Test User"
                },
                new Finance
                {
                    TransactionDate = DateTime.Now,
                    Amount = 45000.00m,
                    Type = "Income",
                    Category = "Sales",
                    Description = "Diamond ring sale",
                    PaymentMethod = "Credit Card",
                    RecordedBy = "Test User"
                },
                new Finance
                {
                    TransactionDate = DateTime.Now,
                    Amount = 30000.00m,
                    Type = "Income",
                    Category = "Sales",
                    Description = "Silver gift items",
                    PaymentMethod = "UPI",
                    RecordedBy = "Test User"
                },
                new Finance
                {
                    TransactionDate = DateTime.Now,
                    Amount = 8000.00m,
                    Type = "Expense",
                    Category = "Rent",
                    Description = "Shop rent payment",
                    PaymentMethod = "Check",
                    RecordedBy = "Test User"
                },
                new Finance
                {
                    TransactionDate = DateTime.Now,
                    Amount = 3000.00m,
                    Type = "Expense",
                    Category = "Utilities",
                    Description = "Electricity bill",
                    PaymentMethod = "Cash",
                    RecordedBy = "Test User"
                }
            };

            foreach (var txn in transactions)
            {
                _dbContext.Finances.Add(txn);
            }
            await _dbContext.SaveChangesAsync();

            // Act - Generate summary report data
            var todayTransactions = await _dbContext.Finances
                .Where(f => f.TransactionDate.Date == DateTime.Now.Date)
                .ToListAsync();

            // Calculate summary metrics
            decimal totalCashSales = todayTransactions
                .Where(t => t.Type == "Income" && t.Category == "Sales" && t.PaymentMethod == "Cash")
                .Sum(t => t.Amount);

            decimal totalCardSales = todayTransactions
                .Where(t => t.Type == "Income" && t.Category == "Sales" && t.PaymentMethod == "Credit Card")
                .Sum(t => t.Amount);

            decimal totalDigitalSales = todayTransactions
                .Where(t => t.Type == "Income" && t.Category == "Sales" && t.PaymentMethod == "UPI")
                .Sum(t => t.Amount);

            decimal totalSales = todayTransactions
                .Where(t => t.Type == "Income" && t.Category == "Sales")
                .Sum(t => t.Amount);

            decimal totalExpenses = todayTransactions
                .Where(t => t.Type == "Expense")
                .Sum(t => t.Amount);

            decimal cashInHand = _initialCashBalance + 
                todayTransactions.Where(t => t.PaymentMethod == "Cash" && t.Type == "Income").Sum(t => t.Amount) - 
                todayTransactions.Where(t => t.PaymentMethod == "Cash" && t.Type == "Expense").Sum(t => t.Amount);

            // Assert
            Assert.AreEqual(75000.00m, totalCashSales);
            Assert.AreEqual(45000.00m, totalCardSales);
            Assert.AreEqual(30000.00m, totalDigitalSales);
            Assert.AreEqual(150000.00m, totalSales);
            Assert.AreEqual(11000.00m, totalExpenses);
            Assert.AreEqual(_initialCashBalance + 75000.00m - 3000.00m, cashInHand); // Opening + cash sales - cash expenses
            
            // Verify transaction counts
            Assert.AreEqual(3, todayTransactions.Count(t => t.Type == "Income" && t.Category == "Sales"));
            Assert.AreEqual(2, todayTransactions.Count(t => t.Type == "Expense"));
            Assert.AreEqual(1, todayTransactions.Count(t => t.Type == "System" && t.Category == "Day Start"));
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}