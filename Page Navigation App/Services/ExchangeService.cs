using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;

namespace Page_Navigation_App.Services
{
    public class ExchangeService
    {
        private readonly AppDbContext _context;
        private readonly StockService _stockService;
        private readonly OrderService _orderService;

        public ExchangeService(
            AppDbContext context,
            StockService stockService,
            OrderService orderService)
        {
            _context = context;
            _stockService = stockService;
            _orderService = orderService;
        }

        public async Task<Order> ProcessExchange(
            int customerId,
            Product returnedProduct,
            decimal returnedQuantity,
            Product newProduct,
            decimal newQuantity,
            decimal exchangeRate,
            string notes = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Create exchange order
                var exchangeOrder = new Order
                {
                    CustomerID = customerId,
                    OrderDate = DateOnly.FromDateTime(DateTime.Now),
                    Status = "Exchange",
                    PaymentMethod = "Exchange",
                    TotalItems = 2, // Returned + new item
                    Notes = notes ?? $"Product exchange: {returnedProduct.ProductName} for {newProduct.ProductName}"
                };

                // 2. Calculate values
                decimal returnValue = returnedProduct.ProductPrice * returnedQuantity;
                decimal newValue = newProduct.ProductPrice * newQuantity;
                decimal balanceAmount = newValue - returnValue;

                // 3. Add order details for both products
                var orderDetails = new List<OrderDetail>();                // Returned product (negative quantity)
                var returnDetail = new OrderDetail
                {
                    ProductID = returnedProduct.ProductID,
                    Product = returnedProduct,
                    UnitPrice = returnedProduct.ProductPrice,
                    Quantity = returnedQuantity,
                    TotalAmount = returnedProduct.ProductPrice * returnedQuantity
                };
                orderDetails.Add(returnDetail);

                // New product
                var newDetail = new OrderDetail
                {
                    ProductID = newProduct.ProductID,
                    Product = newProduct,
                    UnitPrice = newProduct.ProductPrice,
                    Quantity = newQuantity,
                    TotalAmount = newProduct.ProductPrice * newQuantity
                };
                orderDetails.Add(newDetail);

                // 4. Complete exchange calculations
                exchangeOrder.TotalAmount = balanceAmount > 0 ? balanceAmount : 0;                // Calculate the price before tax
                if (balanceAmount > 0)
                {
                    exchangeOrder.TotalAmount = balanceAmount;
                    exchangeOrder.PriceBeforeTax = balanceAmount;
                    exchangeOrder.GrandTotal = Math.Round(balanceAmount * 1.03m, 2); // Apply 3% tax
                }
                else
                {
                    exchangeOrder.TotalAmount = 0;
                    exchangeOrder.PriceBeforeTax = 0;
                    exchangeOrder.GrandTotal = 0;
                }

                // Store exchange details in Notes field instead
                exchangeOrder.Notes = $"Exchange: Returned {returnedQuantity} x {returnedProduct.ProductName} ({returnedProduct.Purity}) worth ₹{returnValue:F2} for {newQuantity} x {newProduct.ProductName} worth ₹{newValue:F2}";

                // 5. Save order
                await _context.Orders.AddAsync(exchangeOrder);
                await _context.SaveChangesAsync();

                // 6. Save order details
                foreach (var detail in orderDetails)
                {
                    detail.OrderID = exchangeOrder.OrderID;
                    await _context.OrderDetails.AddAsync(detail);
                }
                await _context.SaveChangesAsync();

                // 7. Update inventory
                // Reduce inventory for new product
                await _stockService.ReduceStock(
                    newProduct.ProductID,
                    newQuantity,
                    newProduct.FinalPrice,
                    exchangeOrder.OrderID.ToString(),
                    "Exchange-Out");

                // Increase inventory for returned product
                await _stockService.IncreaseStock(
                    returnedProduct.ProductID,
                    returnedQuantity,
                    returnedProduct.FinalPrice,
                    exchangeOrder.OrderID.ToString(),
                    "Exchange-In");

                // 8. Generate invoice number if needed
                if (string.IsNullOrEmpty(exchangeOrder.InvoiceNumber))
                {
                    exchangeOrder.InvoiceNumber = await _orderService.GenerateInvoiceNumber();
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return exchangeOrder;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<Order>> GetExchangeHistory(int customerId)
        {
            // Fixed the query by applying Where on Orders first, then including related data
            return await _context.Orders
                .Where(o => o.CustomerID == customerId && o.Status == "Exchange")
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }
    }
}