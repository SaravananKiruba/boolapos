using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Page_Navigation_App.Services
{
    public class OrderService
    {
        private readonly AppDbContext _context;
        private readonly StockService _stockService;
        private readonly RateMasterService _rateService;

        public OrderService(
            AppDbContext context,
            StockService stockService,
            RateMasterService rateService)
        {
            _context = context;
            _stockService = stockService;
            _rateService = rateService;
        }

        public async Task<Order> CreateOrder(Order order, List<OrderDetail> details)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Add order
                order.OrderDate = DateTime.Now;
                await _context.Orders.AddAsync(order);
                await _context.SaveChangesAsync();

                decimal totalAmount = 0;
                foreach (var detail in details)
                {
                    detail.OrderID = order.OrderID;
                    
                    // Calculate amounts
                    var metalRate = await _rateService.GetCurrentRate(
                        detail.Product.MetalType,
                        detail.Product.Purity);

                    detail.MetalRate = metalRate.SaleRate;
                    detail.BaseAmount = detail.NetWeight * detail.MetalRate;
                    
                    // Add making charges and stone value
                    detail.TaxableAmount = detail.BaseAmount + detail.MakingCharges + detail.StoneValue;
                    
                    // Calculate GST
                    if (!string.IsNullOrEmpty(order.GSTNumber))
                    {
                        detail.CGSTAmount = detail.TaxableAmount * 0.015m; // 1.5%
                        detail.SGSTAmount = detail.TaxableAmount * 0.015m; // 1.5%
                        detail.IGSTAmount = 0;
                    }
                    else
                    {
                        detail.IGSTAmount = detail.TaxableAmount * 0.03m; // 3%
                        detail.CGSTAmount = 0;
                        detail.SGSTAmount = 0;
                    }

                    detail.FinalAmount = detail.TaxableAmount + 
                                       detail.CGSTAmount + 
                                       detail.SGSTAmount + 
                                       detail.IGSTAmount;

                    totalAmount += detail.FinalAmount;

                    // Update stock
                    await _stockService.UpdateStockQuantity(
                        detail.ProductID,
                        "Main", // TODO: Make location configurable
                        -1 * detail.GrossWeight,
                        true);
                }

                // Handle metal exchange
                if (order.HasMetalExchange && order.ExchangeMetalWeight > 0)
                {
                    var exchangeRate = await _rateService.GetCurrentRate(
                        order.ExchangeMetalType,
                        order.ExchangeMetalPurity);

                    order.ExchangeValue = order.ExchangeMetalWeight * exchangeRate.PurchaseRate;
                    totalAmount -= order.ExchangeValue.Value;
                }

                // Update order totals
                order.SubTotal = totalAmount;
                order.TaxAmount = details.Sum(d => d.CGSTAmount + d.SGSTAmount + d.IGSTAmount);
                order.GrandTotal = totalAmount - order.DiscountAmount;

                // Handle EMI
                if (order.PaymentType == "EMI" && order.EMIMonths > 0)
                {
                    order.EMIAmount = Math.Ceiling(order.GrandTotal / order.EMIMonths.Value);
                }

                await _context.OrderDetails.AddRangeAsync(details);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return order;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Order> GetOrderWithDetails(int orderId)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);
        }

        public async Task<IEnumerable<Order>> GetCustomerOrders(int customerId)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .Where(o => o.CustomerID == customerId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<Dictionary<string, decimal>> GetSalesSummary(
            DateTime startDate,
            DateTime endDate)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .ToListAsync();

            return new Dictionary<string, decimal>
            {
                { "TotalSales", orders.Sum(o => o.GrandTotal) },
                { "CashSales", orders.Where(o => o.PaymentType == "Cash").Sum(o => o.GrandTotal) },
                { "CreditSales", orders.Where(o => o.PaymentType == "Credit").Sum(o => o.GrandTotal) },
                { "EMISales", orders.Where(o => o.PaymentType == "EMI").Sum(o => o.GrandTotal) },
                { "CGST", orders.Sum(o => o.OrderDetails.Sum(od => od.CGSTAmount)) },
                { "SGST", orders.Sum(o => o.OrderDetails.Sum(od => od.SGSTAmount)) },
                { "IGST", orders.Sum(o => o.OrderDetails.Sum(od => od.IGSTAmount)) },
                { "ExchangeValue", orders.Where(o => o.HasMetalExchange).Sum(o => o.ExchangeValue ?? 0) }
            };
        }

        public async Task<string> GenerateInvoiceNumber()
        {
            var today = DateTime.Today;
            var lastOrder = await _context.Orders
                .Where(o => o.OrderDate.Date == today)
                .OrderByDescending(o => o.OrderID)
                .FirstOrDefaultAsync();

            int sequence = 1;
            if (lastOrder != null && lastOrder.InvoiceNumber != null)
            {
                var lastSequence = int.Parse(lastOrder.InvoiceNumber.Split('-')[1]);
                sequence = lastSequence + 1;
            }

            return $"{today:yyyyMMdd}-{sequence:D4}";
        }
    }
}