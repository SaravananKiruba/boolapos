using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Page_Navigation_App.Services
{
    public class StockService
    {
        private readonly AppDbContext _context;
        private readonly LogService _logService;

        public StockService(AppDbContext context, LogService logService)
        {
            _context = context;
            _logService = logService;
        }

        // Create stock entry
        public async Task<Stock> AddStock(Stock stock)
        {
            try
            {
                await _context.Stocks.AddAsync(stock);
                await _context.SaveChangesAsync();
                
                await _logService.LogInformationAsync($"Added stock: {stock.Quantity} units of Product ID {stock.ProductID}");
                return stock;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error adding stock: {ex.Message}", exception: ex);
                return null;
            }
        }

        // Get stock by product
        public async Task<IEnumerable<Stock>> GetStockByProduct(int productId)
        {
            return await _context.Stocks
                .Include(s => s.Product)
                .Include(s => s.Supplier)
                .Where(s => s.ProductID == productId)
                .ToListAsync();
        }

        // Get all stock
        public async Task<IEnumerable<Stock>> GetAllStock()
        {
            return await _context.Stocks
                .Include(s => s.Product)
                .Include(s => s.Supplier)
                .OrderBy(s => s.Product.ProductName)
                .ToListAsync();
        }

        // Get stock summary by product
        public async Task<Dictionary<int, decimal>> GetStockSummary()
        {
            return await _context.Stocks
                .GroupBy(s => s.ProductID)
                .ToDictionaryAsync(g => g.Key, g => g.Sum(s => s.Quantity));
        }

        // Update stock quantity (used for sales)
        public async Task<bool> UpdateStockQuantity(int productId, decimal quantityToReduce, string reason = "Sale")
        {
            try
            {
                var stockItems = await _context.Stocks
                    .Where(s => s.ProductID == productId && s.Status == "Available")
                    .OrderBy(s => s.LastUpdated) // FIFO approach
                    .ToListAsync();

                decimal remainingToReduce = quantityToReduce;

                foreach (var stockItem in stockItems)
                {
                    if (remainingToReduce <= 0) break;

                    if (stockItem.Quantity <= remainingToReduce)
                    {
                        remainingToReduce -= stockItem.Quantity;
                        stockItem.Quantity = 0;
                        stockItem.Status = "Sold";
                    }
                    else
                    {
                        stockItem.Quantity -= remainingToReduce;
                        remainingToReduce = 0;
                    }

                    stockItem.LastUpdated = DateTime.Now;
                }

                if (remainingToReduce > 0)
                {
                    await _logService.LogWarningAsync($"Insufficient stock for Product ID {productId}. Required: {quantityToReduce}, Available: {quantityToReduce - remainingToReduce}");
                    return false;
                }

                await _context.SaveChangesAsync();
                await _logService.LogInformationAsync($"Updated stock for Product ID {productId}: Reduced {quantityToReduce} units for {reason}");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error updating stock quantity: {ex.Message}", exception: ex);
                return false;
            }
        }

        // Check available quantity for a product
        public async Task<decimal> GetAvailableQuantity(int productId)
        {
            return await _context.Stocks
                .Where(s => s.ProductID == productId && s.Status == "Available")
                .SumAsync(s => s.Quantity);
        }

        // Get low stock products
        public async Task<IEnumerable<Product>> GetLowStockProducts()
        {
            var stockSummary = await GetStockSummary();
            var lowStockProductIds = new List<int>();

            foreach (var item in stockSummary)
            {
                var product = await _context.Products.FindAsync(item.Key);
                if (product != null && item.Value <= product.ReorderLevel)
                {
                    lowStockProductIds.Add(item.Key);
                }
            }

            return await _context.Products
                .Where(p => lowStockProductIds.Contains(p.ProductID))
                .ToListAsync();
        }

        // Add stock items with unique tags and barcodes
        public async Task<bool> AddStockItems(int productId, int quantity, decimal unitCost, int? purchaseOrderId = null)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null) return false;

                var stockItems = new List<StockItem>();

                for (int i = 0; i < quantity; i++)
                {
                    var stockItem = new StockItem
                    {
                        ProductID = productId,
                        UniqueTagID = GenerateUniqueTagID(product.MetalType, product.Purity),
                        Barcode = GenerateUniqueBarcode(product.ProductID),
                        PurchaseCost = unitCost,
                        SellingPrice = product.ProductPrice,
                        PurchaseOrderID = purchaseOrderId,
                        PurchaseDate = DateTime.Now,
                        HUID = product.HUID,
                        Status = "Available"
                    };

                    stockItems.Add(stockItem);
                }

                await _context.StockItems.AddRangeAsync(stockItems);

                // Also update the main stock table
                var existingStock = await _context.Stocks
                    .FirstOrDefaultAsync(s => s.ProductID == productId && s.PurchaseOrderID == purchaseOrderId);

                if (existingStock != null)
                {
                    existingStock.Quantity += quantity;
                    existingStock.LastUpdated = DateTime.Now;
                }
                else
                {
                    var newStock = new Stock
                    {
                        ProductID = productId,
                        SupplierID = product.SupplierID,
                        Quantity = quantity,
                        UnitCost = unitCost,
                        PurchaseOrderID = purchaseOrderId,
                        Status = "Available"
                    };
                    await _context.Stocks.AddAsync(newStock);
                }

                await _context.SaveChangesAsync();
                await _logService.LogInformationAsync($"Added {quantity} stock items for Product ID {productId} with individual tags and barcodes");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error adding stock items: {ex.Message}", exception: ex);
                return false;
            }
        }

        // Generate unique tag ID for jewelry items
        private string GenerateUniqueTagID(string metalType, string purity)
        {
            var metalCode = metalType.Substring(0, Math.Min(2, metalType.Length)).ToUpper();
            var purityCode = purity.Replace("k", "").Replace("K", "");
            var timestamp = DateTime.Now.ToString("yyMMddHHmmss");
            var random = new Random().Next(100, 999);
            
            return $"{metalCode}{purityCode}-{timestamp}-{random}";
        }

        // Generate unique barcode for individual items
        private string GenerateUniqueBarcode(int productId)
        {
            var timestamp = DateTime.Now.ToString("yyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            
            return $"ITM{productId:D4}{timestamp}{random}";
        }

        // Sell stock item (mark as sold)
        public async Task<bool> SellStockItem(string uniqueTagId, int orderId, int customerId)
        {
            try
            {
                var stockItem = await _context.StockItems
                    .FirstOrDefaultAsync(si => si.UniqueTagID == uniqueTagId && si.Status == "Available");

                if (stockItem == null) return false;

                stockItem.Status = "Sold";
                stockItem.OrderID = orderId;
                stockItem.CustomerID = customerId;
                stockItem.SaleDate = DateTime.Now;

                await _context.SaveChangesAsync();
                await _logService.LogInformationAsync($"Sold stock item {uniqueTagId} to customer {customerId}");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error selling stock item: {ex.Message}", exception: ex);
                return false;
            }
        }

        // Get available stock items for a product
        public async Task<IEnumerable<StockItem>> GetAvailableStockItems(int productId)
        {
            return await _context.StockItems
                .Include(si => si.Product)
                .Where(si => si.ProductID == productId && si.Status == "Available")
                .OrderBy(si => si.CreatedDate)
                .ToListAsync();
        }

        // Get stock item by tag ID or barcode
        public async Task<StockItem> GetStockItemByIdentifier(string identifier)
        {
            return await _context.StockItems
                .Include(si => si.Product)
                .Include(si => si.Customer)
                .FirstOrDefaultAsync(si => si.UniqueTagID == identifier || si.Barcode == identifier);
        }
    }
}
