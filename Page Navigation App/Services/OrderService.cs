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

        // Get all orders
        public async Task<IEnumerable<Order>> GetAllOrders()
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        // Get order details
        public async Task<IEnumerable<OrderDetail>> GetOrderDetails(int orderId)
        {
            return await _context.OrderDetails
                .Include(od => od.Product)
                .ThenInclude(p => p.Category)
                .Where(od => od.OrderID == orderId)
                .ToListAsync();
        }

        // Update order details
        public async Task<bool> UpdateOrderDetails(int orderId, List<OrderDetail> details)
        {
            // Remove existing details
            var existingDetails = await _context.OrderDetails
                .Where(od => od.OrderID == orderId)
                .ToListAsync();
            
            _context.OrderDetails.RemoveRange(existingDetails);
            
            // Add new details
            foreach (var detail in details)
            {
                detail.OrderID = orderId;
                await _context.OrderDetails.AddAsync(detail);
            }
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Order> CreateOrder(Order order, List<OrderDetail> details)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Add order
                order.OrderDate = DateTime.Now;
                
                // Generate invoice number if not provided
                if (string.IsNullOrEmpty(order.InvoiceNumber))
                {
                    order.InvoiceNumber = await GenerateInvoiceNumber();
                }
                
                await _context.Orders.AddAsync(order);
                await _context.SaveChangesAsync();

                decimal totalAmount = 0;
                decimal hallmarkingCharges = 0;
                int totalItems = 0;
                
                foreach (var detail in details)
                {
                    detail.OrderID = order.OrderID;
                    
                    // Get current metal rate - store the result as a decimal
                    var metalRate = _rateService.GetCurrentRate(
                        detail.Product.MetalType,
                        detail.Product.Purity);

                    if (metalRate <= 0)
                        throw new InvalidOperationException($"No rate found for {detail.Product.MetalType} {detail.Product.Purity}");

                    // Calculate base metal amount
                    // metalRate is now a decimal, not an object with SaleRate property
                    detail.MetalRate = metalRate;
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
                    
                    decimal makingAmount = (detail.BaseAmount * makingChargePercentage) / 100;

                    // Calculate total
                    detail.MakingCharges = makingChargePercentage;
                    detail.WastagePercentage = wastagePercentage;
                    detail.WastageAmount = wastageAmount;
                    detail.MakingAmount = makingAmount;
                    detail.TotalAmount = detail.BaseAmount + makingAmount + wastageAmount + detail.StoneValue;

                    // Add hallmarking charges if applicable (typically per piece)
                    if (detail.Product.IsHallmarked)
                    {
                        hallmarkingCharges += 45; // ₹45 per piece
                    }

                    totalAmount += detail.TotalAmount * detail.Quantity;
                    totalItems += (int)detail.Quantity;
                    
                    await _context.OrderDetails.AddAsync(detail);
                    
                    // Update stock - Use the enhanced StockService with proper transaction tracking
                    await _stockService.ReduceStock(
                        detail.ProductID, 
                        detail.Quantity, 
                        detail.UnitPrice, 
                        order.OrderID.ToString(), 
                        "Sale");
                }

                // Update order totals
                order.TotalItems = totalItems;
                order.TotalAmount = totalAmount;
                order.HallmarkingCharges = hallmarkingCharges;
                order.CGST = Math.Round(totalAmount * 0.015m, 2); // 1.5%
                order.SGST = Math.Round(totalAmount * 0.015m, 2); // 1.5%
                order.GrandTotal = totalAmount + hallmarkingCharges + order.CGST + order.SGST - order.DiscountAmount;
                
                await _context.SaveChangesAsync();
                
                // Create finance entry for the sale
                var financeEntry = new Finance
                {
                    TransactionDate = DateTime.Now,
                    Amount = order.GrandTotal,
                    Type = "Income",
                    Category = "Sales",
                    Description = $"Sale Invoice #{order.InvoiceNumber}",
                    PaymentMethod = order.PaymentMethod,
                    ReferenceID = order.OrderID.ToString(),
                    ReferenceType = "Order",
                    RecordedBy = "System"
                };
                
                await _context.Finances.AddAsync(financeEntry);
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

        public async Task<IEnumerable<Order>> GetAllOrdersWithDetails()
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