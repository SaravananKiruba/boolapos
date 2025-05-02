using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
                    OrderDate = DateTime.Now,
                    Status = "Exchange",
                    PaymentMethod = "Exchange",
                    TotalItems = 2, // Returned + new item
                    Notes = notes ?? $"Product exchange: {returnedProduct.Name} for {newProduct.Name}"
                };

                // 2. Calculate values
                decimal returnValue = returnedProduct.FinalPrice * returnedQuantity;
                decimal newValue = newProduct.FinalPrice * newQuantity;
                decimal balanceAmount = newValue - returnValue;

                // 3. Add order details for both products
                var orderDetails = new List<OrderDetail>();

                // Returned product (negative quantity)
                var returnDetail = new OrderDetail
                {
                    ProductID = returnedProduct.ProductID,
                    Quantity = -returnedQuantity, // Negative for returned item
                    UnitPrice = returnedProduct.FinalPrice,
                    TotalAmount = -returnValue, // Negative amount for returned item
                    NetWeight = returnedProduct.NetWeight,
                    GrossWeight = returnedProduct.GrossWeight,
                    MetalRate = returnedProduct.BasePrice,
                    MakingCharges = returnedProduct.MakingCharges,
                    WastagePercentage = returnedProduct.WastagePercentage,
                    HSNCode = returnedProduct.HSNCode
                };
                orderDetails.Add(returnDetail);

                // New product
                var newDetail = new OrderDetail
                {
                    ProductID = newProduct.ProductID,
                    Quantity = newQuantity,
                    UnitPrice = newProduct.FinalPrice,
                    TotalAmount = newValue,
                    NetWeight = newProduct.NetWeight,
                    GrossWeight = newProduct.GrossWeight,
                    MetalRate = newProduct.BasePrice,
                    MakingCharges = newProduct.MakingCharges,
                    WastagePercentage = newProduct.WastagePercentage,
                    HSNCode = newProduct.HSNCode
                };
                orderDetails.Add(newDetail);

                // 4. Complete exchange calculations
                exchangeOrder.TotalAmount = balanceAmount > 0 ? balanceAmount : 0;
                
                // Calculate GST on the balance amount if customer pays extra
                if (balanceAmount > 0)
                {
                    exchangeOrder.CGST = Math.Round(balanceAmount * 0.015m, 2); // 1.5% CGST
                    exchangeOrder.SGST = Math.Round(balanceAmount * 0.015m, 2); // 1.5% SGST
                    exchangeOrder.GrandTotal = balanceAmount + exchangeOrder.CGST + exchangeOrder.SGST;
                }
                else
                {
                    exchangeOrder.CGST = 0;
                    exchangeOrder.SGST = 0;
                    exchangeOrder.GrandTotal = 0;
                }

                // Set exchange-specific fields
                exchangeOrder.HasMetalExchange = true;
                exchangeOrder.ExchangeMetalType = returnedProduct.CategoryID == newProduct.CategoryID ? 
                    returnedProduct.CategoryID.ToString() : "Mixed";
                exchangeOrder.ExchangeMetalPurity = returnedProduct.PurityLevel;
                exchangeOrder.ExchangeMetalWeight = returnedProduct.NetWeight * returnedQuantity;
                exchangeOrder.ExchangeValue = returnValue;

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
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o => o.CustomerID == customerId && o.Status == "Exchange")
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }
    }
}