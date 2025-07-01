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
        private readonly RateMasterService _rateService;
        private readonly LogService _logService;

        public OrderService(
            AppDbContext context,
            RateMasterService rateService,
            LogService logService)
        {
            _context = context;
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

        // Create new order with stock updates and financial entry
        public async Task<(Order order,Finance financeEntry)> CreateOrderWithStockAndFinance(
            Order order, 
            List<OrderDetail> orderDetails)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Generate invoice number if not provided
                if (string.IsNullOrEmpty(order.InvoiceNumber))
                {
                    order.InvoiceNumber = await GenerateInvoiceNumber();
                }
                
                // Add order
                await _context.Orders.AddAsync(order);
                await _context.SaveChangesAsync();

                // Process each order detail
                foreach (var detail in orderDetails)
                {
                    detail.OrderID = order.OrderID;

                    // Calculate totals
                    detail.TotalAmount = detail.UnitPrice * detail.Quantity;

                    await _context.OrderDetails.AddAsync(detail);

                    // Update stock - find and mark stock items as sold

                }
                
                // Calculate order totals
                order.TotalItems = orderDetails.Count;
                order.TotalAmount = orderDetails.Sum(d => d.TotalAmount);
                
                // Create finance entry for the income
                var finance = new Finance
                {
                    TransactionDate = DateTime.Now,
                    TransactionType = "Income",
                    Amount = order.TotalAmount,
                    PaymentMode = order.PaymentMethod,
                    Category = "Sales",
                    Description = $"Sale Invoice #{order.InvoiceNumber}",
                    ReferenceNumber = order.InvoiceNumber,
                    OrderID = order.OrderID,
                    CustomerID = order.CustomerID
                };
                
                await _context.Finances.AddAsync(finance);
                
                // Save all changes
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                await _logService.LogInformationAsync($"Created order #{order.OrderID} with {orderDetails.Count} items and updated stock");
                
                return (order, finance);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                await _logService.LogErrorAsync($"Error creating order with stock updates: {ex.Message}");
                return (null,  null);
            }
        }
        

        public async Task<(Order order, Finance financeEntry)> 
            CreateOrderWithIntegration(Order order, List<OrderDetail> orderDetails)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Generate invoice number if not provided
                if (string.IsNullOrEmpty(order.InvoiceNumber))
                {
                    order.InvoiceNumber = await GenerateInvoiceNumber();
                }
                
                // Add the order
                await _context.Orders.AddAsync(order);
                await _context.SaveChangesAsync();
                
                decimal totalAmount = 0;
                
                // Process each order detail
                foreach (var detail in orderDetails)
                {
                    detail.OrderID = order.OrderID;
                    
                    // Calculate the total
                    detail.TotalAmount = detail.UnitPrice * detail.Quantity;
                    totalAmount += detail.TotalAmount;
                    
                    await _context.OrderDetails.AddAsync(detail);
                    
                    // Find available stock items for this product
                   
                    
                 
                     
                }
                
                // Update order totals
                order.TotalAmount = totalAmount;
                order.TotalItems = orderDetails.Count;
                
                // Create finance entry for income
                var finance = new Finance
                {
                    TransactionDate = DateTime.Now,
                    TransactionType = "Income",
                    Amount = order.TotalAmount,
                    PaymentMode = order.PaymentMethod,
                    Category = "Sales",
                    Description = $"Sale Invoice #{order.InvoiceNumber}",
                    ReferenceNumber = order.InvoiceNumber,
                    OrderID = order.OrderID,
                    CustomerID = order.CustomerID
                };
                
                await _context.Finances.AddAsync(finance);
                await _context.SaveChangesAsync();
                
                await transaction.CommitAsync();
                await _logService.LogInformationAsync($"Created order {order.OrderID} with {orderDetails.Count} products, stock items and recorded income of {order.TotalAmount}");
                
                return (order,  finance);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                await _logService.LogErrorAsync($"Error creating order with integration: {ex.Message}");
                return (null,  null);
            }
        }
        
        // Generate invoice number for use in public methods
        public async Task<string> GenerateInvoiceNumber()
        {
            var today = DateTime.Now;
            var prefix = $"INV-{today:yyyyMMdd}-";
            
            // Find the last invoice number for today
            var lastInvoice = await _context.Orders
                .Where(o => o.InvoiceNumber.StartsWith(prefix))
                .OrderByDescending(o => o.InvoiceNumber)
                .Select(o => o.InvoiceNumber)
                .FirstOrDefaultAsync();
            
            int sequence = 1;
            
            if (lastInvoice != null)
            {
                var lastSequence = lastInvoice.Substring(prefix.Length);
                if (int.TryParse(lastSequence, out int lastSeq))
                {
                    sequence = lastSeq + 1;
                }
            }
            
            return $"{prefix}{sequence:D4}";
        }
        
        // Method to add an order with automatic stock updates
        public async Task<Order> AddOrderWithStockUpdate(Order order, List<OrderDetail> orderDetails, Finance payment = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Generate invoice number if not provided
                if (string.IsNullOrEmpty(order.InvoiceNumber))
                {
                    order.InvoiceNumber = await GenerateInvoiceNumber();
                }
                
                // Add the order
                await _context.Orders.AddAsync(order);
                await _context.SaveChangesAsync();
                
                decimal totalAmount = 0;
                
                // Process each order detail
                foreach (var detail in orderDetails)
                {
                    detail.OrderID = order.OrderID;
                    
                    // Calculate the total
                    detail.TotalAmount = detail.UnitPrice * detail.Quantity;
                    totalAmount += detail.TotalAmount;
                    
                    await _context.OrderDetails.AddAsync(detail);
                    
                   
                    
                    
                   
                    
                }
                
                // Update order totals
                order.TotalAmount = totalAmount;
                order.TotalItems = orderDetails.Count;
                await _context.SaveChangesAsync();
                
                // Create finance entry if not provided
                if (payment == null)
                {
                    payment = new Finance
                    {
                        TransactionDate = DateTime.Now,
                        TransactionType = "Income",
                        Amount = order.TotalAmount,
                        PaymentMode = order.PaymentMethod,
                        Category = "Sales",
                        Description = $"Sale Invoice #{order.InvoiceNumber}",
                        ReferenceNumber = order.InvoiceNumber,
                        OrderID = order.OrderID,
                        CustomerID = order.CustomerID
                    };
                }
                else
                {
                    payment.OrderID = order.OrderID;
                    payment.ReferenceNumber = order.InvoiceNumber;
                }
                
                await _context.Finances.AddAsync(payment);
                await _context.SaveChangesAsync();
                
                await transaction.CommitAsync();
                await _logService.LogInformationAsync($"Created order {order.OrderID} with {orderDetails.Count} products and recorded income of {order.TotalAmount}");
                
                return order;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                await _logService.LogErrorAsync($"Error creating order with stock update: {ex.Message}");
                return null;
            }
        }
          // The duplicate methods have been removed to avoid confusion.
        
        // Get orders by date range
        public async Task<IEnumerable<Order>> GetOrdersByDate(DateTime startDate, DateTime endDate)
        {
            DateOnly start = DateOnly.FromDateTime(startDate);
            DateOnly end = DateOnly.FromDateTime(endDate);
            
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o => o.OrderDate >= start && o.OrderDate <= end)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }
    }
}