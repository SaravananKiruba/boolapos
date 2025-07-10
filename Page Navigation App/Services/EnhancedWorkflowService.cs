using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;

namespace Page_Navigation_App.Services
{
    /// <summary>
    /// ENHANCED Workflow Service with FIXED implementations
    /// This service provides the CORRECT way to handle business workflows
    /// </summary>
    public class EnhancedWorkflowService
    {
        private readonly AppDbContext _context;
        private readonly LogService _logService;
        private readonly OrderService _orderService;
        private readonly PurchaseOrderService _purchaseOrderService;
        private readonly BarcodeService _barcodeService;
        private readonly FinanceService _financeService;

        public EnhancedWorkflowService(
            AppDbContext context,
            LogService logService,
            OrderService orderService,
            PurchaseOrderService purchaseOrderService,
            BarcodeService barcodeService,
            FinanceService financeService)
        {
            _context = context;
            _logService = logService;
            _orderService = orderService;
            _purchaseOrderService = purchaseOrderService;
            _barcodeService = barcodeService;
            _financeService = financeService;
        }

        #region SALES WORKFLOW - Customer → Order → Transaction → Stock

        /// <summary>
        /// FIXED: Complete sales workflow with proper stock item tracking
        /// </summary>
        public async Task<(bool Success, string Message, Order Order)> ProcessSaleWithBarcodeAsync(
            int customerId,
            List<string> barcodes,
            string paymentMethod = "Cash",
            decimal discountAmount = 0,
            string notes = "")
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Step 1: Validate customer
                var customer = await _context.Customers.FindAsync(customerId);
                if (customer == null)
                    return (false, "Customer not found", null);

                // Step 2: Validate and find stock items from barcodes
                var orderDetails = new List<OrderDetail>();
                
                foreach (var barcode in barcodes.Distinct())
                {
                    var stockItem = await _barcodeService.FindStockItemByBarcodeAsync(barcode);
                    if (stockItem == null)
                        return (false, $"Invalid barcode: {barcode}", null);
                        
                    if (stockItem.Status != "Available")
                        return (false, $"Item with barcode {barcode} is not available (Status: {stockItem.Status})", null);

                    var orderDetail = new OrderDetail
                    {
                        ProductID = stockItem.ProductID,
                        StockItemID = stockItem.StockItemID,
                        Quantity = 1, // Always 1 for individual item tracking
                        UnitPrice = stockItem.SellingPrice,
                        TotalAmount = stockItem.SellingPrice,
                        Notes = $"Barcode: {barcode}, Tag: {stockItem.UniqueTagID}"
                    };
                    
                    orderDetails.Add(orderDetail);
                }

                if (!orderDetails.Any())
                    return (false, "No valid items found", null);

                // Step 3: Create order
                var order = new Order
                {
                    CustomerID = customerId,
                    OrderDate = DateOnly.FromDateTime(DateTime.Now),
                    Status = "Completed",
                    PaymentMethod = paymentMethod,
                    DiscountAmount = discountAmount,
                    Notes = notes
                };

                // Step 4: Process order using FIXED OrderService
                var createdOrder = await _orderService.CreateOrder(order, orderDetails);

                await transaction.CommitAsync();
                
                await _logService.LogInformationAsync($"ENHANCED WORKFLOW: Completed sale for Customer #{customerId}, Order #{createdOrder.InvoiceNumber}, Items: {barcodes.Count}");
                
