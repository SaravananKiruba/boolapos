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
    /// Unified service to handle complete business workflows across the system
    /// This service coordinates the three main workflows:
    /// 1. Customer → Order → Transaction → Report → Stock
    /// 2. Supplier → Purchase Order → Transaction → Report → Stock
    /// 3. RateMaster → Product → Purchase Order → Stock
    /// </summary>
    public class WorkflowService
    {
        private readonly AppDbContext _context;
        private readonly LogService _logService;
        private readonly CustomerService _customerService;
        private readonly OrderService _orderService;
        private readonly PurchaseOrderService _purchaseOrderService;
        private readonly StockService _stockService;
        private readonly FinanceService _financeService;
        private readonly RateMasterService _rateService;
        private readonly BarcodeService _barcodeService;
        
        public WorkflowService(
            AppDbContext context,
            LogService logService,
            CustomerService customerService,
            OrderService orderService,
            PurchaseOrderService purchaseOrderService,
            StockService stockService,
            FinanceService financeService,
            RateMasterService rateService,
            BarcodeService barcodeService)
        {
            _context = context;
            _logService = logService;
            _customerService = customerService;
            _orderService = orderService;
            _purchaseOrderService = purchaseOrderService;
            _stockService = stockService;
            _financeService = financeService;
            _rateService = rateService;
            _barcodeService = barcodeService;
        }
        
        #region Workflow 1: Customer → Order → Transaction → Report → Stock
        
        /// <summary>
        /// Complete sales workflow handling all aspects from customer to finance
        /// </summary>
        public async Task<(bool Success, string Message, Order Order)> ProcessCompleteOrderAsync(
            int customerId, 
            List<OrderDetail> orderDetails,
            string paymentMethod,
            decimal discountAmount = 0,
            string notes = "")
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Step 1: Validate customer
                var customer = await _customerService.GetCustomerById(customerId);
                if (customer == null)
                    return (false, "Customer not found", null);
                
                // Step 2: Check stock availability for all items
                var stockAvailability = await _stockService.CheckStockAvailability(orderDetails);
                foreach (var item in orderDetails)
                {
                    if (!stockAvailability.TryGetValue(item.ProductID, out int available) || 
                        available < item.Quantity)
                    {
                        return (false, $"Insufficient stock for product ID {item.ProductID}. Available: {available}, Requested: {item.Quantity}", null);
                    }
                }
                
                // Step 3: Create order
                var order = new Order
                {
                    CustomerID = customerId,
                    OrderDate = DateOnly.FromDateTime(DateTime.Now),
                    Status = "Pending",
                    PaymentMethod = paymentMethod,
                    DiscountAmount = discountAmount,
                    Notes = notes
                };
                
                // Step 4: Process order and deduct stock
                var orderResult = await _orderService.CreateOrderWithStockValidation(order, orderDetails);
                if (!orderResult.Success)
                    return (false, orderResult.ErrorMessage, null);
                
                // Step 5: Create finance entry for income
                var financeEntry = new Finance
                {
                    TransactionDate = DateTime.Now,
                    TransactionType = "Income",
                    Amount = orderResult.Order.GrandTotal,
                    PaymentMethod = paymentMethod,
                    Category = "Sales",
                    Description = $"Sales Invoice #{orderResult.Order.InvoiceNumber}",
                    ReferenceNumber = orderResult.Order.InvoiceNumber,
                    CustomerID = customerId,
                    Status = "Completed"
                };
                
                // Use AddFinanceRecord instead of AddFinanceEntry
                _financeService.AddFinanceRecord(financeEntry);
                
                // Step 6: Log the transaction
                await _logService.LogInformationAsync($"Completed order workflow: Order #{orderResult.Order.InvoiceNumber} for Customer #{customerId}, Total: {orderResult.Order.GrandTotal}");
                
                await transaction.CommitAsync();
                return (true, "Order processed successfully", orderResult.Order);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                await _logService.LogErrorAsync($"Error processing order workflow: {ex.Message}", exception: ex);
                return (false, $"Error: {ex.Message}", null);
            }
        }
        
        /// <summary>
        /// Process an order with barcode scanning
        /// </summary>
        public async Task<(bool Success, string Message, OrderDetail Item)> ScanItemForOrderAsync(
            string barcode, 
            decimal quantity = 1)
        {
            try
            {
                // Step 1: Validate barcode
                if (!await _barcodeService.ValidateBarcodeAsync(barcode))
                    return (false, "Invalid barcode", null);
                
                // Step 2: Look up the product or stock item
                Product product = null;
                
                if (barcode.StartsWith("PR-"))
                {
                    // Product barcode
                    product = await _barcodeService.FindProductByBarcodeAsync(barcode);
                }
                else if (barcode.StartsWith("ITM-"))
                {
                    // Stock item barcode
                    var stockItem = await _barcodeService.FindStockItemByBarcodeAsync(barcode);
                    if (stockItem != null)
                    {
                        product = stockItem.Product;
                        // For individual stock items, quantity is always 1
                        quantity = 1;
                    }
                }
                
                if (product == null)
                    return (false, "Product not found for barcode", null);
                
                // Step 3: Create order detail
                var orderDetail = new OrderDetail
                {
                    ProductID = product.ProductID,
                    Quantity = quantity,
                    UnitPrice = product.ProductPrice,
                    TotalAmount = product.ProductPrice * quantity
                };
                
                return (true, "Item scanned successfully", orderDetail);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error scanning item: {ex.Message}", exception: ex);
                return (false, $"Error: {ex.Message}", null);
            }
        }
        
        #endregion
        
        #region Workflow 2: Supplier → Purchase Order → Transaction → Report → Stock
        
        /// <summary>
        /// Complete purchase workflow from supplier to stock
        /// </summary>
        public async Task<(bool Success, string Message, PurchaseOrder PurchaseOrder)> ProcessCompletePurchaseAsync(
            int supplierId,
            List<PurchaseOrderItem> items,
            string notes = "",
            bool autoReceiveItems = false,
            bool autoRecordPayment = false,
            string paymentMethod = "Bank Transfer")
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Step 1: Create purchase order
                var purchaseOrder = new PurchaseOrder
                {
                    SupplierID = supplierId,
                    OrderDate = DateOnly.FromDateTime(DateTime.Now),
                    Status = "Pending",
                    Notes = notes,
                    CreatedBy = "System",
                    PaymentMethod = paymentMethod,
                };
                
                // Step 2: Submit purchase order
                var createdPO = await _purchaseOrderService.CreatePurchaseOrder(purchaseOrder, items);
                
                // Step 3: Optionally receive items (auto-add to stock)
                if (autoReceiveItems)
                {
                    await _purchaseOrderService.MarkItemsReceived(
                        createdPO.PurchaseOrderID, 
                        null, 
                        "System");
                }
                
                // Step 4: Optionally record payment
                if (autoRecordPayment)
                {
                    await _purchaseOrderService.RecordPurchaseOrderPayment(
                        createdPO.PurchaseOrderID,
                        createdPO.GrandTotal,
                        paymentMethod,
                        "Auto-payment for purchase order");
                }
                
                await transaction.CommitAsync();
                return (true, "Purchase processed successfully", createdPO);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                await _logService.LogErrorAsync($"Error processing purchase workflow: {ex.Message}", exception: ex);
                return (false, $"Error: {ex.Message}", null);
            }
        }
        
        /// <summary>
        /// Process receiving goods with barcode scanning
        /// </summary>
        public async Task<(bool Success, string Message)> ScanItemForGoodsReceiptAsync(
            string barcode,
            int purchaseOrderId)
        {
            try
            {
                // Validate purchase order
                var purchaseOrder = await _purchaseOrderService.GetPurchaseOrderById(purchaseOrderId);
                if (purchaseOrder == null)
                    return (false, "Purchase order not found");
                
                // Find product from barcode
                var product = await _barcodeService.FindProductByBarcodeAsync(barcode);
                if (product == null)
                    return (false, "Product not found for barcode");
                
                // Check if product is in purchase order
                var poItem = purchaseOrder.PurchaseOrderItems
                    .FirstOrDefault(i => i.ProductID == product.ProductID);
                    
                if (poItem == null)
                    return (false, "Product not found in purchase order");
                
                // Mark single item as received
                if (poItem.ReceivedQuantity < poItem.Quantity)
                {
                    poItem.ReceivedQuantity += 1;
                    poItem.ReceivedDate = DateTime.Now;
                    poItem.ReceivedBy = "Scanner";
                    
                    if (poItem.ReceivedQuantity >= poItem.Quantity)
                    {
                        poItem.Status = "Delivered";
                        // IsFullyReceived is a computed property, no need to set it
                    }
                    
                    await _context.SaveChangesAsync();
                    
                    // Add to stock
                    await _stockService.AddStockItems(
                        product.ProductID, 
                        1, 
                        poItem.UnitCost, 
                        purchaseOrderId);
                        
                    return (true, $"Item {product.ProductName} received and added to stock");
                }
                else
                {
                    return (false, "All items of this product have already been received");
                }
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error scanning item for goods receipt: {ex.Message}", exception: ex);
                return (false, $"Error: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Workflow 3: RateMaster → Product → Purchase Order → Stock
        
        /// <summary>
        /// Update rates and propagate changes through the system
        /// </summary>
        public async Task<(bool Success, string Message, int UpdatedProducts)> UpdateRatesAndPropagateChangesAsync(
            string metalType,
            string purity,
            decimal newRate,
            string source = "Manual Update")
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Step 1: Add new rate
                var rate = new RateMaster
                {
                    MetalType = metalType,
                    Purity = purity,
                    Rate = newRate,
                    EffectiveDate = DateTime.Now,
                    IsActive = true,
                    Source = source,
                    EnteredBy = "System"
                };
                
                await _context.RateMaster.AddAsync(rate);
                await _context.SaveChangesAsync();
                
                // Step 2: Deactivate old rates for this metal and purity
                var oldRates = await _context.RateMaster
                    .Where(r => r.MetalType == metalType && 
                           r.Purity == purity && 
                           r.RateID != rate.RateID && 
                           r.IsActive)
                    .ToListAsync();
                           
                foreach (var oldRate in oldRates)
                {
                    oldRate.IsActive = false;
                    oldRate.ValidUntil = DateTime.Now;
                    oldRate.UpdatedBy = "System";
                }
                
                await _context.SaveChangesAsync();
                
                // Step 3: Update all product prices based on new rate
                int updatedProducts = await _rateService.UpdateProductPricesBasedOnCurrentRatesAsync();
                
                await _logService.LogInformationAsync($"Updated {metalType} {purity} rate to {newRate}, affected {updatedProducts} products");
                
                await transaction.CommitAsync();
                return (true, $"Rate updated successfully, {updatedProducts} products updated", updatedProducts);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                await _logService.LogErrorAsync($"Error updating rates: {ex.Message}", exception: ex);
                return (false, $"Error: {ex.Message}", 0);
            }
        }
        
        /// <summary>
        /// Get comprehensive inventory value based on current rates
        /// </summary>
        public async Task<(decimal CurrentValue, decimal PurchaseValue, decimal PotentialProfit)> GetInventoryValueAtCurrentRatesAsync()
        {
            try
            {
                // Step 1: Get all available stock items
                var stockItems = await _context.StockItems
                    .Include(si => si.Product)
                    .Where(si => si.Status == "Available")
                    .ToListAsync();
                
                // Step 2: Calculate values
                decimal purchaseValue = stockItems.Sum(si => si.PurchaseCost);
                decimal currentValue = 0;
                
                // Group by product to minimize rate lookups
                var productGroups = stockItems.GroupBy(si => si.ProductID).ToList();
                
                foreach (var group in productGroups)
                {
                    var product = group.First().Product;
                    
                    // Skip non-metal products
                    if (product == null || (product.MetalType != "Gold" && product.MetalType != "Silver" && product.MetalType != "Platinum"))
                        continue;
                    
                    // Get current rate
                    decimal currentRate = await _rateService.GetLatestRateAsync(product.MetalType, product.Purity);
                    if (currentRate <= 0) continue;
                    
                    // Calculate current value based on rate
                    foreach (var item in group)
                    {
                        var (_, updatedPrice) = await _rateService.CalculateEnhancedProductPriceAsync(
                            product.NetWeight,
                            product.MetalType,
                            product.Purity,
                            product.WastagePercentage,
                            product.MakingCharges,
                            product.HUID,
                            currentRate);
                            
                        currentValue += updatedPrice;
                    }
                }
                
                return (currentValue, purchaseValue, currentValue - purchaseValue);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error calculating inventory value: {ex.Message}", exception: ex);
                return (0, 0, 0);
            }
        }
        
        #endregion
    }
}
