using System;
using System.Collections.Generic;
using System.Linq;
using Page_Navigation_App.Model;

namespace Page_Navigation_App.Services
{
    /// <summary>
    /// Service responsible for calculating prices and order totals
    /// according to business requirements
    /// </summary>
    public class PricingService
    {        /// <summary>
        /// Calculate the final price for a product based on current rate
        /// </summary>
        /// <param name="product">The product to calculate price for</param>
        /// <param name="ratePerGram">The current rate per gram for the metal</param>
        public void CalculateProductPrice(Product product, decimal ratePerGram)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));
            
            // Use the calculation method in the Product class with the provided rate
            product.CalculateProductPrice(ratePerGram);
        }
        
        /// <summary>
        /// Calculate the product price breakdown
        /// </summary>
        /// <param name="product">The product to calculate breakdown for</param>
        /// <param name="ratePerGram">The current rate per gram for the metal</param>
        /// <returns>A detailed price breakdown</returns>
        public string GetPriceBreakdown(Product product, decimal ratePerGram)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));
                
            return product.GetPriceBreakdown(ratePerGram);
        }
        
        /// <summary>
        /// Calculate totals for a specific order detail
        /// </summary>
        /// <param name="orderDetail">The order detail to calculate</param>
        public void CalculateOrderDetailTotals(OrderDetail orderDetail)
        {
            if (orderDetail == null)
                throw new ArgumentNullException(nameof(orderDetail));
            
            if (orderDetail.Product == null)
                throw new InvalidOperationException("Product must be loaded to calculate order detail totals");
            
            // Use the calculation method in the OrderDetail class
            orderDetail.CalculateTotals();
        }
        
        /// <summary>
        /// Calculate all totals for an order including:
        /// - Total before tax
        /// - Discount
        /// - Taxable amount
        /// - GST calculation (if applicable)
        /// - Grand total
        /// </summary>
        /// <param name="order">The order to calculate totals for</param>
        public void CalculateOrderTotals(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));
            
            if (order.OrderDetails == null || !order.OrderDetails.Any())
                throw new InvalidOperationException("Order must have order details to calculate totals");
            
            // First calculate totals for each order detail
            foreach (var detail in order.OrderDetails)
            {
                if (detail.Product == null)
                    throw new InvalidOperationException($"Product for OrderDetail ID {detail.OrderDetailID} must be loaded");
                
                CalculateOrderDetailTotals(detail);
            }
            
            // Then calculate the overall order totals
            order.CalculateOrderTotals();
            
            // Update total items count
            order.TotalItems = order.OrderDetails.Sum(od => (int)Math.Ceiling(od.Quantity));
        }
    }
}
