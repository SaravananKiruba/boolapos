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
    public class Workflow1_CustomerSaleTests
    {
        private AppDbContext _dbContext;
        private Customer _testCustomer;
        private Product _testProduct;

        [TestInitialize]
        public void TestInitialize()
        {
            // Set up in-memory database for testing
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "JewelryShopTestDb_" + Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);

            // Create test customer
            _testCustomer = new Customer
            {
                Name = "Test Customer",
                Mobile = "9876543210",
                Email = "test@example.com",
                Address = "Test Address",
                CreditLimit = 50000,
                CustomerType = "Retail"
            };

            // Create test product (jewelry item)
            _testProduct = new Product
            {
                Name = "Gold Ring",
                Barcode = "GR001",
                NetWeight = 10.5m, // in grams
                GrossWeight = 11.0m, // in grams
                BasePrice = 5000.00m, // per gram price
                MakingCharges = 2000.00m,
                WastagePercentage = 7.5m,
                CategoryID = 1,
                SubcategoryID = 1,
                PurityLevel = "22K",
                StockQuantity = 5,
                FinalPrice = 65000.00m // Calculated price with all charges
            };

            _dbContext.Customers.Add(_testCustomer);
            _dbContext.Products.Add(_testProduct);
            _dbContext.SaveChanges();
        }

        [TestMethod]
        public async Task AddCustomer_ShouldCreateNewCustomer()
        {
            // Arrange
            var newCustomer = new Customer
            {
                Name = "New Test Customer",
                Mobile = "1234567890",
                Email = "newtest@example.com",
                Address = "New Test Address",
                CreditLimit = 100000,
                CustomerType = "Wholesale"
            };

            // Act
            _dbContext.Customers.Add(newCustomer);
            await _dbContext.SaveChangesAsync();

            // Assert
            var savedCustomer = await _dbContext.Customers.FindAsync(newCustomer.CustomerID);
            Assert.IsNotNull(savedCustomer);
            Assert.AreEqual("New Test Customer", savedCustomer.Name);
            Assert.AreEqual("1234567890", savedCustomer.Mobile);
            Assert.AreEqual(100000, savedCustomer.CreditLimit);
        }

        [TestMethod]
        public async Task AddProductToInventory_ShouldCreateNewProduct()
        {
            // Arrange
            var newProduct = new Product
            {
                Name = "Silver Bracelet",
                Barcode = "SB001",
                NetWeight = 25.0m,
                GrossWeight = 26.5m,
                BasePrice = 75.00m, // per gram price for silver
                MakingCharges = 500.00m,
                WastagePercentage = 5.0m,
                CategoryID = 2, // Silver category
                SubcategoryID = 5, // Bracelet subcategory
                PurityLevel = "92.5%",
                StockQuantity = 10,
                FinalPrice = 2500.00m
            };

            // Act
            _dbContext.Products.Add(newProduct);
            await _dbContext.SaveChangesAsync();

            // Assert
            var savedProduct = await _dbContext.Products.FindAsync(newProduct.ProductID);
            Assert.IsNotNull(savedProduct);
            Assert.AreEqual("Silver Bracelet", savedProduct.Name);
            Assert.AreEqual("SB001", savedProduct.Barcode);
            Assert.AreEqual(25.0m, savedProduct.NetWeight);
            Assert.AreEqual(10, savedProduct.StockQuantity);
        }

        [TestMethod]
        public async Task CreateOrder_WithSelectedCustomerAndProduct_ShouldCreateOrder()
        {
            // Arrange
            var customer = await _dbContext.Customers.FirstAsync();
            var product = await _dbContext.Products.FirstAsync();

            var order = new Order
            {
                CustomerID = customer.CustomerID,
                OrderDate = DateTime.Now,
                Status = "Pending",
                PaymentMethod = "Cash",
                TotalItems = 1
            };

            var orderDetail = new OrderDetail
            {
                ProductID = product.ProductID,
                Quantity = 1,
                UnitPrice = product.FinalPrice,
                TotalAmount = product.FinalPrice,
                NetWeight = product.NetWeight,
                GrossWeight = product.GrossWeight,
                MetalRate = product.BasePrice,
                MakingCharges = product.MakingCharges,
                WastagePercentage = product.WastagePercentage
            };

            // Calculate taxes (GST for jewelry is typically 3% - 1.5% CGST + 1.5% SGST)
            order.CGST = orderDetail.TotalAmount * 0.015m;
            order.SGST = orderDetail.TotalAmount * 0.015m;
            
            // Calculate grand total
            order.TotalAmount = orderDetail.TotalAmount;
            order.GrandTotal = order.TotalAmount + order.CGST + order.SGST;

            // Act
            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync();

            orderDetail.OrderID = order.OrderID;
            _dbContext.OrderDetails.Add(orderDetail);
            await _dbContext.SaveChangesAsync();

            // Assert
            var savedOrder = await _dbContext.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderID == order.OrderID);

            Assert.IsNotNull(savedOrder);
            Assert.AreEqual(customer.CustomerID, savedOrder.CustomerID);
            Assert.AreEqual(1, savedOrder.OrderDetails.Count);
            Assert.AreEqual(product.ProductID, savedOrder.OrderDetails.First().ProductID);
            Assert.AreEqual(product.FinalPrice, savedOrder.OrderDetails.First().UnitPrice);
            
            // Verify the calculated values
            Assert.AreEqual(product.FinalPrice, savedOrder.TotalAmount);
            Assert.AreEqual(product.FinalPrice * 0.015m, savedOrder.CGST);
            Assert.AreEqual(product.FinalPrice * 0.015m, savedOrder.SGST);
            Assert.AreEqual(product.FinalPrice + (product.FinalPrice * 0.03m), savedOrder.GrandTotal);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}