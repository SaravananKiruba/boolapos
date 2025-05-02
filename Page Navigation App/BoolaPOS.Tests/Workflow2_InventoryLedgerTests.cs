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
    public class Workflow2_InventoryLedgerTests
    {
        private AppDbContext _dbContext;
        private Product _testProduct;
        private Customer _testCustomer;

        [TestInitialize]
        public void TestInitialize()
        {
            // Set up in-memory database for testing
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "JewelryShopInventoryTestDb_" + Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);

            // Create test product (gold item)
            _testProduct = new Product
            {
                Name = "Gold Necklace",
                Barcode = "GN001",
                NetWeight = 25.5m, // in grams
                GrossWeight = 26.0m, // in grams
                BasePrice = 5500.00m, // per gram price
                MakingCharges = 15000.00m,
                WastagePercentage = 8.0m,
                CategoryID = 1, // Gold category
                SubcategoryID = 3, // Necklace subcategory
                PurityLevel = "22K",
                StockQuantity = 3,
                FinalPrice = 180000.00m // Calculated price with all charges
            };

            // Create test customer
            _testCustomer = new Customer
            {
                Name = "Inventory Test Customer",
                Mobile = "8877665544",
                Email = "inventorytest@example.com",
                Address = "Inventory Test Address",
                CreditLimit = 200000,
                CustomerType = "Retail"
            };

            _dbContext.Products.Add(_testProduct);
            _dbContext.Customers.Add(_testCustomer);
            _dbContext.SaveChanges();
        }

        [TestMethod]
        public async Task AddGoldItemToInventory_ShouldBeVisibleInStockList()
        {
            // Arrange
            var newGoldItem = new Product
            {
                Name = "Gold Bangle",
                Barcode = "GB001",
                NetWeight = 30.0m,
                GrossWeight = 31.5m,
                BasePrice = 5500.00m, // per gram price
                MakingCharges = 20000.00m,
                WastagePercentage = 7.5m,
                CategoryID = 1, // Gold category
                SubcategoryID = 4, // Bangle subcategory
                PurityLevel = "22K",
                StockQuantity = 2,
                FinalPrice = 200000.00m
            };

            // Act
            _dbContext.Products.Add(newGoldItem);
            await _dbContext.SaveChangesAsync();

            // Assert
            var productsInStock = await _dbContext.Products.Where(p => p.StockQuantity > 0).ToListAsync();
            
            Assert.IsTrue(productsInStock.Count > 0);
            Assert.IsNotNull(productsInStock.FirstOrDefault(p => p.Barcode == "GB001"));
            Assert.AreEqual(2, productsInStock.FirstOrDefault(p => p.Barcode == "GB001").StockQuantity);
        }

        [TestMethod]
        public async Task PerformSale_ShouldReduceInventoryInRealTime()
        {
            // Arrange
            var originalQuantity = _testProduct.StockQuantity;
            
            var order = new Order
            {
                CustomerID = _testCustomer.CustomerID,
                OrderDate = DateTime.Now,
                Status = "Completed",
                PaymentMethod = "Cash",
                TotalItems = 1
            };

            var orderDetail = new OrderDetail
            {
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

            // Act - Create order and update inventory
            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync();

            orderDetail.OrderID = order.OrderID;
            _dbContext.OrderDetails.Add(orderDetail);
            
            // Update inventory - simulate what happens in the service
            var product = await _dbContext.Products.FindAsync(_testProduct.ProductID);
            product.StockQuantity -= orderDetail.Quantity;
            
            await _dbContext.SaveChangesAsync();

            // Assert
            var updatedProduct = await _dbContext.Products.FindAsync(_testProduct.ProductID);
            Assert.AreEqual(originalQuantity - 1, updatedProduct.StockQuantity);
        }

        [TestMethod]
        public async Task ExchangeProduct_ShouldUpdateLedgerCorrectly()
        {
            // Arrange
            var originalProduct = _testProduct;
            var originalQuantity = originalProduct.StockQuantity;
            
            var exchangeProduct = new Product
            {
                Name = "Silver Chain",
                Barcode = "SC001",
                NetWeight = 40.0m,
                GrossWeight = 41.0m,
                BasePrice = 80.00m, // per gram price for silver
                MakingCharges = 1000.00m,
                WastagePercentage = 5.0m,
                CategoryID = 2, // Silver category
                SubcategoryID = 3, // Chain subcategory
                PurityLevel = "92.5%",
                StockQuantity = 5,
                FinalPrice = 5000.00m
            };
            
            _dbContext.Products.Add(exchangeProduct);
            await _dbContext.SaveChangesAsync();

            // Act - Simulate exchange transaction
            // 1. Reduce original product stock
            originalProduct.StockQuantity -= 1;
            
            // 2. Record new transaction for exchange
            var exchangeOrder = new Order
            {
                CustomerID = _testCustomer.CustomerID,
                OrderDate = DateTime.Now,
                Status = "Exchange",
                PaymentMethod = "Exchange",
                TotalItems = 1,
                Notes = "Product exchange: Gold Necklace for Silver Chain"
            };
            
            _dbContext.Orders.Add(exchangeOrder);
            await _dbContext.SaveChangesAsync();
            
            // 3. Add order details for both products
            var orderDetail1 = new OrderDetail
            {
                OrderID = exchangeOrder.OrderID,
                ProductID = originalProduct.ProductID,
                Quantity = -1, // Negative for returned item
                UnitPrice = originalProduct.FinalPrice,
                TotalAmount = -originalProduct.FinalPrice, // Negative amount for returned item
                NetWeight = originalProduct.NetWeight,
                GrossWeight = originalProduct.GrossWeight,
                MetalRate = originalProduct.BasePrice,
                MakingCharges = originalProduct.MakingCharges,
                WastagePercentage = originalProduct.WastagePercentage
            };
            
            var orderDetail2 = new OrderDetail
            {
                OrderID = exchangeOrder.OrderID,
                ProductID = exchangeProduct.ProductID,
                Quantity = 1,
                UnitPrice = exchangeProduct.FinalPrice,
                TotalAmount = exchangeProduct.FinalPrice,
                NetWeight = exchangeProduct.NetWeight,
                GrossWeight = exchangeProduct.GrossWeight,
                MetalRate = exchangeProduct.BasePrice,
                MakingCharges = exchangeProduct.MakingCharges,
                WastagePercentage = exchangeProduct.WastagePercentage
            };
            
            _dbContext.OrderDetails.Add(orderDetail1);
            _dbContext.OrderDetails.Add(orderDetail2);
            
            // 4. Update inventory for exchanged product
            exchangeProduct.StockQuantity -= 1;
            
            await _dbContext.SaveChangesAsync();

            // Assert
            var updatedOriginalProduct = await _dbContext.Products.FindAsync(originalProduct.ProductID);
            var updatedExchangeProduct = await _dbContext.Products.FindAsync(exchangeProduct.ProductID);
            
            // Check stock quantities are updated
            Assert.AreEqual(originalQuantity - 1, updatedOriginalProduct.StockQuantity);
            Assert.AreEqual(4, updatedExchangeProduct.StockQuantity); // Initial 5 - 1
            
            // Check that exchange transaction is recorded
            var transaction = await _dbContext.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Status == "Exchange");
                
            Assert.IsNotNull(transaction);
            Assert.AreEqual(2, transaction.OrderDetails.Count);
            Assert.AreEqual("Exchange", transaction.PaymentMethod);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}