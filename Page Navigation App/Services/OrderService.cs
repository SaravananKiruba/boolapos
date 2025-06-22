using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;

namespace Page_Navigation_App.Services
{    public class OrderService
    {
        private readonly AppDbContext _context;
        private readonly StockService _stockService;
        private readonly RateMasterService _rateService;
        private readonly LogService _logService;

        public OrderService(
            AppDbContext context,
            StockService stockService,
            RateMasterService rateService,
            LogService logService)
        {
            _context = context;
            _stockService = stockService;
            _rateService = rateService;
            _logService = logService;
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
            return true;        }
        
        // Add new method for filtering orders by date
        public async Task<IEnumerable<Order>> FilterOrdersByDate(DateOnly startDate, DateOnly endDate)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
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
        }        // Filter orders by DateTime range
        public async Task<IEnumerable<Order>> GetOrdersByDateRange(DateTime startDate, DateTime endDate)
        {
            DateOnly startDateOnly = DateOnly.FromDateTime(startDate);
            DateOnly endDateOnly = DateOnly.FromDateTime(endDate);
            
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .Where(o => o.OrderDate >= startDateOnly && o.OrderDate <= endDateOnly)
                .ToListAsync();
        }

        // Get all orders
        public async Task<IEnumerable<Order>> GetAllOrders()
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        // Get order details
        public async Task<IEnumerable<OrderDetail>> GetOrderDetails(int orderId)
        {
            return await _context.OrderDetails
                .Include(od => od.Product)
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
        }        public async Task<Order> CreateOrder(Order order, List<OrderDetail> details)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Add order
                order.OrderDate = DateOnly.FromDateTime(DateTime.Now);
                
                // Generate invoice number if not provided
                if (string.IsNullOrEmpty(order.InvoiceNumber))
                {
                    order.InvoiceNumber = await GenerateInvoiceNumber();
                }
                
                await _context.Orders.AddAsync(order);
                await _context.SaveChangesAsync();

                decimal totalAmount = 0;
                int totalItems = 0;
                
                foreach (var detail in details)
                {
                    detail.OrderID = order.OrderID;
                    
                    // Set unit price from product price
                    detail.UnitPrice = detail.Product.ProductPrice;
                    
                    // Calculate total for this item
                    detail.TotalAmount = detail.UnitPrice * detail.Quantity;

                    totalAmount += detail.TotalAmount;                    totalItems += (int)detail.Quantity;
                    
                    await _context.OrderDetails.AddAsync(detail);
                      // Update stock - Use the enhanced StockService with proper transaction tracking
                    // Use our new method that also updates the individual stock items
                    await _stockService.DecreaseStockForOrder(
                        detail.ProductID,
                        (int)detail.Quantity,
                        order.OrderID);
                    
                    // Fallback to traditional stock reduction if individual item tracking fails
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
                order.PriceBeforeTax = totalAmount + order.DiscountAmount; // Apply discount
                if (order.PriceBeforeTax < 0) order.PriceBeforeTax = 0; // Ensure it doesn't go negative
                order.GrandTotal = Math.Round(order.PriceBeforeTax * 1.03m, 2); // Add 3% tax
                
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
                return order;            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                await _logService.LogErrorAsync($"Error creating order: {ex.Message}", userId: null, exception: ex);
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
        }        public async Task<Dictionary<string, decimal>> GetSalesSummary(DateTime startDate, DateTime endDate)        {
            // Convert DateTime to DateOnly for query
            var startDateOnly = DateOnly.FromDateTime(startDate);
            var endDateOnly = DateOnly.FromDateTime(endDate);
            
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .Where(o => o.OrderDate >= startDateOnly && o.OrderDate <= endDateOnly)
                .ToListAsync();            return new Dictionary<string, decimal>
            {
                { "TotalSales", orders.Sum(o => o.GrandTotal) },
                { "CashSales", orders.Where(o => o.PaymentType == "Cash").Sum(o => o.GrandTotal) },
                { "CreditSales", orders.Where(o => o.PaymentType == "Credit").Sum(o => o.GrandTotal) },
                { "EMISales", orders.Where(o => o.PaymentType == "EMI").Sum(o => o.GrandTotal) },
                { "TotalBeforeTax", orders.Sum(o => o.PriceBeforeTax) },
                { "TaxAmount", orders.Sum(o => o.GrandTotal - o.PriceBeforeTax) },
                { "DiscountAmount", orders.Sum(o => o.DiscountAmount) }
            };
        }

