using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Page_Navigation_App.Services
{
    public class PurchaseOrderService
    {
        private readonly AppDbContext _context;
        private readonly StockService _stockService;

        public PurchaseOrderService(AppDbContext context, StockService stockService)
        {
            _context = context;
            _stockService = stockService;
        }

        // Create a new purchase order
        public async Task<PurchaseOrder> CreatePurchaseOrder(PurchaseOrder purchaseOrder, List<PurchaseOrderItem> items)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Generate purchase order number
                purchaseOrder.PurchaseOrderNumber = await GeneratePurchaseOrderNumber();
                
                // Add the purchase order
                await _context.PurchaseOrders.AddAsync(purchaseOrder);
                await _context.SaveChangesAsync();
                
                // Set the purchase order ID for each item
                foreach (var item in items)
                {
                    item.PurchaseOrderID = purchaseOrder.PurchaseOrderID;
                    item.TotalPrice = item.UnitPrice * item.Quantity;
                    
                    await _context.PurchaseOrderItems.AddAsync(item);
                }
                
                // Calculate the total amount
                purchaseOrder.TotalAmount = items.Sum(i => i.TotalPrice);
                purchaseOrder.BalanceAmount = purchaseOrder.TotalAmount - purchaseOrder.PaidAmount;
                
                // Update payment status based on paid amount
                if (purchaseOrder.PaidAmount >= purchaseOrder.TotalAmount)
                {
                    purchaseOrder.PaymentStatus = "Paid";
                }
                else if (purchaseOrder.PaidAmount > 0)
                {
                    purchaseOrder.PaymentStatus = "Partial";
                }
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                return purchaseOrder;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                
                // Log the error
                await _context.LogEntries.AddAsync(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    LogLevel = "Error",
                    Message = $"Failed to create purchase order: {ex.Message}",
                    Source = "PurchaseOrderService.CreatePurchaseOrder",
                    ExceptionDetails = ex.ToString()
                });
                await _context.SaveChangesAsync();
                
                return null;
            }
        }
        
        // Complete a purchase order and add items to stock
        public async Task<bool> CompletePurchaseOrder(int purchaseOrderId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var purchaseOrder = await _context.PurchaseOrders
                    .Include(po => po.PurchaseOrderItems)
                    .ThenInclude(poi => poi.Product)
                    .FirstOrDefaultAsync(po => po.PurchaseOrderID == purchaseOrderId);
                    
                if (purchaseOrder == null) return false;
                
                // Update the purchase order status
                purchaseOrder.Status = "Completed";
                purchaseOrder.ActualDeliveryDate = DateTime.Now;
                
                // Create stock entries for each item
                foreach (var item in purchaseOrder.PurchaseOrderItems)
                {
                    // Create a stock entry for this product
                    var stock = new Stock
                    {
                        ProductID = item.ProductID,
                        SupplierID = purchaseOrder.SupplierID,
                        PurchaseDate = DateTime.Now,
                        QuantityPurchased = item.Quantity,
                        PurchaseRate = item.UnitPrice,
                        TotalAmount = item.TotalPrice,
                        InvoiceNumber = purchaseOrder.PurchaseOrderNumber,
                        PaymentStatus = purchaseOrder.PaymentStatus,
                        Location = "Main",
                        LastUpdated = DateTime.Now,
                        AddedDate = DateTime.Now
                    };
                    
                    await _context.Stocks.AddAsync(stock);
                    await _context.SaveChangesAsync();
                    
                    // Now create individual stock items with unique IDs for each product
                    await _stockService.AddStockItems(
                        item.ProductID, 
                        stock.StockID, 
                        Convert.ToInt32(Math.Floor(item.Quantity)));
                }
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                
                // Log the error
                await _context.LogEntries.AddAsync(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    LogLevel = "Error",
                    Message = $"Failed to complete purchase order: {ex.Message}",
                    Source = "PurchaseOrderService.CompletePurchaseOrder",
                    ExceptionDetails = ex.ToString()
                });
                await _context.SaveChangesAsync();
                
                return false;
            }
        }
        
        // Create an expense entry for a purchase order
        public async Task<Expense> CreateExpenseFromPurchaseOrder(int purchaseOrderId, string additionalNotes = null)
        {
            try
            {
                var purchaseOrder = await _context.PurchaseOrders
                    .Include(po => po.Supplier)
                    .FirstOrDefaultAsync(po => po.PurchaseOrderID == purchaseOrderId);
                    
                if (purchaseOrder == null) return null;
                if (purchaseOrder.HasExpenseEntry) return null; // Already has an expense entry
                
                var expense = new Expense
                {
                    Description = $"Purchase from {purchaseOrder.Supplier?.SupplierName ?? "supplier"}",
                    ExpenseDate = DateTime.Now,
                    Amount = purchaseOrder.TotalAmount,
                    Category = "Purchase",
                    PaymentMethod = purchaseOrder.PaymentMethod,
                    PurchaseOrderID = purchaseOrderId,
                    Recipient = purchaseOrder.Supplier?.SupplierName,
                    ReferenceNumber = purchaseOrder.PurchaseOrderNumber,
                    Notes = string.IsNullOrEmpty(additionalNotes) ? 
                        $"Expense for purchase order {purchaseOrder.PurchaseOrderNumber}" : 
                        additionalNotes
                };
                
                await _context.Expenses.AddAsync(expense);
                
                // Update the purchase order to indicate an expense entry has been created
                purchaseOrder.HasExpenseEntry = true;
                
                await _context.SaveChangesAsync();
                return expense;
            }
            catch (Exception ex)
            {
                // Log the error
                await _context.LogEntries.AddAsync(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    LogLevel = "Error",
                    Message = $"Failed to create expense from purchase order: {ex.Message}",
                    Source = "PurchaseOrderService.CreateExpenseFromPurchaseOrder",
                    ExceptionDetails = ex.ToString()
                });
                await _context.SaveChangesAsync();
                
                return null;
            }
        }
        
        // Get all purchase orders with optional filtering
        public async Task<IEnumerable<PurchaseOrder>> GetPurchaseOrders(
            string status = null, 
            string paymentStatus = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _context.PurchaseOrders
                .Include(po => po.Supplier)
                .AsQueryable();
                
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(po => po.Status == status);
            }
            
            if (!string.IsNullOrWhiteSpace(paymentStatus))
            {
                query = query.Where(po => po.PaymentStatus == paymentStatus);
            }
            
            if (startDate.HasValue)
            {
                query = query.Where(po => po.PurchaseDate >= startDate.Value);
            }
            
            if (endDate.HasValue)
            {
                query = query.Where(po => po.PurchaseDate <= endDate.Value);
            }
            
            return await query
                .OrderByDescending(po => po.PurchaseDate)
                .ToListAsync();
        }
        
        // Get purchase order by ID with details
        public async Task<PurchaseOrder> GetPurchaseOrderById(int purchaseOrderId)
        {
            return await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.PurchaseOrderItems)
                    .ThenInclude(poi => poi.Product)
                .FirstOrDefaultAsync(po => po.PurchaseOrderID == purchaseOrderId);
        }
        
        // Update purchase order payment
        public async Task<bool> UpdatePayment(
            int purchaseOrderId, 
            decimal paidAmount,
            string paymentMethod)
        {
            try
            {
                var purchaseOrder = await _context.PurchaseOrders
                    .FindAsync(purchaseOrderId);
                    
                if (purchaseOrder == null) return false;
                
                purchaseOrder.PaidAmount = paidAmount;
                purchaseOrder.BalanceAmount = purchaseOrder.TotalAmount - paidAmount;
                purchaseOrder.PaymentMethod = paymentMethod;
                
                // Update payment status
                if (paidAmount >= purchaseOrder.TotalAmount)
                {
                    purchaseOrder.PaymentStatus = "Paid";
                }
                else if (paidAmount > 0)
                {
                    purchaseOrder.PaymentStatus = "Partial";
                }
                else
                {
                    purchaseOrder.PaymentStatus = "Pending";
                }
                
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        // Generate a unique purchase order number
        private async Task<string> GeneratePurchaseOrderNumber()
        {
            string dateCode = DateTime.Now.ToString("yyyyMMdd");
            string poPrefix = $"PO-{dateCode}-";
            
            // Check for existing purchase orders with the same prefix
            int count = await _context.PurchaseOrders
                .CountAsync(po => po.PurchaseOrderNumber.StartsWith(poPrefix));
                
            // Create a sequential number with leading zeros
            string sequenceNumber = (count + 1).ToString().PadLeft(4, '0');
            
            return $"{poPrefix}{sequenceNumber}";
        }
    }
}
