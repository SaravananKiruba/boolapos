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
    public class Workflow6_DashboardReportsTests
    {
        private AppDbContext _dbContext;
        private List<Product> _products;
        private List<Customer> _customers;
        private List<Order> _orders;
        private DateTime _startDate;
        private DateTime _endDate;

        [TestInitialize]
        public void TestInitialize()
        {
            // Set up in-memory database for testing
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "JewelryShopDashboardTestDb_" + Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);

            // Set date range for testing
            _startDate = new DateTime(2025, 4, 1);
            _endDate = new DateTime(2025, 5, 2);

            // Create test customers
            _customers = new List<Customer>
            {
                new Customer
                {
                    Name = "Report Test Customer 1",
                    Mobile = "9988776655",
                    Email = "report1@example.com",
                    Address = "Report Test Address 1",
                    CreditLimit = 100000,
                    CustomerType = "Retail"
                },
                new Customer
                {
                    Name = "Report Test Customer 2",
                    Mobile = "8877665544",
                    Email = "report2@example.com",
                    Address = "Report Test Address 2",
                    CreditLimit = 150000,
                    CustomerType = "Wholesale",
                    GSTNumber = "27AADCB2230M1Z3"
                }
            };

            _dbContext.Customers.AddRange(_customers);
            _dbContext.SaveChanges();

            // Create test products
            _products = new List<Product>
            {
                new Product
                {
                    Name = "Gold Ring",
                    Barcode = "GR002",
                    NetWeight = 8.5m,
                    GrossWeight = 9.0m,
                    BasePrice = 5200.00m,
                    MakingCharges = 4000.00m,
                    WastagePercentage = 7.0m,
                    CategoryID = 1,
                    SubcategoryID = 1,
                    PurityLevel = "22K",
                    StockQuantity = 5,
                    FinalPrice = 50000.00m,
                    HSNCode = "7113"
                },
                new Product
                {
                    Name = "Silver Anklet",
                    Barcode = "SA001",
                    NetWeight = 35.0m,
                    GrossWeight = 36.0m,
                    BasePrice = 75.00m,
                    MakingCharges = 1500.00m,
                    WastagePercentage = 5.0m,
                    CategoryID = 2,
                    SubcategoryID = 7,
                    PurityLevel = "92.5%",
                    StockQuantity = 8,
                    FinalPrice = 4500.00m,
                    HSNCode = "7113"
                }
            };

            _dbContext.Products.AddRange(_products);
            _dbContext.SaveChanges();

            // Create test orders with various dates
            _orders = new List<Order>();
            
            // Order 1 - First month
            var order1 = new Order
            {
                CustomerID = _customers[0].CustomerID,
                OrderDate = new DateTime(2025, 4, 5),
                Status = "Completed",
                PaymentMethod = "Cash",
                InvoiceNumber = "INV-2025-0101"
            };
            _dbContext.Orders.Add(order1);
            _dbContext.SaveChanges();
            
            var orderDetail1 = new OrderDetail
            {
                OrderID = order1.OrderID,
                ProductID = _products[0].ProductID,
                Quantity = 1,
                UnitPrice = _products[0].FinalPrice,
                TotalAmount = _products[0].FinalPrice,
                NetWeight = _products[0].NetWeight,
                GrossWeight = _products[0].GrossWeight,
                MetalRate = _products[0].BasePrice,
                MakingCharges = _products[0].MakingCharges,
                WastagePercentage = _products[0].WastagePercentage,
                HSNCode = _products[0].HSNCode
            };
            _dbContext.OrderDetails.Add(orderDetail1);
            
            // Order 2 - Current month
            var order2 = new Order
            {
                CustomerID = _customers[1].CustomerID,
                OrderDate = new DateTime(2025, 5, 1),
                Status = "Completed",
                PaymentMethod = "Credit Card",
                InvoiceNumber = "INV-2025-0202"
            };
            _dbContext.Orders.Add(order2);
            _dbContext.SaveChanges();
            
            var orderDetail2A = new OrderDetail
            {
                OrderID = order2.OrderID,
                ProductID = _products[0].ProductID,
                Quantity = 1,
                UnitPrice = _products[0].FinalPrice,
                TotalAmount = _products[0].FinalPrice,
                NetWeight = _products[0].NetWeight,
                GrossWeight = _products[0].GrossWeight,
                MetalRate = _products[0].BasePrice,
                MakingCharges = _products[0].MakingCharges,
                WastagePercentage = _products[0].WastagePercentage,
                HSNCode = _products[0].HSNCode
            };
            
            var orderDetail2B = new OrderDetail
            {
                OrderID = order2.OrderID,
                ProductID = _products[1].ProductID,
                Quantity = 2,
                UnitPrice = _products[1].FinalPrice,
                TotalAmount = _products[1].FinalPrice * 2,
                NetWeight = _products[1].NetWeight,
                GrossWeight = _products[1].GrossWeight,
                MetalRate = _products[1].BasePrice,
                MakingCharges = _products[1].MakingCharges,
                WastagePercentage = _products[1].WastagePercentage,
                HSNCode = _products[1].HSNCode
            };
            
            _dbContext.OrderDetails.AddRange(orderDetail2A, orderDetail2B);
            
            // Calculate totals and tax
            order1.TotalItems = 1;
            order1.TotalAmount = orderDetail1.TotalAmount;
            order1.CGST = order1.TotalAmount * 0.015m;
            order1.SGST = order1.TotalAmount * 0.015m;
            order1.GrandTotal = order1.TotalAmount + order1.CGST + order1.SGST;
            
            order2.TotalItems = 2;
            order2.TotalAmount = orderDetail2A.TotalAmount + orderDetail2B.TotalAmount;
            order2.CGST = order2.TotalAmount * 0.015m;
            order2.SGST = order2.TotalAmount * 0.015m;
            order2.GrandTotal = order2.TotalAmount + order2.CGST + order2.SGST;
            
            _orders.Add(order1);
            _orders.Add(order2);
            
            _dbContext.SaveChanges();
            
            // Update product stock quantities
            var product1 = _dbContext.Products.Find(_products[0].ProductID);
            product1.StockQuantity -= 2; // Sold in both orders
            
            var product2 = _dbContext.Products.Find(_products[1].ProductID);
            product2.StockQuantity -= 2; // Sold in second order
            
            _dbContext.SaveChanges();
        }

        [TestMethod]
        public async Task ViewDashboard_ShouldShowCorrectTotals()
        {
            // Act - Calculate dashboard metrics
            var totalSales = await _dbContext.Orders
                .Where(o => o.OrderDate.Date >= _startDate.Date && o.OrderDate.Date <= _endDate.Date)
                .SumAsync(o => o.GrandTotal);
                
            var totalOrders = await _dbContext.Orders
                .Where(o => o.OrderDate.Date >= _startDate.Date && o.OrderDate.Date <= _endDate.Date)
                .CountAsync();
                
            var totalCustomers = await _dbContext.Customers.CountAsync();
            
            var totalProducts = await _dbContext.Products.CountAsync();
            
            var inventoryValue = await _dbContext.Products
                .SumAsync(p => p.FinalPrice * p.StockQuantity);

            // Assert
            // Two orders in our test data
            Assert.AreEqual(2, totalOrders);
            
            // Expected sales: order1.GrandTotal + order2.GrandTotal
            var expectedSales = _orders[0].GrandTotal + _orders[1].GrandTotal;
            Assert.AreEqual(expectedSales, totalSales);
            
            // We added two customers in our test
            Assert.AreEqual(2, totalCustomers);
            
            // We added two products in our test
            Assert.AreEqual(2, totalProducts);
            
            // Inventory calculation: (Product1.FinalPrice * (5-2)) + (Product2.FinalPrice * (8-2))
            var expectedInventoryValue = (_products[0].FinalPrice * 3) + (_products[1].FinalPrice * 6);
            Assert.AreEqual(expectedInventoryValue, inventoryValue);
        }

        [TestMethod]
        public async Task ExportSalesReport_ShouldIncludeAllSales()
        {
            // Act - Get sales data for reporting
            var salesReport = await _dbContext.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => o.OrderDate.Date >= _startDate.Date && o.OrderDate.Date <= _endDate.Date)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
                
            // For export, we'd format this into rows
            var reportRows = salesReport.Select(o => new
            {
                InvoiceNumber = o.InvoiceNumber,
                Date = o.OrderDate.ToString("yyyy-MM-dd"),
                CustomerName = o.Customer?.Name,
                TotalItems = o.TotalItems,
                SubTotal = o.TotalAmount,
                CGST = o.CGST,
                SGST = o.SGST,
                GrandTotal = o.GrandTotal,
                PaymentMethod = o.PaymentMethod,
                Status = o.Status
            }).ToList();

            // Assert
            Assert.AreEqual(2, reportRows.Count);
            
            // Check latest order first (order by desc)
            Assert.AreEqual("INV-2025-0202", reportRows[0].InvoiceNumber);
            Assert.AreEqual("Report Test Customer 2", reportRows[0].CustomerName);
            Assert.AreEqual("Credit Card", reportRows[0].PaymentMethod);
            
            Assert.AreEqual("INV-2025-0101", reportRows[1].InvoiceNumber);
            Assert.AreEqual("Report Test Customer 1", reportRows[1].CustomerName);
            Assert.AreEqual("Cash", reportRows[1].PaymentMethod);
            
            // Validate tax calculations in the report
            foreach (var row in reportRows)
            {
                // GST should be 3% (1.5% CGST + 1.5% SGST)
                Assert.AreEqual(row.SubTotal * 0.015m, row.CGST);
                Assert.AreEqual(row.SubTotal * 0.015m, row.SGST);
                Assert.AreEqual(row.SubTotal + row.CGST + row.SGST, row.GrandTotal);
            }
        }

        [TestMethod]
        public async Task ExportCustomerPurchaseHistory_ShouldShowCustomerOrders()
        {
            // Arrange - Select a customer for history report
            var customerId = _customers[1].CustomerID;
            
            // Act - Get customer purchase history
            var customerHistory = await _dbContext.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => o.CustomerID == customerId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
                
            var historyRows = customerHistory.SelectMany(o => o.OrderDetails.Select(od => new
            {
                InvoiceNumber = o.InvoiceNumber,
                Date = o.OrderDate.ToString("yyyy-MM-dd"),
                ProductName = od.Product?.Name,
                Quantity = od.Quantity,
                UnitPrice = od.UnitPrice,
                TotalAmount = od.TotalAmount,
                PaymentMethod = o.PaymentMethod
            })).ToList();

            // Assert
            Assert.AreEqual(1, customerHistory.Count); // One order for customer 2
            Assert.AreEqual(2, historyRows.Count); // Two items in that order
            
            Assert.AreEqual("INV-2025-0202", historyRows[0].InvoiceNumber);
            Assert.AreEqual("2025-05-01", historyRows[0].Date);
            
            // Should have both products
            var productNames = historyRows.Select(r => r.ProductName).ToList();
            CollectionAssert.Contains(productNames, "Gold Ring");
            CollectionAssert.Contains(productNames, "Silver Anklet");
        }

        [TestMethod]
        public async Task ExportGSTSummary_ShouldCalculateCorrectTaxes()
        {
            // Act - Get GST data for the date range
            var gstReport = await _dbContext.Orders
                .Where(o => o.OrderDate.Date >= _startDate.Date && o.OrderDate.Date <= _endDate.Date)
                .OrderBy(o => o.OrderDate)
                .ToListAsync();
                
            // Group by month for GST returns
            var monthlySummary = gstReport
                .GroupBy(o => new { Month = o.OrderDate.Month, Year = o.OrderDate.Year })
                .Select(g => new
                {
                    Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                    TaxableAmount = g.Sum(o => o.TotalAmount),
                    CGST = g.Sum(o => o.CGST),
                    SGST = g.Sum(o => o.SGST),
                    TotalTax = g.Sum(o => o.CGST + o.SGST),
                    TotalInvoiceValue = g.Sum(o => o.GrandTotal)
                })
                .ToList();

            // Assert
            Assert.AreEqual(2, monthlySummary.Count); // Two months in our test data
            
            // Verify each month's data
            var april = monthlySummary.FirstOrDefault(m => m.Period == "2025-04");
            var may = monthlySummary.FirstOrDefault(m => m.Period == "2025-05");
            
            Assert.IsNotNull(april);
            Assert.IsNotNull(may);
            
            // April has only order1
            Assert.AreEqual(_orders[0].TotalAmount, april.TaxableAmount);
            Assert.AreEqual(_orders[0].CGST, april.CGST);
            Assert.AreEqual(_orders[0].SGST, april.SGST);
            Assert.AreEqual(_orders[0].GrandTotal, april.TotalInvoiceValue);
            
            // May has only order2
            Assert.AreEqual(_orders[1].TotalAmount, may.TaxableAmount);
            Assert.AreEqual(_orders[1].CGST, may.CGST);
            Assert.AreEqual(_orders[1].SGST, may.SGST);
            Assert.AreEqual(_orders[1].GrandTotal, may.TotalInvoiceValue);
            
            // Total tax should be 3% of taxable amount
            Assert.AreEqual(april.TaxableAmount * 0.03m, april.TotalTax);
            Assert.AreEqual(may.TaxableAmount * 0.03m, may.TotalTax);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}