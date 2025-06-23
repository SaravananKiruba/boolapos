using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Page_Navigation_App.Services
{
    /// <summary>
    /// Service that integrates stock handling with purchase orders and finance
    /// </summary>
    public class StockIntegrationService
    {
        private readonly AppDbContext _context;
        private readonly StockService _stockService;
        private readonly PurchaseOrderService _purchaseOrderService;
        private readonly FinanceService _financeService;
        private readonly StockLedgerService _stockLedgerService;
        private readonly LogService _logService;

        public StockIntegrationService(
            AppDbContext context,
            StockService stockService,
            PurchaseOrderService purchaseOrderService,
            FinanceService financeService,
            StockLedgerService stockLedgerService,
            LogService logService)
        {
            _context = context;
            _stockService = stockService;
            _purchaseOrderService = purchaseOrderService;
            _financeService = financeService;
            _stockLedgerService = stockLedgerService;
            _logService = logService;
        }

        /// <summary>
        /// Adds stock for a product with automatic creation of individual stock items
        /// and links to purchase order and finance
        /// </summary>
        public async Task<(Stock stock, PurchaseOrder purchaseOrder, List<StockItem> stockItems)> 
            AddStockWithItems(int productId, int supplierId, decimal quantity, decimal unitPrice, string invoiceNumber = null, string location = "Main")
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // 1. Create the stock entry
                var stock = new Stock
                {
                    ProductID = productId,
                    SupplierID = supplierId,
                    PurchaseDate = DateTime.Now,
                    QuantityPurchased = quantity,
                    PurchaseRate = unitPrice,
                    TotalAmount = quantity * unitPrice,
                    InvoiceNumber = invoiceNumber ?? $"INV-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                    PaymentStatus = "Pending",
                    Location = location ?? "Main",
                    AddedDate = DateTime.Now,
                    LastUpdated = DateTime.Now
                };
                
                await _context.Stocks.AddAsync(stock);
                await _context.SaveChangesAsync();
                
                // 2. Generate stock items
                var stockItems = new List<StockItem>();
                int itemCount = (int)Math.Ceiling(quantity);
                int existingCount = await _context.StockItems.CountAsync(si => si.ProductID == productId);
                
                for (int i = 0; i < itemCount; i++)
                {
                    var stockItem = new StockItem
                    {
                        ProductID = productId,
                        StockID = stock.StockID,
                        AddedDate = DateTime.Now,
                        Status = "Available",
                        StockItemCode = GenerateStockItemCode(productId, existingCount + i)
                    };
                    
                    await _context.StockItems.AddAsync(stockItem);
                    stockItems.Add(stockItem);
                }
                
                await _context.SaveChangesAsync();
                
                // 3. Create purchase order
                var product = await _context.Products.FindAsync(productId);
                var purchaseOrder = new PurchaseOrder
                {
                    SupplierID = supplierId,
                    PurchaseDate = DateTime.Now,
                    Status = "Completed",
                    TotalAmount = stock.TotalAmount,
                    PaidAmount = 0,
                    BalanceAmount = stock.TotalAmount,
                    PaymentStatus = "Pending",
                    PaymentMethod = "Cash",
                    PurchaseOrderNumber = $"PO-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}"
                };
                
                await _context.PurchaseOrders.AddAsync(purchaseOrder);
                await _context.SaveChangesAsync();
                
                // 4. Create purchase order item
                var purchaseItem = new PurchaseOrderItem
                {
                    PurchaseOrderID = purchaseOrder.PurchaseOrderID,
                    ProductID = productId,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    TotalPrice = stock.TotalAmount,
                    Notes = $"Added via stock module on {DateTime.Now}"
                };
                
                await _context.PurchaseOrderItems.AddAsync(purchaseItem);
                
                // 5. Create stock ledger entry
                var ledgerEntry = new StockLedger
                {
                    ProductID = productId,
                    TransactionDate = DateTime.Now,
                    TransactionType = "Purchase",
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    TotalAmount = stock.TotalAmount,
                    ReferenceID = purchaseOrder.PurchaseOrderID.ToString(),
                    ReferenceNumber = purchaseOrder.PurchaseOrderNumber,
                    Notes = "Purchased via stock module"
                };
                
                await _context.StockLedgers.AddAsync(ledgerEntry);
                await _context.SaveChangesAsync();
                
                await transaction.CommitAsync();
                await _logService.LogInformationAsync($"Added {quantity} units of product {productId} with {stockItems.Count} stock items and purchase order {purchaseOrder.PurchaseOrderNumber}");
                
                return (stock, purchaseOrder, stockItems);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                await _logService.LogErrorAsync($"Error adding stock with items: {ex.Message}");
                return (null, null, null);
            }
        }
        
        /// <summary>
        /// Records an expense for a purchase order
        /// </summary>
        public async Task<(bool success, Finance finance, Expense expense)> 
            RecordPurchaseExpense(int purchaseOrderId, string description = null)
        {
            try
            {
                // Get purchase order
                var purchaseOrder = await _context.PurchaseOrders
                    .Include(po => po.Supplier)
                    .FirstOrDefaultAsync(po => po.PurchaseOrderID == purchaseOrderId);
                
                if (purchaseOrder == null)
                {
                    await _logService.LogErrorAsync($"Purchase order {purchaseOrderId} not found");
                    return (false, null, null);
                }
                
                // Create finance entry
                var finance = new Finance
                {
                    TransactionDate = DateTime.Now,
                    TransactionType = "Expense",
                    Amount = purchaseOrder.TotalAmount,
                    PaymentMode = purchaseOrder.PaymentMethod,
                    Category = "Purchase",
                    Description = description ?? $"Purchase Order #{purchaseOrder.PurchaseOrderNumber}",
                    ReferenceNumber = purchaseOrder.PurchaseOrderNumber
                };
                
                await _context.Finances.AddAsync(finance);
                
                // Create expense entry
                var expense = new Expense
                {
                    Description = description ?? $"Purchase Order #{purchaseOrder.PurchaseOrderNumber}",
                    ExpenseDate = DateTime.Now,
                    Amount = purchaseOrder.TotalAmount,
                    Category = "Purchase",
                    PaymentMethod = purchaseOrder.PaymentMethod,
                    PurchaseOrderID = purchaseOrder.PurchaseOrderID,
                    Recipient = purchaseOrder.Supplier?.SupplierName,
                    ReferenceNumber = purchaseOrder.PurchaseOrderNumber
                };
                
                await _context.Expenses.AddAsync(expense);
                await _context.SaveChangesAsync();
                
                await _logService.LogInformationAsync($"Recorded expense for purchase order {purchaseOrder.PurchaseOrderNumber}: {purchaseOrder.TotalAmount}");
                return (true, finance, expense);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error recording purchase expense: {ex.Message}");
                return (false, null, null);
            }
        }
        
        /// <summary>
        /// Updates stock when an order is created
        /// </summary>
        public async Task<(bool success, List<StockItem> usedItems, Finance income)> 
            ProcessOrderWithStockAndFinance(Order order, List<OrderDetail> orderDetails)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var usedStockItems = new List<StockItem>();
                
                // Process each order detail to update stock
                foreach (var detail in orderDetails)
                {
                    // Find available stock items for this product
                    var stockItems = await _context.StockItems
                        .Where(si => si.ProductID == detail.ProductID && si.Status == "Available")
                        .OrderBy(si => si.AddedDate)
                        .Take((int)Math.Ceiling(detail.Quantity))
                        .ToListAsync();
                    
                    if (stockItems.Count < (int)Math.Ceiling(detail.Quantity))
                    {
                        await _logService.LogWarningAsync($"Not enough stock items for product {detail.ProductID}. Needed: {detail.Quantity}, Available: {stockItems.Count}");
                        return (false, null, null);
                    }
                    
                    // Update stock items
                    foreach (var stockItem in stockItems)
                    {
                        stockItem.Status = "Sold";
                        stockItem.SoldDate = DateTime.Now;
                        stockItem.OrderID = order.OrderID;
                        
                        usedStockItems.Add(stockItem);
                    }
                    
                    // Create stock ledger entry
                    var ledgerEntry = new StockLedger
                    {
                        ProductID = detail.ProductID,
                        TransactionDate = DateTime.Now,
                        TransactionType = "Sale",
                        Quantity = detail.Quantity * -1, // Negative for outgoing stock
                        UnitPrice = detail.UnitPrice,
                        TotalAmount = detail.TotalAmount,
                        ReferenceID = order.OrderID.ToString(),
                        ReferenceNumber = order.InvoiceNumber,
                        Notes = "Sold via order"
                    };
                    
                    await _context.StockLedgers.AddAsync(ledgerEntry);
                }
                
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
                    CustomerID = order.CustomerID
                };
                
                await _context.Finances.AddAsync(finance);
                await _context.SaveChangesAsync();
                
                await transaction.CommitAsync();
                await _logService.LogInformationAsync($"Processed order {order.OrderID} with {usedStockItems.Count} stock items used and recorded income");
                
                return (true, usedStockItems, finance);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                await _logService.LogErrorAsync($"Error processing order with stock and finance: {ex.Message}");
                return (false, null, null);
            }
        }
        
        // Helper method to generate a stock item code
        private string GenerateStockItemCode(int productId, int currentCount)
        {
            // Format: ProductID_PREFIX_XXXX
            string sequenceNumber = (currentCount + 1).ToString().PadLeft(4, '0');
            return $"{productId}_KAMJEW_{sequenceNumber}";
        }
    }
}
