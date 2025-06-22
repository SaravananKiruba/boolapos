using System;
using System.Collections.Generic;
using Page_Navigation_App.Model;
using Page_Navigation_App.Services;

namespace Page_Navigation_App.Utilities
{
    /// <summary>
    /// Demo class showing how to use the pricing system
    /// </summary>
    public class PricingDemo
    {
        public void RunDemo()
        {
            // Initialize pricing service
            var pricingService = new PricingService();
            
            // Create a sample product
            var product = new Product
            {
                ProductID = 1,
                ProductName = "Gold Ring",
                MetalType = "Gold",
                Purity = "22k",
                GrossWeight = 10.5m,
                NetWeight = 9.8m,
                BasePrice = 50000.00m,  // ₹50,000
                MakingCharges = 5000.00m, // ₹5,000
                WastagePercentage = 8.00m, // 8%
                Description = "22k Gold Ring with traditional design"
            };
              // Calculate the final price for the product
            decimal ratePerGram = 5500.00m; // Example gold rate per gram
            pricingService.CalculateProductPrice(product, ratePerGram);
            Console.WriteLine($"Product: {product.ProductName}");
            Console.WriteLine($"Product Price: ₹{product.ProductPrice:N2}");
            Console.WriteLine($"Making Charges: ₹{product.MakingCharges:N2}");
            Console.WriteLine($"Wastage ({product.WastagePercentage}%): ₹{product.NetWeight * ratePerGram * product.WastagePercentage / 100:N2}");
            Console.WriteLine($"Final Product Price: ₹{product.ProductPrice:N2}");
            Console.WriteLine();
            
            // Create another product
            var product2 = new Product
            {
                ProductID = 2,
                ProductName = "Gold Chain",
                MetalType = "Gold",
                Purity = "22k",
                GrossWeight = 25.0m,
                NetWeight = 24.0m,
                BasePrice = 120000.00m,  // ₹120,000
                MakingCharges = 10000.00m, // ₹10,000
                WastagePercentage = 5.00m, // 5%
                Description = "22k Gold Chain"
            };
            pricingService.CalculateProductPrice(product2, ratePerGram);
              // Create an order with these products
            var order = new Order
            {
                OrderID = 1,
                CustomerID = 1,
                OrderDate = DateOnly.FromDateTime(DateTime.Now),
                Status = "Pending",
                PaymentMethod = "Cash",
                DiscountAmount = 2000.00m, // ₹2,000 discount
                OrderDetails = new List<OrderDetail>()
            };
              // Add first product to order
            var orderDetail1 = new OrderDetail
            {
                OrderID = order.OrderID,
                ProductID = product.ProductID,
                Product = product,
                Quantity = 1,
                UnitPrice = product.ProductPrice,
                TotalAmount = product.ProductPrice
            };
            order.OrderDetails.Add(orderDetail1);
              // Add second product to order
            var orderDetail2 = new OrderDetail
            {
                OrderID = order.OrderID,
                ProductID = product2.ProductID,
                Product = product2,
                Quantity = 1,
                UnitPrice = product2.ProductPrice,
                TotalAmount = product2.ProductPrice
            };
            order.OrderDetails.Add(orderDetail2);
            
            // Calculate order totals using new simplified calculation
            order.CalculateOrderTotals(); // Use the method on the order object
            
            Console.WriteLine("Order Summary");
            Console.WriteLine("=============");
            Console.WriteLine($"Total Items: {order.TotalItems}");
            Console.WriteLine($"Total Amount: ₹{order.TotalAmount:N2}");
            Console.WriteLine($"Discount: ₹{order.DiscountAmount:N2}");
            Console.WriteLine($"Price Before Tax: ₹{order.PriceBeforeTax:N2}");
            Console.WriteLine($"Tax (3%): ₹{order.GrandTotal - order.PriceBeforeTax:N2}");
            Console.WriteLine($"Grand Total: ₹{order.GrandTotal:N2}");
        }
    }
}
