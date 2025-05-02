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
    public class Workflow7_CustomInvoiceTests
    {
        private AppDbContext _dbContext;
        private Customer _testCustomer;
        private Product _testProduct1;
        private Product _testProduct2;
        private Order _testOrder;
        private BusinessInfo _businessInfo;

        [TestInitialize]
        public void TestInitialize()
        {
            // Set up in-memory database for testing
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "JewelryShopInvoiceTestDb_" + Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);

            // Create test customer
            _testCustomer = new Customer
            {
                Name = "Invoice Test Customer",
                Mobile = "7788990011",
                Email = "invoice@example.com",
                Address = "123 Invoice St, Test City",
                CreditLimit = 150000,
                CustomerType = "Retail",
                GSTNumber = "27AADCB2230M1Z3" // Optional GST number for B2B customers
            };

            // Create test products
            _testProduct1 = new Product
            {
                Name = "Diamond Necklace",
                Barcode = "DN001",
                NetWeight = 12.5m,
                GrossWeight = 13.0m,
                BasePrice = 6000.00m,
                MakingCharges = 25000.00m,
                WastagePercentage = 5.5m,
                CategoryID = 3, // Diamond category
                SubcategoryID = 3, // Necklace subcategory
                PurityLevel = "SI-IJ",
                StockQuantity = 3,
                FinalPrice = 120000.00m,
                HSNCode = "7113" // HSN code for jewelry
            };

            _testProduct2 = new Product
            {
                Name = "Gold Bracelet",
                Barcode = "GB002",
                NetWeight = 18.0m,
                GrossWeight = 18.5m,
                BasePrice = 5500.00m,
                MakingCharges = 12000.00m,
                WastagePercentage = 7.5m,
                CategoryID = 1, // Gold category
                SubcategoryID = 4, // Bracelet subcategory
                PurityLevel = "22K",
                StockQuantity = 5,
                FinalPrice = 115000.00m,
                HSNCode = "7113" // HSN code for jewelry
            };

            // Create business information for invoice
            _businessInfo = new BusinessInfo
            {
                Name = "Royal Jewelry Palace",
                Address = "456 Diamond Street, Luxury Mall",
                City = "Mumbai",
                State = "Maharashtra",
                PostalCode = "400001",
                Country = "India",
                Phone = "022-12345678",
                Email = "info@royaljewelrypalace.com",
                Website = "www.royaljewelrypalace.com",
                TaxIdentificationNumber = "27AABCP9571L1ZQ", // GSTIN
                LegalRegistrationNumber = "ABCDE1234F",
                Logo = "logo.png",
                BankName = "HDFC Bank",
                BankAccountNumber = "12345678901234",
                BankIFSC = "HDFC0001234"
            };

            _dbContext.Customers.Add(_testCustomer);
            _dbContext.Products.AddRange(_testProduct1, _testProduct2);
            _dbContext.BusinessInfos.Add(_businessInfo);
            _dbContext.SaveChanges();

            // Create test order
            _testOrder = new Order
            {
                CustomerID = _testCustomer.CustomerID,
                Customer = _testCustomer,
                OrderDate = DateTime.Now,
                Status = "Completed",
                PaymentMethod = "Credit Card",
                TotalItems = 2,
                InvoiceNumber = "INV-2025-0001",
                Notes = "Special customer, provide gift wrapping"
            };

            _dbContext.Orders.Add(_testOrder);
            _dbContext.SaveChanges();

            // Add order details
            var orderDetails = new List<OrderDetail>
            {
                new OrderDetail
                {
                    OrderID = _testOrder.OrderID,
                    ProductID = _testProduct1.ProductID,
                    Product = _testProduct1,
                    Quantity = 1,
                    UnitPrice = _testProduct1.FinalPrice,
                    TotalAmount = _testProduct1.FinalPrice,
                    NetWeight = _testProduct1.NetWeight,
                    GrossWeight = _testProduct1.GrossWeight,
                    MetalRate = _testProduct1.BasePrice,
                    MakingCharges = _testProduct1.MakingCharges,
                    WastagePercentage = _testProduct1.WastagePercentage,
                    HSNCode = _testProduct1.HSNCode
                },
                new OrderDetail
                {
                    OrderID = _testOrder.OrderID,
                    ProductID = _testProduct2.ProductID,
                    Product = _testProduct2,
                    Quantity = 1,
                    UnitPrice = _testProduct2.FinalPrice,
                    TotalAmount = _testProduct2.FinalPrice,
                    NetWeight = _testProduct2.NetWeight,
                    GrossWeight = _testProduct2.GrossWeight,
                    MetalRate = _testProduct2.BasePrice,
                    MakingCharges = _testProduct2.MakingCharges,
                    WastagePercentage = _testProduct2.WastagePercentage,
                    HSNCode = _testProduct2.HSNCode
                }
            };

            _dbContext.OrderDetails.AddRange(orderDetails);

            // Calculate and update order totals
            _testOrder.TotalAmount = orderDetails.Sum(od => od.TotalAmount);
            _testOrder.CGST = _testOrder.TotalAmount * 0.015m;
            _testOrder.SGST = _testOrder.TotalAmount * 0.015m;
            _testOrder.GrandTotal = _testOrder.TotalAmount + _testOrder.CGST + _testOrder.SGST;

            _dbContext.SaveChanges();
        }

        [TestMethod]
        public async Task GenerateInvoice_ShouldContainCorrectOrderDetails()
        {
            // Act - Load all relevant data for the order
            var order = await _dbContext.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderID == _testOrder.OrderID);

            var businessInfo = await _dbContext.BusinessInfos.FirstOrDefaultAsync();

            // Assert - Check key invoice components are present
            Assert.IsNotNull(order);
            Assert.IsNotNull(businessInfo);
            
            // Verify business details
            Assert.AreEqual("Royal Jewelry Palace", businessInfo.Name);
            Assert.AreEqual("27AABCP9571L1ZQ", businessInfo.TaxIdentificationNumber);
            
            // Verify customer details
            Assert.AreEqual(_testCustomer.Name, order.Customer.Name);
            Assert.AreEqual(_testCustomer.Mobile, order.Customer.Mobile);
            Assert.AreEqual(_testCustomer.Address, order.Customer.Address);
            Assert.AreEqual(_testCustomer.GSTNumber, order.Customer.GSTNumber);
            
            // Verify order details
            Assert.AreEqual(2, order.OrderDetails.Count);
            Assert.AreEqual("INV-2025-0001", order.InvoiceNumber);
            
            // Verify product details
            var diamondNecklace = order.OrderDetails.FirstOrDefault(od => od.Product.Name == "Diamond Necklace");
            var goldBracelet = order.OrderDetails.FirstOrDefault(od => od.Product.Name == "Gold Bracelet");
            
            Assert.IsNotNull(diamondNecklace);
            Assert.IsNotNull(goldBracelet);
            
            Assert.AreEqual(120000.00m, diamondNecklace.UnitPrice);
            Assert.AreEqual(115000.00m, goldBracelet.UnitPrice);
            Assert.AreEqual("7113", diamondNecklace.HSNCode);
            
            // Verify tax calculations
            decimal totalBeforeTax = order.OrderDetails.Sum(od => od.TotalAmount);
            decimal expectedCGST = totalBeforeTax * 0.015m;
            decimal expectedSGST = totalBeforeTax * 0.015m;
            decimal expectedGrandTotal = totalBeforeTax + expectedCGST + expectedSGST;
            
            Assert.AreEqual(expectedCGST, order.CGST);
            Assert.AreEqual(expectedSGST, order.SGST);
            Assert.AreEqual(expectedGrandTotal, order.GrandTotal);
        }

        [TestMethod]
        public async Task VerifyInvoiceDetails_ShouldIncludeGSTDetails()
        {
            // Act - Load the order with all needed details
            var order = await _dbContext.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderID == _testOrder.OrderID);

            var businessInfo = await _dbContext.BusinessInfos.FirstOrDefaultAsync();

            // Prepare data for GST invoice
            var invoiceItems = order.OrderDetails.Select(od => new
            {
                ItemName = od.Product.Name,
                HSNCode = od.HSNCode,
                Quantity = od.Quantity,
                UnitPrice = od.UnitPrice,
                NetAmount = od.TotalAmount,
                CGST = od.TotalAmount * 0.015m,
                SGST = od.TotalAmount * 0.015m,
                Amount = od.TotalAmount + (od.TotalAmount * 0.03m) // Including GST
            }).ToList();

            // Assert
            Assert.IsNotNull(order);
            Assert.AreEqual(2, invoiceItems.Count);
            
            // Verify tax rate
            var taxRate = 3.0m; // 1.5% CGST + 1.5% SGST
            
            // Verify seller GSTIN
            Assert.AreEqual("27AABCP9571L1ZQ", businessInfo.TaxIdentificationNumber);
            
            // Check if buyer GSTIN is present for B2B invoice
            Assert.IsFalse(string.IsNullOrEmpty(order.Customer.GSTNumber));
            
            // Verify HSN codes
            Assert.AreEqual("7113", invoiceItems[0].HSNCode);
            Assert.AreEqual("7113", invoiceItems[1].HSNCode);
            
            // Verify tax calculations for each item
            Assert.AreEqual(invoiceItems[0].NetAmount * 0.015m, invoiceItems[0].CGST);
            Assert.AreEqual(invoiceItems[0].NetAmount * 0.015m, invoiceItems[0].SGST);
            Assert.AreEqual(invoiceItems[0].NetAmount * (1 + taxRate / 100), invoiceItems[0].Amount);
            
            // Verify invoice totals
            decimal totalTaxableValue = invoiceItems.Sum(item => item.NetAmount);
            decimal totalCGST = invoiceItems.Sum(item => item.CGST);
            decimal totalSGST = invoiceItems.Sum(item => item.SGST);
            decimal grandTotal = invoiceItems.Sum(item => item.Amount);
            
            Assert.AreEqual(order.TotalAmount, totalTaxableValue);
            Assert.AreEqual(order.CGST, totalCGST);
            Assert.AreEqual(order.SGST, totalSGST);
            Assert.AreEqual(order.GrandTotal, grandTotal);
        }

        [TestMethod]
        public async Task PrintInvoice_ShouldFormatCurrencyAndWeightCorrectly()
        {
            // Act - Get order and format values for printing
            var order = await _dbContext.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderID == _testOrder.OrderID);

            // Format currency values with INR symbol and thousand separators
            var formattedGrandTotal = string.Format("₹{0:N2}", order.GrandTotal);
            var formattedCGST = string.Format("₹{0:N2}", order.CGST);
            var formattedSGST = string.Format("₹{0:N2}", order.SGST);
            
            // Format weight values with grams
            var formattedOrderDetailsList = order.OrderDetails.Select(od => new
            {
                ProductName = od.Product.Name,
                NetWeight = string.Format("{0:N3} g", od.NetWeight),
                GrossWeight = string.Format("{0:N3} g", od.GrossWeight),
                UnitPrice = string.Format("₹{0:N2}", od.UnitPrice),
                MakingCharges = string.Format("₹{0:N2}", od.MakingCharges),
                TotalAmount = string.Format("₹{0:N2}", od.TotalAmount)
            }).ToList();

            // Assert
            Assert.IsNotNull(order);
            Assert.IsNotNull(formattedOrderDetailsList);
            Assert.AreEqual(2, formattedOrderDetailsList.Count);
            
            // Check formatting of specific values
            Assert.IsTrue(formattedGrandTotal.StartsWith("₹"));
            Assert.IsTrue(formattedCGST.StartsWith("₹"));
            Assert.IsTrue(formattedSGST.StartsWith("₹"));
            
            // Check weight formatting
            Assert.IsTrue(formattedOrderDetailsList[0].NetWeight.EndsWith(" g"));
            Assert.IsTrue(formattedOrderDetailsList[0].GrossWeight.EndsWith(" g"));
            
            // Check currency formatting for unit price
            Assert.IsTrue(formattedOrderDetailsList[0].UnitPrice.StartsWith("₹"));
            Assert.IsTrue(formattedOrderDetailsList[1].UnitPrice.StartsWith("₹"));
            
            // Make sure values themselves are consistent
            var detailItem1 = formattedOrderDetailsList[0];
            var originalDetail1 = order.OrderDetails.First(od => od.Product.Name == detailItem1.ProductName);
            Assert.AreEqual(string.Format("{0:N3} g", originalDetail1.NetWeight), detailItem1.NetWeight);
            Assert.AreEqual(string.Format("₹{0:N2}", originalDetail1.UnitPrice), detailItem1.UnitPrice);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}