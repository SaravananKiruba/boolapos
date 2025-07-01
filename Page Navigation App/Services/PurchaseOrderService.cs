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
        private readonly LogService _logService;
        private readonly StockService _stockService;
        private readonly FinanceService _financeService;

        public PurchaseOrderService(
            AppDbContext context, 
            LogService logService, 
            StockService stockService,
            FinanceService financeService)
        {
            _context = context;
            _logService = logService;
            _stockService = stockService;
            _financeService = financeService;
        }

        // Create purchase order
        public async Task<PurchaseOrder> CreatePurchaseOrder(PurchaseOrder purchaseOrder, List<PurchaseOrderItem> items)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Generate purchase order number if not provided
                if (string.IsNullOrEmpty(purchaseOrder.PurchaseOrderNumber))
                {
                    purchaseOrder.PurchaseOrderNumber = await GeneratePurchaseOrderNumber();
                }

                // Add purchase order
                await _context.PurchaseOrders.AddAsync(purchaseOrder);
                await _context.SaveChangesAsync();

                decimal totalAmount = 0;
                int totalItems = 0;

                // Process each purchase order item
                foreach (var item in items)
                {
                    item.PurchaseOrderID = purchaseOrder.PurchaseOrderID;
                    item.TotalAmount = item.UnitCost * item.Quantity;
                    totalAmount += item.TotalAmount;
                    totalItems += (int)item.Quantity;

                    await _context.PurchaseOrderItems.AddAsync(item);
                }

                // Update purchase order totals
                purchaseOrder.TotalItems = totalItems;
                purchaseOrder.TotalAmount = totalAmount;
                purchaseOrder.TaxAmount = Math.Round(totalAmount * 0.18m, 2); // 18% GST
                purchaseOrder.GrandTotal = purchaseOrder.TotalAmount + purchaseOrder.TaxAmount - purchaseOrder.DiscountAmount;

                await _context.SaveChangesAsync();

                // Create finance entry for the expense
                var financeEntry = new Finance
                {
                    TransactionDate = DateTime.Now,
                    Amount = purchaseOrder.GrandTotal,
                    TransactionType = "Expense",
                    PaymentMethod = purchaseOrder.PaymentMethod,
                    PaymentMode = purchaseOrder.PaymentMethod,
                    Category = "Purchase",
                    Description = $"Purchase Order #{purchaseOrder.PurchaseOrderNumber} - ₹{purchaseOrder.GrandTotal:N2}",
                    Notes = $"Purchase from {purchaseOrder.Supplier?.SupplierName ?? "Supplier"}",
                    ReferenceNumber = purchaseOrder.PurchaseOrderNumber,
                    CreatedBy = purchaseOrder.CreatedBy,
                    Currency = "INR",
                    Status = "Pending"
                };

                await _context.Finances.AddAsync(financeEntry);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                await _logService.LogInformationAsync($"Created Purchase Order #{purchaseOrder.PurchaseOrderNumber} with {items.Count} items");

                return purchaseOrder;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                await _logService.LogErrorAsync($"Error creating purchase order: {ex.Message}", exception: ex);
                throw;
            }
        }

        // Get all purchase orders
        public async Task<IEnumerable<PurchaseOrder>> GetAllPurchaseOrders()
        {
            return await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.PurchaseOrderItems)
                    .ThenInclude(poi => poi.Product)
                .OrderByDescending(po => po.OrderDate)
                .ToListAsync();
        }

        // Get purchase order by ID
        public async Task<PurchaseOrder> GetPurchaseOrderById(int id)
        {
            return await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.PurchaseOrderItems)
                    .ThenInclude(poi => poi.Product)
                .FirstOrDefaultAsync(po => po.PurchaseOrderID == id);
        }

        // Get purchase orders by supplier
        public async Task<IEnumerable<PurchaseOrder>> GetPurchaseOrdersBySupplier(int supplierId)
        {
            return await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.PurchaseOrderItems)
                    .ThenInclude(poi => poi.Product)
                .Where(po => po.SupplierID == supplierId)
                .OrderByDescending(po => po.OrderDate)
                .ToListAsync();
        }

        // Receive purchase order (mark as delivered and add to stock)
        public async Task<bool> ReceivePurchaseOrder(int purchaseOrderId, List<PurchaseOrderItem> receivedItems = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var purchaseOrder = await GetPurchaseOrderById(purchaseOrderId);
                if (purchaseOrder == null) return false;

                // If specific items are provided, use them; otherwise, receive all items
                var itemsToReceive = receivedItems ?? purchaseOrder.PurchaseOrderItems.ToList();

                foreach (var item in itemsToReceive)
                {
                    // Update received quantity
                    var originalItem = purchaseOrder.PurchaseOrderItems.FirstOrDefault(poi => poi.PurchaseOrderItemID == item.PurchaseOrderItemID);
                    if (originalItem != null)
                    {
                        originalItem.ReceivedQuantity += item.Quantity;
                        originalItem.ReceivedDate = DateTime.Now;
                        
                        if (originalItem.ReceivedQuantity >= originalItem.Quantity)
                        {
                            originalItem.Status = "Delivered";
                        }
                    }

                    // Add to stock with individual item tracking
                    await _stockService.AddStockItems(
                        item.ProductID, 
                        (int)item.Quantity, 
                        item.UnitCost, 
                        purchaseOrderId);
                }

                // Update purchase order status
                bool allItemsReceived = purchaseOrder.PurchaseOrderItems.All(poi => poi.IsFullyReceived);
                purchaseOrder.Status = allItemsReceived ? "Delivered" : "Partially Delivered";
                purchaseOrder.ActualDeliveryDate = DateOnly.FromDateTime(DateTime.Now);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _logService.LogInformationAsync($"Received Purchase Order #{purchaseOrder.PurchaseOrderNumber}. Status: {purchaseOrder.Status}");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                await _logService.LogErrorAsync($"Error receiving purchase order: {ex.Message}", exception: ex);
                return false;
            }
        }

        // Update purchase order
        public async Task<bool> UpdatePurchaseOrder(PurchaseOrder purchaseOrder)
        {
            try
            {
                var existingPO = await _context.PurchaseOrders.FindAsync(purchaseOrder.PurchaseOrderID);
                if (existingPO == null) return false;

                existingPO.SupplierID = purchaseOrder.SupplierID;
                existingPO.ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate;
                existingPO.Status = purchaseOrder.Status;
                existingPO.PaymentMethod = purchaseOrder.PaymentMethod;
                existingPO.PaymentStatus = purchaseOrder.PaymentStatus;
                existingPO.Notes = purchaseOrder.Notes;
                existingPO.LastModified = DateTime.Now;

                await _context.SaveChangesAsync();
                await _logService.LogInformationAsync($"Updated Purchase Order #{existingPO.PurchaseOrderNumber}");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error updating purchase order: {ex.Message}", exception: ex);
                return false;
            }
        }

        // Cancel purchase order
        public async Task<bool> CancelPurchaseOrder(int purchaseOrderId, string reason)
        {
            try
            {
                var purchaseOrder = await _context.PurchaseOrders.FindAsync(purchaseOrderId);
                if (purchaseOrder == null) return false;

                purchaseOrder.Status = "Cancelled";
                purchaseOrder.Notes += $"\nCancelled: {reason}";
                purchaseOrder.LastModified = DateTime.Now;

                await _context.SaveChangesAsync();
                await _logService.LogInformationAsync($"Cancelled Purchase Order #{purchaseOrder.PurchaseOrderNumber}. Reason: {reason}");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error cancelling purchase order: {ex.Message}", exception: ex);
                return false;
            }
        }

        // Record payment for purchase order
        public async Task<bool> RecordPayment(int purchaseOrderId, decimal amount, string paymentMethod, string notes = null)
        {
            try
            {
                var purchaseOrder = await _context.PurchaseOrders.FindAsync(purchaseOrderId);
                if (purchaseOrder == null) return false;

                purchaseOrder.PaidAmount += amount;
                
                if (purchaseOrder.PaidAmount >= purchaseOrder.GrandTotal)
                {
                    purchaseOrder.PaymentStatus = "Paid";
                }
                else if (purchaseOrder.PaidAmount > 0)
                {
                    purchaseOrder.PaymentStatus = "Partial";
                }

                // Create finance entry for the payment
                var financeEntry = new Finance
                {
                    TransactionDate = DateTime.Now,
                    Amount = amount,
                    TransactionType = "Expense",
                    PaymentMethod = paymentMethod,
                    PaymentMode = paymentMethod,
                    Category = "Purchase Payment",
                    Description = $"Payment for Purchase Order #{purchaseOrder.PurchaseOrderNumber}",
                    Notes = notes,
                    ReferenceNumber = purchaseOrder.PurchaseOrderNumber,
                    CreatedBy = "System",
                    Currency = "INR",
                    Status = "Completed"
                };

                await _context.Finances.AddAsync(financeEntry);
                await _context.SaveChangesAsync();

                await _logService.LogInformationAsync($"Recorded payment of ₹{amount:N2} for Purchase Order #{purchaseOrder.PurchaseOrderNumber}");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error recording payment: {ex.Message}", exception: ex);
                return false;
            }
        }

        // Generate purchase order number
        private async Task<string> GeneratePurchaseOrderNumber()
        {
            var year = DateTime.Now.Year.ToString();
            var month = DateTime.Now.Month.ToString("D2");
            
            var lastPO = await _context.PurchaseOrders
                .Where(po => po.PurchaseOrderNumber.StartsWith($"PO{year}{month}"))
                .OrderByDescending(po => po.PurchaseOrderNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastPO != null)
            {
                var lastNumberPart = lastPO.PurchaseOrderNumber.Substring(6); // Remove "PO" + year + month
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"PO{year}{month}{nextNumber:D4}";
        }

        // Get pending purchase orders
        public async Task<IEnumerable<PurchaseOrder>> GetPendingPurchaseOrders()
        {
            return await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Where(po => po.Status == "Pending" || po.Status == "Confirmed")
                .OrderBy(po => po.ExpectedDeliveryDate)
                .ToListAsync();
        }

        // Get purchase summary for a date range
        public async Task<Dictionary<string, decimal>> GetPurchaseSummary(DateTime startDate, DateTime endDate)
        {
            var purchases = await _context.PurchaseOrders
                .Where(po => po.OrderDate >= DateOnly.FromDateTime(startDate) && 
                           po.OrderDate <= DateOnly.FromDateTime(endDate) &&
                           po.Status != "Cancelled")
                .ToListAsync();

            return new Dictionary<string, decimal>
            {
                { "TotalPurchases", purchases.Sum(po => po.GrandTotal) },
                { "TotalOrders", purchases.Count },
                { "AverageOrderValue", purchases.Any() ? purchases.Average(po => po.GrandTotal) : 0 },
                { "TotalItems", purchases.Sum(po => po.TotalItems) }
            };
        }
    }
}
