using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;

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

        // Add new method for adding order
        public async Task<Order> AddOrder(Order order)
        {
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
            return order;
        }

        // Add new method for updating order
        public async Task<bool> UpdateOrder(Order order)
        {
            var existingOrder = await _context.Orders.FindAsync(order.OrderID);
            if (existingOrder == null) return false;

            _context.Entry(existingOrder).CurrentValues.SetValues(order);
            await _context.SaveChangesAsync();
            return true;
        }

        // Add new method for filtering orders by date
        public async Task<IEnumerable<Order>> FilterOrdersByDate(DateTime startDate, DateTime endDate)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.OrderDate.Date >= startDate.Date && o.OrderDate.Date <= endDate.Date)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        // Add new method for filtering orders by customer
        public async Task<IEnumerable<Order>> FilterOrdersByCustomer(int customerId)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.CustomerID == customerId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
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
                decimal hallmarkingCharges = 0;
                
                foreach (var detail in details)
                {
                    detail.OrderID = order.OrderID;
                    
                    // Get current metal rate
                    var metalRate = await _rateService.GetCurrentRate(
                        detail.Product.MetalType,
                        detail.Product.Purity);

                    if (metalRate == null)
                        throw new InvalidOperationException($"No rate found for {detail.Product.MetalType} {detail.Product.Purity}");

                    // Calculate base metal amount
                    detail.MetalRate = metalRate.SaleRate;
                    detail.BaseAmount = detail.NetWeight * detail.MetalRate;

                    // Calculate wastage
                    decimal wastagePercentage = detail.Product.WastagePercentage;
                    if (detail.Product.Subcategory?.SpecialWastage != null)
                    {
                        wastagePercentage = detail.Product.Subcategory.SpecialWastage.Value;
                    }
                    else if (detail.Product.Category?.DefaultWastage != null)
                    {
                        wastagePercentage = detail.Product.Category.DefaultWastage;
                    }
                    
                    decimal wastageAmount = (detail.BaseAmount * wastagePercentage) / 100;

                    // Calculate making charges
                    decimal makingChargePercentage = detail.Product.MakingCharges;
                    if (detail.Product.Subcategory?.SpecialMakingCharges != null)
                    {
                        makingChargePercentage = detail.Product.Subcategory.SpecialMakingCharges.Value;
                    }
                    else if (detail.Product.Category?.DefaultMakingCharges != null)
                    {
                        makingChargePercentage = detail.Product.Category.DefaultMakingCharges;
                    }

                    detail.MakingCharges = (detail.BaseAmount * makingChargePercentage) / 100;

                    // Calculate value addition if any
                    decimal valueAdditionAmount = 0;
                    if (detail.Product.ValueAdditionPercentage > 0)
                    {
                        valueAdditionAmount = (detail.BaseAmount * detail.Product.ValueAdditionPercentage) / 100;
                    }

                    // Calculate taxable amount including all components
                    detail.TaxableAmount = detail.BaseAmount + 
                                         wastageAmount + 
                                         detail.MakingCharges + 
                                         detail.StoneValue +
                                         valueAdditionAmount;
                    
                    // Calculate GST based on customer's GST registration
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

                    // Calculate final amount
                    detail.FinalAmount = detail.TaxableAmount + 
                                       detail.CGSTAmount + 
                                       detail.SGSTAmount + 
                                       detail.IGSTAmount;

                    totalAmount += detail.FinalAmount;

                    // Add hallmarking charges if applicable
                    if (detail.Product.HallmarkNumber != null)
                    {
                        hallmarkingCharges += metalRate.HallmarkingCharge;
                    }

                    // Update stock
                    await _stockService.UpdateStockQuantity(
                        detail.ProductID,
                        "Main",
                        -1 * detail.GrossWeight,
                        true);
                }

                // Handle metal exchange
                if (order.HasMetalExchange && order.ExchangeMetalWeight > 0)
                {
                    var exchangeRate = await _rateService.GetCurrentRate(
                        order.ExchangeMetalType,
                        order.ExchangeMetalPurity.ToString());

                    if (exchangeRate == null)
                        throw new InvalidOperationException($"No rate found for exchange metal {order.ExchangeMetalType} {order.ExchangeMetalPurity}");

                    // Use purchase rate for exchange metal
                    order.ExchangeValue = order.ExchangeMetalWeight * exchangeRate.PurchaseRate;
                    totalAmount -= order.ExchangeValue;
                }

                // Add hallmarking charges to total
                totalAmount += hallmarkingCharges;

                // Update order totals
                order.SubTotal = totalAmount;
                order.TaxAmount = details.Sum(d => d.CGSTAmount + d.SGSTAmount + d.IGSTAmount);
                order.GrandTotal = totalAmount - order.DiscountAmount;

                // Handle EMI
                if (order.PaymentType == "EMI" && order.EMIMonths > 0)
                {
                    order.EMIAmount = Math.Ceiling(order.GrandTotal / order.EMIMonths);
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

        public async Task<Dictionary<string, decimal>> GetSalesSummary(DateTime startDate, DateTime endDate)
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
                { "ExchangeValue", orders.Where(o => o.HasMetalExchange).Sum(o => o.ExchangeValue) }
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

        public async Task<IEnumerable<Order>> GetOrdersByDate(DateTime startDate, DateTime endDate)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o => o.OrderDate.Date >= startDate.Date && 
                           o.OrderDate.Date <= endDate.Date)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetAllOrders()
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }
    }
}