                return (true, $"Sale completed successfully. Invoice: {createdOrder.InvoiceNumber}", createdOrder);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                await _logService.LogErrorAsync($"ENHANCED WORKFLOW ERROR: {ex.Message}", exception: ex);
                return (false, $"Error: {ex.Message}", null);
            }
        }

        /// <summary>
        /// FIXED: Scan barcode for order entry with proper validation
        /// </summary>
        public async Task<(bool Success, string Message, StockItem StockItem)> ScanItemForSaleAsync(string barcode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(barcode))
                    return (false, "Barcode cannot be empty", null);

                if (!await _barcodeService.ValidateBarcodeAsync(barcode))
                    return (false, "Invalid barcode format", null);

                var stockItem = await _barcodeService.FindStockItemByBarcodeAsync(barcode);
                if (stockItem == null)
                    return (false, "Item not found", null);

                if (stockItem.Status != "Available")
                    return (false, $"Item is {stockItem.Status}, cannot be sold", null);

                await _logService.LogInformationAsync($"ENHANCED WORKFLOW: Scanned item - Barcode: {barcode}, Tag: {stockItem.UniqueTagID}, Product: {stockItem.Product?.ProductName}");
                
                return (true, "Item scanned successfully", stockItem);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"ENHANCED WORKFLOW SCAN ERROR: {ex.Message}", exception: ex);
                return (false, $"Scan error: {ex.Message}", null);
            }
        }

        #endregion

        #region PURCHASE WORKFLOW - Supplier → Purchase Order → Transaction → Stock

        /// <summary>
        /// FIXED: Complete purchase workflow with proper stock item creation
        /// </summary>
        public async Task<(bool Success, string Message, List<StockItem> CreatedItems)> ProcessPurchaseReceiptAsync(
            int purchaseOrderId,
            Dictionary<int, int> receivedQuantities, // ProductID -> Quantity
            string receivedBy = "System")
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Step 1: Get purchase order
                var purchaseOrder = await _context.PurchaseOrders
                    .Include(po => po.PurchaseOrderItems)
                        .ThenInclude(poi => poi.Product)
                    .FirstOrDefaultAsync(po => po.PurchaseOrderID == purchaseOrderId);
                    
                if (purchaseOrder == null)
                    return (false, "Purchase order not found", null);

                if (purchaseOrder.Status == "Delivered")
                    return (false, "Purchase order already fully delivered", null);

                var createdStockItems = new List<StockItem>();

                // Step 2: Process each received item
                foreach (var received in receivedQuantities)
                {
                    var productId = received.Key;
                    var quantity = received.Value;
                    
                    var poItem = purchaseOrder.PurchaseOrderItems
                        .FirstOrDefault(poi => poi.ProductID == productId);
                        
                    if (poItem == null)
                        return (false, $"Product ID {productId} not found in purchase order", null);

                    if (quantity > poItem.Quantity)
                        return (false, $"Cannot receive more than ordered quantity for Product ID {productId}", null);

                    // Create individual stock items
                    for (int i = 0; i < quantity; i++)
                    {
                        var stockItem = new StockItem
                        {
                            ProductID = productId,
                            UniqueTagID = await GenerateUniqueTagID(productId),
                            Barcode = "PENDING", // Will be generated after save
                            PurchaseCost = poItem.UnitCost,
                            SellingPrice = poItem.Product?.ProductPrice ?? 0,
                            Status = "Available",
                            Location = "Main Store",
                            PurchaseOrderID = purchaseOrderId,
                            PurchaseOrderItemID = poItem.PurchaseOrderItemID,
                            PurchaseDate = DateTime.Now,
                            HUID = poItem.Product?.HUID
                        };

                        await _context.StockItems.AddAsync(stockItem);
                        createdStockItems.Add(stockItem);
                    }

                    // Update purchase order item status
                    poItem.ReceivedQuantity = quantity;
                    poItem.Status = quantity >= poItem.Quantity ? "Delivered" : "Partially Delivered";
                }

                // Save to get StockItemIDs
                await _context.SaveChangesAsync();

                // Generate barcodes for all created items
                foreach (var stockItem in createdStockItems)
                {
                    stockItem.Barcode = await _barcodeService.GenerateAndSaveStockItemBarcodeAsync(stockItem.StockItemID);
                }

                // Update purchase order status
                var allItemsDelivered = purchaseOrder.PurchaseOrderItems.All(poi => poi.Status == "Delivered");
                purchaseOrder.Status = allItemsDelivered ? "Delivered" : "Partially Delivered";
                purchaseOrder.ItemReceiptStatus = purchaseOrder.Status;

                // Update or create aggregate stock records
                await UpdateAggregateStockFromItems(createdStockItems);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _logService.LogInformationAsync($"ENHANCED WORKFLOW: Received {createdStockItems.Count} items for PO #{purchaseOrder.PurchaseOrderNumber}");
                
                return (true, $"Successfully received {createdStockItems.Count} items with barcodes", createdStockItems);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                await _logService.LogErrorAsync($"ENHANCED WORKFLOW PURCHASE ERROR: {ex.Message}", exception: ex);
                return (false, $"Error: {ex.Message}", null);
            }
        }

        #endregion

        #region INVENTORY OPERATIONS

        /// <summary>
        /// Get complete stock item information with barcode
        /// </summary>
        public async Task<StockItem> GetStockItemDetailsAsync(string identifier)
        {
            return await _context.StockItems
                .Include(si => si.Product)
                .Include(si => si.Customer)
                .Include(si => si.Order)
                .Include(si => si.OrderDetail)
                .FirstOrDefaultAsync(si => 
                    si.Barcode == identifier || 
                    si.UniqueTagID == identifier);
        }

        /// <summary>
        /// Get all available stock items for a product
        /// </summary>
        public async Task<List<StockItem>> GetAvailableStockItemsAsync(int productId)
        {
            return await _context.StockItems
                .Include(si => si.Product)
                .Where(si => si.ProductID == productId && si.Status == "Available")
                .OrderBy(si => si.CreatedDate) // FIFO
                .ToListAsync();
        }

        /// <summary>
        /// Validate inventory consistency between Stock and StockItem tables
        /// </summary>
        public async Task<(bool IsConsistent, List<string> Issues)> ValidateInventoryConsistencyAsync()
        {
            var issues = new List<string>();
            
            var products = await _context.Products.ToListAsync();
            
            foreach (var product in products)
            {
                // Count from StockItems
                var availableCount = await _context.StockItems
                    .CountAsync(si => si.ProductID == product.ProductID && si.Status == "Available");
                    
                var soldCount = await _context.StockItems
                    .CountAsync(si => si.ProductID == product.ProductID && si.Status == "Sold");

                // Count from Stock aggregate
                var stocks = await _context.Stocks
                    .Where(s => s.ProductID == product.ProductID)
                    .ToListAsync();
                    
                var aggregateAvailable = stocks.Sum(s => s.AvailableCount);
                var aggregateSold = stocks.Sum(s => s.SoldCount);

                if (availableCount != aggregateAvailable)
                {
                    issues.Add($"Product {product.ProductName}: Available count mismatch - StockItems: {availableCount}, Aggregate: {aggregateAvailable}");
                }

                if (soldCount != aggregateSold)
                {
                    issues.Add($"Product {product.ProductName}: Sold count mismatch - StockItems: {soldCount}, Aggregate: {aggregateSold}");
                }
            }

            return (issues.Count == 0, issues);
        }

        #endregion

        #region PRIVATE HELPERS

        private async Task<string> GenerateUniqueTagID(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            var prefix = product?.MetalType?.Substring(0, Math.Min(2, product.MetalType.Length)).ToUpper() ?? "IT";
            
            string tagId;
            do
            {
                tagId = $"{prefix}{DateTime.Now:yyyyMMdd}{Random.Shared.Next(1000, 9999)}";
            }
            while (await _context.StockItems.AnyAsync(si => si.UniqueTagID == tagId));

            return tagId;
        }

        private async Task UpdateAggregateStockFromItems(List<StockItem> stockItems)
        {
            var groupedItems = stockItems.GroupBy(si => new { si.ProductID, si.PurchaseOrderID });

            foreach (var group in groupedItems)
            {
                var existingStock = await _context.Stocks
                    .FirstOrDefaultAsync(s => s.ProductID == group.Key.ProductID && 
                                            s.PurchaseOrderID == group.Key.PurchaseOrderID);

                if (existingStock == null)
                {
                    var firstItem = group.First();
                    existingStock = new Stock
                    {
                        ProductID = group.Key.ProductID,
                        SupplierID = firstItem.PurchaseOrder?.SupplierID ?? 0,
                        PurchaseOrderID = group.Key.PurchaseOrderID,
                        Quantity = 0,
                        UnitCost = firstItem.PurchaseCost,
                        Location = "Main Store",
                        Status = "Available",
                        AvailableCount = 0,
                        SoldCount = 0,
                        ReservedCount = 0
                    };
                    await _context.Stocks.AddAsync(existingStock);
                }

                existingStock.Quantity += group.Count();
                existingStock.AvailableCount += group.Count();
                existingStock.LastUpdated = DateTime.Now;
            }
        }

        #endregion
    }
}
