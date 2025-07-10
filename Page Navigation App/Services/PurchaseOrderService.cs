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
        private readonly BarcodeService _barcodeService;

        public PurchaseOrderService(
            AppDbContext context, 
            LogService logService, 
            StockService stockService,
            FinanceService financeService,
            BarcodeService barcodeService)
        {
            _context = context;
            _logService = logService;
            _stockService = stockService;
            _financeService = financeService;
            _barcodeService = barcodeService;
        }

        // Create purchase order
        public async Task<PurchaseOrder> CreatePurchaseOrder(PurchaseOrder purchaseOrder, List<PurchaseOrderItem> items)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Validate input
                if (purchaseOrder == null)
                    throw new ArgumentNullException(nameof(purchaseOrder));
                
                if (items == null || !items.Any())
                    throw new ArgumentException("Purchase order must have at least one item.", nameof(items));

                // Ensure supplier exists
                var supplier = await _context.Suppliers.FindAsync(purchaseOrder.SupplierID);
                if (supplier == null)
                    throw new InvalidOperationException($"Supplier with ID {purchaseOrder.SupplierID} not found.");

                // Generate purchase order number if not provided
                if (string.IsNullOrEmpty(purchaseOrder.PurchaseOrderNumber))
                {
                    purchaseOrder.PurchaseOrderNumber = await GeneratePurchaseOrderNumber();
                }

                // Check for duplicate purchase order number
                var existingPO = await _context.PurchaseOrders
                    .FirstOrDefaultAsync(po => po.PurchaseOrderNumber == purchaseOrder.PurchaseOrderNumber);
                if (existingPO != null)
                {
                    throw new InvalidOperationException($"Purchase order number {purchaseOrder.PurchaseOrderNumber} already exists.");
                }

                // Validate products exist
                var productIds = items.Select(i => i.ProductID).Distinct().ToList();
                var existingProducts = await _context.Products
                    .Where(p => productIds.Contains(p.ProductID))
                    .Select(p => p.ProductID)
                    .ToListAsync();
                
                var missingProducts = productIds.Except(existingProducts).ToList();
                if (missingProducts.Any())
                    throw new InvalidOperationException($"Products with IDs [{string.Join(", ", missingProducts)}] not found.");

                // Add purchase order
                await _context.PurchaseOrders.AddAsync(purchaseOrder);
                await _context.SaveChangesAsync();

                decimal totalAmount = 0;
                int totalItems = 0;

                // Process each purchase order item
                foreach (var item in items)
                {
                    item.PurchaseOrderID = purchaseOrder.PurchaseOrderID;
                    item.TotalAmount = Math.Round(item.UnitCost * item.Quantity, 2);
                    totalAmount += item.TotalAmount;
                    totalItems += (int)item.Quantity;

                    await _context.PurchaseOrderItems.AddAsync(item);
                }

                // Update purchase order totals - SIMPLIFIED (No GST)
                purchaseOrder.TotalItems = totalItems;
                purchaseOrder.TotalAmount = Math.Round(totalAmount, 2);
                // NO GST for Purchase Orders as per requirement
                purchaseOrder.GrandTotal = Math.Round(purchaseOrder.TotalAmount - purchaseOrder.DiscountAmount, 2);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Log after successful commit
                try
                {
                    await _logService.LogInformationAsync($"Created Purchase Order #{purchaseOrder.PurchaseOrderNumber} with {items.Count} items - Total: ₹{purchaseOrder.GrandTotal:N2}");
                }
                catch (Exception logEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to log: {logEx.Message}");
                }

                return purchaseOrder;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                
                // Log error after rollback
                try
                {
                    await _logService.LogErrorAsync($"Error creating purchase order: {ex.Message}", exception: ex);
                }
                catch (Exception logEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to log error: {logEx.Message}");
                }
                
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

        // Receive purchase order (mark as delivered and add to stock) - Enhanced with better error handling
        public async Task<bool> ReceivePurchaseOrder(int purchaseOrderId, List<PurchaseOrderItem> receivedItems = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Load purchase order with all related data
                var purchaseOrder = await _context.PurchaseOrders
                    .Include(po => po.Supplier)
                    .Include(po => po.PurchaseOrderItems)
                        .ThenInclude(poi => poi.Product)
                    .FirstOrDefaultAsync(po => po.PurchaseOrderID == purchaseOrderId);
                
                if (purchaseOrder == null) 
                {
                    await transaction.RollbackAsync();
                    throw new InvalidOperationException($"Purchase order with ID {purchaseOrderId} not found.");
                }

                // Validate purchase order can be received
                if (purchaseOrder.Status == "Cancelled")
                {
                    await transaction.RollbackAsync();
                    throw new InvalidOperationException("Cannot receive a cancelled purchase order.");
                }

                if (purchaseOrder.Status == "Delivered")
                {
                    await transaction.RollbackAsync();
                    throw new InvalidOperationException("Purchase order has already been fully delivered.");
                }

                // Determine which items to receive
                var itemsToProcess = new List<(PurchaseOrderItem originalItem, decimal quantityToReceive)>();
                
                if (receivedItems != null && receivedItems.Any())
                {
                    // Process specific items provided
                    foreach (var receivedItem in receivedItems)
                    {
                        var originalItem = purchaseOrder.PurchaseOrderItems
                            .FirstOrDefault(poi => poi.PurchaseOrderItemID == receivedItem.PurchaseOrderItemID);
                        
                        if (originalItem != null && originalItem.ReceivedQuantity < originalItem.Quantity)
                        {
                            var maxReceivable = originalItem.Quantity - originalItem.ReceivedQuantity;
                            var quantityToReceive = Math.Min(receivedItem.Quantity, maxReceivable);
                            
                            if (quantityToReceive > 0)
                            {
                                itemsToProcess.Add((originalItem, quantityToReceive));
                            }
                        }
                    }
                }
                else
                {
                    // Process all pending items
                    foreach (var item in purchaseOrder.PurchaseOrderItems)
                    {
                        var remainingQuantity = item.Quantity - item.ReceivedQuantity;
                        if (remainingQuantity > 0)
                        {
                            itemsToProcess.Add((item, remainingQuantity));
                        }
                    }
                }

                if (!itemsToProcess.Any())
                {
                    await transaction.RollbackAsync();
                    throw new InvalidOperationException("No items available to receive.");
                }

                // Process each item
                foreach (var (originalItem, quantityToReceive) in itemsToProcess)
                {
                    try
                    {
                        // Validate product exists
                        if (originalItem.Product == null)
                        {
                            var product = await _context.Products.FindAsync(originalItem.ProductID);
                            if (product == null)
                            {
                                throw new InvalidOperationException($"Product with ID {originalItem.ProductID} not found.");
                            }
                            originalItem.Product = product;
                        }

                        // Update purchase order item
                        originalItem.ReceivedQuantity += quantityToReceive;
                        originalItem.ReceivedDate = DateTime.Now;
                        originalItem.ReceivedBy = Environment.UserName ?? "System";
                        originalItem.IsAddedToStock = true;
                        originalItem.StockAddedDate = DateTime.Now;
                        
                        // Update status based on received quantity
                        if (originalItem.ReceivedQuantity >= originalItem.Quantity)
                        {
                            originalItem.Status = "Delivered";
                        }
                        else
                        {
                            originalItem.Status = "Partially Delivered";
                        }

                        // Validate item data before saving
                        if (originalItem.ProductID <= 0)
                            throw new InvalidOperationException($"Invalid ProductID: {originalItem.ProductID}");
                        
                        if (originalItem.PurchaseOrderID <= 0)
                            throw new InvalidOperationException($"Invalid PurchaseOrderID: {originalItem.PurchaseOrderID}");
                        
                        if (originalItem.UnitCost <= 0)
                            throw new InvalidOperationException($"Invalid UnitCost: {originalItem.UnitCost}");

                        // Add to stock
                        await AddStockItemsDirectly(
                            originalItem.ProductID, 
                            (int)quantityToReceive, 
                            originalItem.UnitCost, 
                            purchaseOrderId, 
                            purchaseOrder.SupplierID);
                    }
                    catch (Exception itemEx)
                    {
                        throw new InvalidOperationException($"Error processing item {originalItem.ProductID}: {itemEx.Message}", itemEx);
                    }
                }

                // Update purchase order status
                var allItemsReceived = purchaseOrder.PurchaseOrderItems.All(poi => 
                    poi.ReceivedQuantity >= poi.Quantity);
                
                var anyItemsReceived = purchaseOrder.PurchaseOrderItems.Any(poi => 
                    poi.ReceivedQuantity > 0);

                purchaseOrder.Status = allItemsReceived ? "Delivered" : 
                                     anyItemsReceived ? "Partially Delivered" : "Pending";
                
                purchaseOrder.ItemReceiptStatus = allItemsReceived ? "Completed" : 
                                                anyItemsReceived ? "Partial" : "Pending";
                
                purchaseOrder.IsItemsReceived = allItemsReceived;
                purchaseOrder.ActualDeliveryDate = allItemsReceived ? DateOnly.FromDateTime(DateTime.Now) : null;
                purchaseOrder.ItemsReceivedDate = allItemsReceived ? DateTime.Now : null;
                purchaseOrder.LastModified = DateTime.Now;

                // Validate data before saving
                ValidatePurchaseOrderData(purchaseOrder);

                // Save all changes in one transaction
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException dbEx)
                {
                    var innerMessage = dbEx.InnerException?.Message ?? "No inner exception details";
                    throw new InvalidOperationException($"Database update failed: {dbEx.Message}. Inner: {innerMessage}", dbEx);
                }
                catch (Exception saveEx)
                {
                    throw new InvalidOperationException($"Failed to save changes: {saveEx.Message}", saveEx);
                }
                
                await transaction.CommitAsync();

                // Log success after transaction commits
                try
                {
                    await _logService.LogInformationAsync($"Successfully received Purchase Order #{purchaseOrder.PurchaseOrderNumber}. Status: {purchaseOrder.Status}, Items Processed: {itemsToProcess.Count}");
                }
                catch (Exception logEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to log success: {logEx.Message}");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                
                // Create detailed error message
                var errorDetails = $"Error receiving purchase order {purchaseOrderId}: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorDetails += $" Inner Exception: {ex.InnerException.Message}";
                }
                
                // Log error after rollback
                try
                {
                    await _logService.LogErrorAsync(errorDetails, exception: ex);
                }
                catch (Exception logEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to log error: {logEx.Message}");
                }
                
                // Re-throw with more specific information
                throw new InvalidOperationException(errorDetails, ex);
            }
        }

        // Helper method to add stock items directly within the same transaction
        private async Task AddStockItemsDirectly(int productId, int quantity, decimal unitCost, int purchaseOrderId, int? supplierId)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null) 
                    throw new InvalidOperationException($"Product with ID {productId} not found.");

                if (quantity <= 0)
                    throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

                if (unitCost <= 0)
                    throw new ArgumentException("Unit cost must be greater than zero.", nameof(unitCost));

                // Determine the supplier ID to use
                var stockSupplierId = supplierId ?? product.SupplierID;

                // Create StockItem entries for individual tracking
                var stockItems = new List<StockItem>();
                for (int i = 0; i < quantity; i++)
                {
                    // Generate unique tag ID and barcode with stronger uniqueness guarantees
                    string uniqueTagID;
                    string barcode;
                    
                    do {
                        // Use a combination of timestamp, Guid and random number to ensure uniqueness
                        string metalCode = product.MetalType?.Substring(0, Math.Min(2, (product.MetalType ?? "GLD").Length)).ToUpper() ?? "GLD";
                        string purityCode = (product.Purity ?? "22K").Replace("k", "").Replace("K", "");
                        string timestamp = DateTime.Now.ToString("yyMMddHHmmss");
                        string guid = Guid.NewGuid().ToString("N");
                        string uniquePart = guid.Substring(0, 10);
                        
                        uniqueTagID = $"{metalCode}{purityCode}-{timestamp}-{uniquePart}";
                    } while (await _context.StockItems.AnyAsync(si => si.UniqueTagID == uniqueTagID));
                    
                    do {
                        // Use a similar approach for barcode but with a different format
                        string timestamp = DateTime.Now.ToString("yyMMddHHmm");
                        string guid = Guid.NewGuid().ToString("N");
                        string uniquePart = guid.Substring(0, 8);
                        
                        barcode = $"ITM{productId:D4}-{timestamp}-{uniquePart}";
                    } while (await _context.StockItems.AnyAsync(si => si.Barcode == barcode));
                    
                    var stockItem = new StockItem
                    {
                        ProductID = productId,
                        UniqueTagID = uniqueTagID,
                        Barcode = barcode,
                        PurchaseCost = unitCost,
                        SellingPrice = product.ProductPrice,
                        PurchaseOrderID = purchaseOrderId,
                        PurchaseDate = DateTime.Now,
                        HUID = product.HUID ?? string.Empty,
                        Status = "Available",
                        CreatedDate = DateTime.Now,
                        Location = "Main Store"
                    };
                    stockItems.Add(stockItem);
                }

                if (stockItems.Any())
                {
                    await _context.StockItems.AddRangeAsync(stockItems);
                }

                // Also update/create main stock entry
                var existingStock = await _context.Stocks
                    .FirstOrDefaultAsync(s => s.ProductID == productId && s.PurchaseOrderID == purchaseOrderId);

                if (existingStock != null)
                {
                    existingStock.Quantity += quantity;
                    existingStock.AvailableCount += quantity;
                    existingStock.LastUpdated = DateTime.Now;
                }
                else
                {
                    var newStock = new Stock
                    {
                        ProductID = productId,
                        SupplierID = stockSupplierId,
                        Quantity = quantity,
                        AvailableCount = quantity,
                        ReservedCount = 0,
                        SoldCount = 0,
                        UnitCost = unitCost,
                        PurchaseOrderID = purchaseOrderId,
                        Status = "Available",
                        Location = "Main Store",
                        LastUpdated = DateTime.Now,
                        PurchaseOrderReceivedDate = DateTime.Now,
                        Batch = $"PO{purchaseOrderId}_{DateTime.Now:yyyyMMdd}"
                    };
                    await _context.Stocks.AddAsync(newStock);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to add stock items for Product ID {productId}: {ex.Message}", ex);
            }
        }

        // Note: Unique ID generation is now handled within AddStockItemsDirectly method

        // Update purchase order
        public async Task<bool> UpdatePurchaseOrder(PurchaseOrder purchaseOrder)
        {
            try
            {
                if (purchaseOrder == null || purchaseOrder.PurchaseOrderID <= 0)
                    return false;

                var existingPO = await _context.PurchaseOrders.FindAsync(purchaseOrder.PurchaseOrderID);
                if (existingPO == null) return false;

                // Ensure supplier exists if being changed
                if (existingPO.SupplierID != purchaseOrder.SupplierID)
                {
                    var supplier = await _context.Suppliers.FindAsync(purchaseOrder.SupplierID);
                    if (supplier == null)
                        throw new InvalidOperationException($"Supplier with ID {purchaseOrder.SupplierID} not found.");
                }

                existingPO.SupplierID = purchaseOrder.SupplierID;
                existingPO.ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate;
                existingPO.Status = purchaseOrder.Status;
                existingPO.PaymentMethod = purchaseOrder.PaymentMethod;
                existingPO.PaymentStatus = purchaseOrder.PaymentStatus;
                existingPO.Notes = purchaseOrder.Notes;
                existingPO.DiscountAmount = purchaseOrder.DiscountAmount;
                existingPO.LastModified = DateTime.Now;

                // Recalculate grand total
                existingPO.GrandTotal = Math.Round(existingPO.TotalAmount - existingPO.DiscountAmount, 2);

                await _context.SaveChangesAsync();
                
                // Log after successful save
                try
                {
                    await _logService.LogInformationAsync($"Updated Purchase Order #{existingPO.PurchaseOrderNumber}");
                }
                catch (Exception logEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to log: {logEx.Message}");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    await _logService.LogErrorAsync($"Error updating purchase order: {ex.Message}", exception: ex);
                }
                catch (Exception logEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to log error: {logEx.Message}");
                }
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
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Load purchase order with validation
                var purchaseOrder = await _context.PurchaseOrders
                    .FirstOrDefaultAsync(po => po.PurchaseOrderID == purchaseOrderId);
                
                if (purchaseOrder == null)
                {
                    throw new InvalidOperationException($"Purchase order with ID {purchaseOrderId} not found.");
                }

                // Validate payment amount
                if (amount <= 0)
                {
                    throw new ArgumentException("Payment amount must be greater than zero.");
                }

                var remainingAmount = purchaseOrder.GrandTotal - purchaseOrder.PaidAmount;
                if (amount > remainingAmount)
                {
                    throw new ArgumentException($"Payment amount (₹{amount:N2}) cannot exceed remaining balance (₹{remainingAmount:N2}).");
                }

                // Update purchase order payment details
                purchaseOrder.PaidAmount += amount;
                
                // Update payment status
                if (purchaseOrder.PaidAmount >= purchaseOrder.GrandTotal)
                {
                    purchaseOrder.PaymentStatus = "Paid";
                }
                else if (purchaseOrder.PaidAmount > 0)
                {
                    purchaseOrder.PaymentStatus = "Partial";
                }
                else
                {
                    purchaseOrder.PaymentStatus = "Pending";
                }

                // Create finance entry for the payment
                var financeEntry = new Finance
                {
                    TransactionDate = DateTime.Now,
                    Amount = amount,
                    TransactionType = "Expense",
                    PaymentMethod = paymentMethod ?? "Cash",
                    PaymentMode = paymentMethod ?? "Cash",
                    Category = "Purchase Payment",
                    Description = $"Payment for Purchase Order #{purchaseOrder.PurchaseOrderNumber}",
                    Notes = notes ?? string.Empty,
                    ReferenceNumber = purchaseOrder.PurchaseOrderNumber,
                    CreatedBy = Environment.UserName ?? "System",
                    Currency = "INR",
                    Status = "Completed"
                };

                await _context.Finances.AddAsync(financeEntry);
                
                // Save all changes
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _logService.LogInformationAsync($"Recorded payment of ₹{amount:N2} for Purchase Order #{purchaseOrder.PurchaseOrderNumber}. Remaining: ₹{(purchaseOrder.GrandTotal - purchaseOrder.PaidAmount):N2}");
                
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                await _logService.LogErrorAsync($"Error recording payment: {ex.Message}", exception: ex);
                throw;
            }
        }

        // Generate purchase order number
        private async Task<string> GeneratePurchaseOrderNumber()
        {
            var year = DateTime.Now.Year.ToString();
            var month = DateTime.Now.Month.ToString("D2");
            var day = DateTime.Now.Day.ToString("D2");
            
            // Get the count of purchase orders created today
            var today = DateOnly.FromDateTime(DateTime.Now);
            var todaysOrderCount = await _context.PurchaseOrders
                .CountAsync(po => po.OrderDate == today);

            var nextNumber = todaysOrderCount + 1;
            
            // Generate unique number with retry logic
            string newPONumber;
            int attempt = 0;
            const int maxAttempts = 10;
            
            do
            {
                newPONumber = $"PO{year}{month}{day}{(nextNumber + attempt):D4}";
                var exists = await _context.PurchaseOrders
                    .AnyAsync(po => po.PurchaseOrderNumber == newPONumber);
                
                if (!exists)
                {
                    return newPONumber;
                }
                
                attempt++;
            } while (attempt < maxAttempts);

            // If all attempts failed, use timestamp
            var timestamp = DateTime.Now.ToString("HHmmss");
            return $"PO{year}{month}{day}{timestamp}";
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

        // Enhanced method for Item Received - Add to Stock
        public async Task<bool> MarkItemsReceived(int purchaseOrderId, List<int> receivedItemIds = null, string receivedBy = "System")
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var purchaseOrder = await GetPurchaseOrderById(purchaseOrderId);
                if (purchaseOrder == null) return false;

                var itemsToReceive = receivedItemIds?.Count > 0 
                    ? purchaseOrder.PurchaseOrderItems.Where(poi => receivedItemIds.Contains(poi.PurchaseOrderItemID)).ToList()
                    : purchaseOrder.PurchaseOrderItems.ToList();

                foreach (var item in itemsToReceive)
                {
                    if (item.IsFullyReceived) continue; // Skip already received items

                    // Mark item as received
                    item.ReceivedQuantity = item.Quantity;
                    item.ReceivedDate = DateTime.Now;
                    item.ReceivedBy = receivedBy;
                    item.Status = "Delivered";
                    item.IsAddedToStock = true;
                    item.StockAddedDate = DateTime.Now;

                    // Add individual items to stock with unique Tag IDs and Barcodes
                    await AddItemsToStock(item);
                }

                // Update purchase order status
                purchaseOrder.IsItemsReceived = purchaseOrder.AreAllItemsReceived;
                purchaseOrder.ItemsReceivedDate = purchaseOrder.IsItemsReceived ? DateTime.Now : null;
                purchaseOrder.ItemReceiptStatus = purchaseOrder.IsItemsReceived ? "Completed" : 
                    (purchaseOrder.TotalReceivedQuantity > 0 ? "Partial" : "Pending");
                
                purchaseOrder.Status = purchaseOrder.IsItemsReceived ? "Delivered" : "Partially Delivered";
                purchaseOrder.ActualDeliveryDate = purchaseOrder.IsItemsReceived ? DateOnly.FromDateTime(DateTime.Now) : null;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _logService.LogInformationAsync($"Items received for Purchase Order #{purchaseOrder.PurchaseOrderNumber}. Status: {purchaseOrder.ItemReceiptStatus}");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                await _logService.LogErrorAsync($"Error marking items as received: {ex.Message}", exception: ex);
                return false;
            }
        }

        // Enhanced method for Amount Paid - Create Finance Record
        public async Task<bool> RecordPurchaseOrderPayment(int purchaseOrderId, decimal paidAmount, string paymentMethod = "Cash", string notes = "")
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var purchaseOrder = await GetPurchaseOrderById(purchaseOrderId);
                if (purchaseOrder == null) return false;

                // Update purchase order payment details
                purchaseOrder.PaidAmount += paidAmount;
                purchaseOrder.PaymentMethod = paymentMethod;
                
                // Update payment status
                if (purchaseOrder.IsFullyPaid)
                {
                    purchaseOrder.PaymentStatus = "Paid";
                }
                else if (purchaseOrder.PaidAmount > 0)
                {
                    purchaseOrder.PaymentStatus = "Partial";
                }

                // Create Finance Record for the expense
                var financeEntry = new Finance
                {
                    TransactionDate = DateTime.Now,
                    Amount = paidAmount,
                    TransactionType = "Expense",
                    PaymentMethod = paymentMethod,
                    PaymentMode = paymentMethod,
                    Category = "Purchase",
                    Description = $"Payment for Purchase Order #{purchaseOrder.PurchaseOrderNumber}",
                    Notes = $"Purchase from {purchaseOrder.Supplier?.SupplierName ?? "Supplier"}. {notes}",
                    ReferenceNumber = purchaseOrder.PurchaseOrderNumber,
                    CreatedBy = purchaseOrder.CreatedBy,
                    Currency = "INR",
                    Status = "Completed"
                };

                await _context.Finances.AddAsync(financeEntry);
                
                // Link finance record to purchase order
                purchaseOrder.HasFinanceRecord = true;
                purchaseOrder.FinanceRecordID = financeEntry.FinanceID;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _logService.LogInformationAsync($"Payment of ₹{paidAmount:N2} recorded for Purchase Order #{purchaseOrder.PurchaseOrderNumber}. Payment Status: {purchaseOrder.PaymentStatus}");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                await _logService.LogErrorAsync($"Error recording payment: {ex.Message}", exception: ex);
                return false;
            }
        }

        // Helper method to add items to stock with individual tracking
        private async Task AddItemsToStock(PurchaseOrderItem item)
        {
            // Create or update stock record
            var existingStock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.ProductID == item.ProductID && 
                                         s.SupplierID == item.PurchaseOrder.SupplierID &&
                                         s.PurchaseOrderID == item.PurchaseOrderID);

            if (existingStock == null)
            {
                existingStock = new Stock
                {
                    ProductID = item.ProductID,
                    SupplierID = item.PurchaseOrder.SupplierID,
                    PurchaseOrderID = item.PurchaseOrderID,
                    Quantity = 0,
                    UnitCost = item.UnitCost,
                    Location = "Main Store",
                    Status = "Available",
                    AvailableCount = 0,
                    PurchaseOrderReceivedDate = DateTime.Now
                };
                await _context.Stocks.AddAsync(existingStock);
                await _context.SaveChangesAsync(); // Save to get Stock ID
            }

            // CRITICAL FIX: Add individual stock items with proper barcode generation
            for (int i = 0; i < (int)item.Quantity; i++)
            {
                var stockItem = new StockItem
                {
                    ProductID = item.ProductID,
                    UniqueTagID = await GenerateUniqueTagID(item.ProductID),
                    // Temporarily set barcode - will be generated after saving
                    Barcode = "PENDING", 
                    PurchaseCost = item.UnitCost,
                    SellingPrice = item.Product?.ProductPrice ?? 0,
                    Status = "Available",
                    Location = "Main Store",
                    PurchaseOrderID = item.PurchaseOrderID,
                    PurchaseOrderItemID = item.PurchaseOrderItemID,
                    PurchaseDate = DateTime.Now
                };

                await _context.StockItems.AddAsync(stockItem);
            }

            // Save to get StockItemIDs first
            await _context.SaveChangesAsync();
            
            // CRITICAL FIX: Generate and save barcodes after getting IDs
            var newStockItems = await _context.StockItems
                .Where(si => si.PurchaseOrderItemID == item.PurchaseOrderItemID && si.Barcode == "PENDING")
                .ToListAsync();
                
            foreach (var stockItem in newStockItems)
            {
                stockItem.Barcode = await _barcodeService.GenerateAndSaveStockItemBarcodeAsync(stockItem.StockItemID);
            }

            // Update stock totals
            existingStock.Quantity += item.Quantity;
            existingStock.AvailableCount += (int)item.Quantity;
            existingStock.LastUpdated = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        // Helper method to generate unique Tag ID
        private async Task<string> GenerateUniqueTagID(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            var productCode = product?.TagNumber?.Substring(0, Math.Min(3, product.TagNumber.Length)) ?? "PRD";
            
            string tagId;
            do
            {
                tagId = $"{productCode}{DateTime.Now:yyyyMMdd}{Random.Shared.Next(1000, 9999)}";
            }
            while (await _context.StockItems.AnyAsync(si => si.UniqueTagID == tagId));

            return tagId;
        }

        // Helper method to generate barcode
        // [REMOVED - Replaced with BarcodeService.GenerateStockItemBarcodeAsync]

        // Helper method to validate purchase order data before saving
        private void ValidatePurchaseOrderData(PurchaseOrder purchaseOrder)
        {
            if (purchaseOrder == null)
                throw new ArgumentNullException(nameof(purchaseOrder));

            if (purchaseOrder.SupplierID <= 0)
                throw new InvalidOperationException($"Invalid SupplierID: {purchaseOrder.SupplierID}");

            if (string.IsNullOrEmpty(purchaseOrder.PurchaseOrderNumber))
                throw new InvalidOperationException("PurchaseOrderNumber cannot be empty");

            if (string.IsNullOrEmpty(purchaseOrder.Status))
                throw new InvalidOperationException("Status cannot be empty");

            if (string.IsNullOrEmpty(purchaseOrder.PaymentMethod))
                throw new InvalidOperationException("PaymentMethod cannot be empty");

            if (string.IsNullOrEmpty(purchaseOrder.PaymentStatus))
                throw new InvalidOperationException("PaymentStatus cannot be empty");

            if (string.IsNullOrEmpty(purchaseOrder.ItemReceiptStatus))
                throw new InvalidOperationException("ItemReceiptStatus cannot be empty");

            if (string.IsNullOrEmpty(purchaseOrder.CreatedBy))
                throw new InvalidOperationException("CreatedBy cannot be empty");

            // Validate purchase order items
            if (purchaseOrder.PurchaseOrderItems != null)
            {
                foreach (var item in purchaseOrder.PurchaseOrderItems)
                {
                    if (item.ProductID <= 0)
                        throw new InvalidOperationException($"Invalid ProductID in item: {item.ProductID}");
                    
                    if (item.Quantity <= 0)
                        throw new InvalidOperationException($"Invalid Quantity in item: {item.Quantity}");
                    
                    if (item.UnitCost < 0)
                        throw new InvalidOperationException($"Invalid UnitCost in item: {item.UnitCost}");
                    
                    if (string.IsNullOrEmpty(item.Status))
                        throw new InvalidOperationException("Item Status cannot be empty");
                }
            }
        }

        // Debug method to check purchase order data
        public async Task<string> DebugPurchaseOrderAsync(int purchaseOrderId)
        {
            try
            {
                var purchaseOrder = await _context.PurchaseOrders
                    .Include(po => po.Supplier)
                    .Include(po => po.PurchaseOrderItems)
                        .ThenInclude(poi => poi.Product)
                    .FirstOrDefaultAsync(po => po.PurchaseOrderID == purchaseOrderId);
                
                if (purchaseOrder == null)
                    return $"Purchase Order {purchaseOrderId} not found";

                var debug = new System.Text.StringBuilder();
                debug.AppendLine($"Purchase Order Debug Info:");
                debug.AppendLine($"ID: {purchaseOrder.PurchaseOrderID}");
                debug.AppendLine($"Number: {purchaseOrder.PurchaseOrderNumber}");
                debug.AppendLine($"Supplier: {purchaseOrder.Supplier?.SupplierName ?? "NULL"}");
                debug.AppendLine($"Status: {purchaseOrder.Status}");
                debug.AppendLine($"Items Count: {purchaseOrder.PurchaseOrderItems.Count}");
                
                foreach (var item in purchaseOrder.PurchaseOrderItems)
                {
                    debug.AppendLine($"  Item: ProductID={item.ProductID}, Product={item.Product?.ProductName ?? "NULL"}, Qty={item.Quantity}, Cost={item.UnitCost}, Status={item.Status}");
                }
                
                return debug.ToString();
            }
            catch (Exception ex)
            {
                return $"Debug error: {ex.Message}";
            }
        }

        // Delete purchase order
        public async Task<bool> DeletePurchaseOrder(int purchaseOrderId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Load purchase order with all related data
                var purchaseOrder = await _context.PurchaseOrders
                    .Include(po => po.PurchaseOrderItems)
                    .FirstOrDefaultAsync(po => po.PurchaseOrderID == purchaseOrderId);
                
                if (purchaseOrder == null)
                {
                    throw new InvalidOperationException($"Purchase order with ID {purchaseOrderId} not found.");
                }

                // Check if purchase order can be deleted
                if (purchaseOrder.Status == "Delivered" || purchaseOrder.Status == "Partially Delivered")
                {
                    throw new InvalidOperationException("Cannot delete a purchase order that has been delivered or partially delivered.");
                }

                if (purchaseOrder.PaymentStatus == "Paid" || purchaseOrder.PaymentStatus == "Partial")
                {
                    throw new InvalidOperationException("Cannot delete a purchase order that has payments recorded.");
                }

                // Check if any stock items were added from this purchase order
                var stockItemsExist = await _context.StockItems
                    .AnyAsync(si => si.PurchaseOrderID == purchaseOrderId);

                if (stockItemsExist)
                {
                    throw new InvalidOperationException("Cannot delete a purchase order that has stock items added to inventory.");
                }

                // Remove purchase order items first
                _context.PurchaseOrderItems.RemoveRange(purchaseOrder.PurchaseOrderItems);
                
                // Remove purchase order
                _context.PurchaseOrders.Remove(purchaseOrder);
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Log successful deletion
                await _logService.LogInformationAsync($"Deleted Purchase Order #{purchaseOrder.PurchaseOrderNumber}");
                
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                await _logService.LogErrorAsync($"Error deleting purchase order: {ex.Message}", exception: ex);
                throw;
            }
        }
    }
}