        public async Task<string> GenerateInvoiceNumber()
        {            var today = DateOnly.FromDateTime(DateTime.Today);
            var lastOrder = await _context.Orders
                .Where(o => o.OrderDate == today)
                .OrderByDescending(o => o.OrderID)
                .FirstOrDefaultAsync();

            int sequence = 1;
            if (lastOrder != null && lastOrder.InvoiceNumber != null)
            {
                var lastSequence = int.Parse(lastOrder.InvoiceNumber.Split('-')[1]);
                sequence = lastSequence + 1;
            }

            return $"{today:yyyyMMdd}-{sequence:D4}";
        }        public async Task<IEnumerable<Order>> GetOrdersByDate(DateTime startDate, DateTime endDate)
        {
            // Convert DateTime to DateOnly for query
            var startDateOnly = DateOnly.FromDateTime(startDate);
            var endDateOnly = DateOnly.FromDateTime(endDate);
            
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o => o.OrderDate >= startDateOnly && o.OrderDate <= endDateOnly)
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
        
        // New method to add order and update stock
        public async Task<Order> AddOrderWithStockUpdate(Order order, List<OrderDetail> orderDetails, Finance payment)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Add the order
                await _context.Orders.AddAsync(order);
                await _context.SaveChangesAsync();
                
                // 2. Add order details and update stock
                foreach (var detail in orderDetails)
                {
                    detail.OrderID = order.OrderID;
                    await _context.OrderDetails.AddAsync(detail);

                    // Reduce stock by marking StockItems as "Sold"
                    await UpdateStockForOrderItem(detail, order.OrderID);
                }
                
                await _context.SaveChangesAsync();
                
                // 3. Add payment record if provided
                if (payment != null)
                {
                    payment.OrderReference = order.OrderID;
                    payment.ReferenceType = "Order";
                    payment.ReferenceNumber = order.InvoiceNumber;
                    await _context.Finances.AddAsync(payment);
                    await _context.SaveChangesAsync();
                }
                
                await transaction.CommitAsync();
                return order;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                await _logService.LogErrorAsync($"Error adding order with stock update: {ex.Message}");
                return null;
            }
        }
        
        // Helper method to update stock for an order item
        private async Task<bool> UpdateStockForOrderItem(OrderDetail orderDetail, int orderId)
        {
            try
            {
                // Get available stock items for this product
                var stockItems = await _context.StockItems
                    .Where(si => si.ProductID == orderDetail.ProductID && si.Status == "Available")
                    .OrderBy(si => si.AddedDate) // First In, First Out
                    .Take((int)orderDetail.Quantity)
                    .ToListAsync();
                
                if (stockItems.Count < orderDetail.Quantity)
                {
                    await _logService.LogWarningAsync($"Insufficient stock for product ID {orderDetail.ProductID}. Needed: {orderDetail.Quantity}, Available: {stockItems.Count}");
                    return false;
                }
                
                // Mark stock items as sold
                foreach (var item in stockItems)
                {
                    item.Status = "Sold";
                    item.OrderID = orderId;
                    item.SoldDate = DateTime.Now;
                }
                
                await _context.SaveChangesAsync();
                
                // Update the stock quantities
                var stockGroups = stockItems.GroupBy(si => si.StockID);
                foreach (var group in stockGroups)
                {
                    var stockId = group.Key;
                    var count = group.Count();
                    
                    var stock = await _context.Stocks.FindAsync(stockId);
                    if (stock != null)
                    {
                        stock.QuantityPurchased -= count;
                        stock.LastUpdated = DateTime.Now;
                        stock.LastSold = DateTime.Now;
                    }
                }
                
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error updating stock for order item: {ex.Message}");
                return false;
            }
        }
    }
}