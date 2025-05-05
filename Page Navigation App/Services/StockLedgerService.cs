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
    /// Service to manage inventory stock movements and ledger entries
    /// </summary>
    public class StockLedgerService
    {
        private readonly AppDbContext _context;
        private readonly LogService _logService;

        public StockLedgerService(AppDbContext context, LogService logService)
        {
            _context = context;
            _logService = logService;
        }

        /// <summary>
        /// Add a new stock ledger entry for inventory movement
        /// </summary>
        public async Task<bool> AddLedgerEntryAsync(StockLedger ledgerEntry)
        {
            try
            {
                // Set default date if not provided
                if (ledgerEntry.TransactionDate == default)
                {
                    ledgerEntry.TransactionDate = DateTime.Now;
                }

                // Validate required fields
                if (ledgerEntry.ProductID <= 0 || string.IsNullOrEmpty(ledgerEntry.TransactionType))
                {
                    await _logService.LogErrorAsync("Invalid stock ledger entry: Missing required fields");
                    return false;
                }

                // Add the ledger entry
                await _context.StockLedgers.AddAsync(ledgerEntry);
                await _context.SaveChangesAsync();

                await _logService.LogInformationAsync($"Stock ledger entry added for product {ledgerEntry.ProductID}: {ledgerEntry.TransactionType} {ledgerEntry.Quantity}");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error adding stock ledger entry: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Record stock receipt from supplier
        /// </summary>
        public async Task<bool> RecordStockReceiptAsync(
            int productId, 
            int supplierId, 
            decimal quantity, 
            string referenceNumber, 
            decimal costPrice = 0,
            string notes = null)
        {
            try
            {
                // Validate product exists
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    await _logService.LogErrorAsync($"Product not found with ID: {productId}");
                    return false;
                }

                // Update product stock
                product.StockQuantity += (int)quantity;

                // Create ledger entry
                var ledgerEntry = new StockLedger
                {
                    ProductID = productId,
                    TransactionDate = DateTime.Now,
                    TransactionType = "Receipt",
                    Quantity = quantity,
                    ReferenceNumber = referenceNumber,
                    SupplierID = supplierId,
                    UnitCost = costPrice,
                    Notes = notes
                };

                await _context.StockLedgers.AddAsync(ledgerEntry);
                await _context.SaveChangesAsync();

                await _logService.LogInformationAsync($"Stock receipt recorded for product {productId}: {quantity} units");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error recording stock receipt: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Record stock sale
        /// </summary>
        public async Task<bool> RecordStockSaleAsync(
            int productId, 
            int orderId, 
            decimal quantity, 
            decimal salePrice)
        {
            try
            {
                // Validate product exists
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    await _logService.LogErrorAsync($"Product not found with ID: {productId}");
                    return false;
                }

                // Check if sufficient stock is available
                if (product.StockQuantity < quantity)
                {
                    await _logService.LogErrorAsync($"Insufficient stock for product {productId}: Available {product.StockQuantity}, Requested {quantity}");
                    return false;
                }

                // Update product stock
                product.StockQuantity -= (int)quantity;

                // Create ledger entry
                var ledgerEntry = new StockLedger
                {
                    ProductID = productId,
                    TransactionDate = DateTime.Now,
                    TransactionType = "Sale",
                    Quantity = -quantity, // Negative quantity for sales
                    ReferenceNumber = $"ORD-{orderId}",
                    OrderID = orderId,
                    UnitPrice = salePrice
                };

                await _context.StockLedgers.AddAsync(ledgerEntry);
                await _context.SaveChangesAsync();

                await _logService.LogInformationAsync($"Stock sale recorded for product {productId}: {quantity} units");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error recording stock sale: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Record stock return from customer
        /// </summary>
        public async Task<bool> RecordStockReturnAsync(
            int productId, 
            int orderId, 
            decimal quantity, 
            string referenceNumber,
            string notes = null)
        {
            try
            {
                // Validate product exists
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    await _logService.LogErrorAsync($"Product not found with ID: {productId}");
                    return false;
                }

                // Update product stock
                product.StockQuantity += (int)quantity;

                // Create ledger entry
                var ledgerEntry = new StockLedger
                {
                    ProductID = productId,
                    TransactionDate = DateTime.Now,
                    TransactionType = "Return",
                    Quantity = quantity,
                    ReferenceNumber = referenceNumber,
                    OrderID = orderId,
                    Notes = notes
                };

                await _context.StockLedgers.AddAsync(ledgerEntry);
                await _context.SaveChangesAsync();

                await _logService.LogInformationAsync($"Stock return recorded for product {productId}: {quantity} units");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error recording stock return: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Record stock adjustment (loss, damage, etc.)
        /// </summary>
        public async Task<bool> RecordStockAdjustmentAsync(
            int productId, 
            decimal quantity, 
            string reason,
            string referenceNumber = null,
            string notes = null)
        {
            try
            {
                // Validate product exists
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    await _logService.LogErrorAsync($"Product not found with ID: {productId}");
                    return false;
                }

                // Update product stock
                int previousStock = product.StockQuantity;
                product.StockQuantity += (int)quantity; // Can be negative for reduction

                // Create ledger entry
                var ledgerEntry = new StockLedger
                {
                    ProductID = productId,
                    TransactionDate = DateTime.Now,
                    TransactionType = "Adjustment",
                    Quantity = quantity,
                    ReferenceNumber = referenceNumber ?? $"ADJ-{DateTime.Now:yyyyMMddHHmmss}",
                    Notes = $"Reason: {reason}, Previous Stock: {previousStock}, {notes}"
                };

                await _context.StockLedgers.AddAsync(ledgerEntry);
                await _context.SaveChangesAsync();

                await _logService.LogInformationAsync($"Stock adjustment recorded for product {productId}: {quantity} units ({reason})");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error recording stock adjustment: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get stock ledger entries for a product within a date range
        /// </summary>
        public async Task<List<StockLedger>> GetProductLedgerAsync(
            int productId, 
            DateTime? fromDate = null, 
            DateTime? toDate = null)
        {
            try
            {
                var query = _context.StockLedgers
                    .Where(sl => sl.ProductID == productId);

                if (fromDate.HasValue)
                {
                    query = query.Where(sl => sl.TransactionDate >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(sl => sl.TransactionDate <= toDate.Value);
                }

                return await query
                    .OrderByDescending(sl => sl.TransactionDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error getting product ledger: {ex.Message}");
                return new List<StockLedger>();
            }
        }

        /// <summary>
        /// Calculate current inventory value
        /// </summary>
        public async Task<decimal> GetTotalInventoryValueAsync()
        {
            try
            {
                return await _context.Products
                    .SumAsync(p => p.FinalPrice * p.StockQuantity);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error calculating inventory value: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Get low stock products (below reorder level)
        /// </summary>
        public async Task<List<Product>> GetLowStockProductsAsync()
        {
            try
            {
                return await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .Where(p => p.StockQuantity <= p.ReorderLevel && p.IsActive)
                    .OrderBy(p => p.StockQuantity)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error getting low stock products: {ex.Message}");
                return new List<Product>();
            }
        }
        
        /// <summary>
        /// Transfer stock between locations (if multi-location inventory is used)
        /// </summary>
        public async Task<bool> TransferStockAsync(
            int productId, 
            string fromLocation, 
            string toLocation, 
            decimal quantity,
            string referenceNumber = null,
            string notes = null)
        {
            try
            {
                // Validate product exists
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    await _logService.LogErrorAsync($"Product not found with ID: {productId}");
                    return false;
                }

                // For a full multi-location system, you would update location-specific stock quantities
                // Here we just record the transfer in the ledger

                // Create ledger entry for transfer out
                var transferOutEntry = new StockLedger
                {
                    ProductID = productId,
                    TransactionDate = DateTime.Now,
                    TransactionType = "Transfer Out",
                    Quantity = -quantity,
                    ReferenceNumber = referenceNumber ?? $"TRF-{DateTime.Now:yyyyMMddHHmmss}",
                    Notes = $"From: {fromLocation}, To: {toLocation}, {notes}"
                };

                // Create ledger entry for transfer in
                var transferInEntry = new StockLedger
                {
                    ProductID = productId,
                    TransactionDate = DateTime.Now,
                    TransactionType = "Transfer In",
                    Quantity = quantity,
                    ReferenceNumber = referenceNumber ?? $"TRF-{DateTime.Now:yyyyMMddHHmmss}",
                    Notes = $"From: {fromLocation}, To: {toLocation}, {notes}"
                };

                await _context.StockLedgers.AddAsync(transferOutEntry);
                await _context.StockLedgers.AddAsync(transferInEntry);
                await _context.SaveChangesAsync();

                await _logService.LogInformationAsync($"Stock transfer recorded for product {productId}: {quantity} units from {fromLocation} to {toLocation}");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error recording stock transfer: {ex.Message}");
                return false;
            }
        }
    }
}