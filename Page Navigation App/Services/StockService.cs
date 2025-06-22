using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Page_Navigation_App.Services
{
    public class StockService
    {
        private readonly AppDbContext _context;
        private readonly RateMasterService _rateService;
        private const int DEAD_STOCK_DAYS = 180; // 6 months

        public StockService(AppDbContext context, RateMasterService rateService)
        {
            _context = context;
            _rateService = rateService;
        }

        // Create
        public async Task<Stock> AddStock(Stock stock)
        {
            try
            {
                stock.AddedDate = DateTime.Now;
                stock.LastUpdated = DateTime.Now;
                
                // Validate the product exists
                var product = await _context.Products.FindAsync(stock.ProductID);
                if (product == null) return null;

                await _context.Stocks.AddAsync(stock);
                await _context.SaveChangesAsync();
                return stock;
            }
            catch
            {
                return null;
            }
        }

        // Read
        public async Task<Stock> GetStockById(int stockId)
        {
            return await _context.Stocks
                .Include(s => s.Product)
                .FirstOrDefaultAsync(s => s.StockID == stockId);
        }

        public async Task<IEnumerable<Stock>> GetAllStocks()
        {
            return await _context.Stocks
                .Include(s => s.Product)
                .OrderBy(s => s.Product.ProductName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Stock>> GetStockByProduct(int productId)
        {
            return await _context.Stocks
                .Include(s => s.Product)
                .Where(s => s.ProductID == productId)
                .OrderBy(s => s.Location)
                .ToListAsync();
        }

        public async Task<IEnumerable<Stock>> GetStockByLocation(string location)
        {
            return await _context.Stocks
                .Include(s => s.Product)
                .Where(s => s.Location == location)
                .OrderBy(s => s.Product.ProductName)
                .ToListAsync();
        }

        // Update
        public async Task<bool> UpdateStock(Stock stock)
        {
            try
            {
                var existingStock = await _context.Stocks.FindAsync(stock.StockID);
                if (existingStock == null) return false;

                stock.LastUpdated = DateTime.Now;
                _context.Entry(existingStock).CurrentValues.SetValues(stock);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateStockQuantity(int stockId, decimal newQuantity)
        {
            try
            {
                var stock = await _context.Stocks.FindAsync(stockId);
                if (stock == null) return false;

                stock.Quantity = newQuantity;
                stock.LastUpdated = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> TransferStock(
            int productId, 
            string fromLocation, 
            string toLocation, 
            decimal quantity)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var sourceStock = await _context.Stocks
                    .FirstOrDefaultAsync(s => s.ProductID == productId && 
                                            s.Location == fromLocation);

                if (sourceStock == null || sourceStock.Quantity < quantity)
                    return false;

                var destinationStock = await _context.Stocks
                    .FirstOrDefaultAsync(s => s.ProductID == productId && 
                                            s.Location == toLocation);

                // Update source stock
                sourceStock.Quantity -= quantity;
                sourceStock.LastUpdated = DateTime.Now;

                if (destinationStock == null)
                {
                    // Create new stock entry for destination
                    destinationStock = new Stock
                    {
                        ProductID = productId,
                        Location = toLocation,
                        Quantity = quantity,
                        AddedDate = DateTime.Now,
                        LastUpdated = DateTime.Now
                    };
                    await _context.Stocks.AddAsync(destinationStock);
                }
                else
                {
                    // Update existing destination stock
                    destinationStock.Quantity += quantity;
                    destinationStock.LastUpdated = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        // Delete
        public async Task<bool> DeleteStock(int stockId)
        {
            try
            {
                var stock = await _context.Stocks.FindAsync(stockId);
                if (stock == null) return false;

                _context.Stocks.Remove(stock);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Additional helper methods
        public async Task<IEnumerable<Stock>> GetDeadStock()
        {
            var cutoffDate = DateTime.Now.AddDays(-DEAD_STOCK_DAYS);
            return await _context.Stocks
                .Include(s => s.Product)
                .Where(s => s.LastUpdated <= cutoffDate && s.Quantity > 0)
                .OrderByDescending(s => s.LastUpdated)
                .ToListAsync();
        }

        public async Task<IEnumerable<Stock>> GetLowStock(decimal threshold)
        {
            return await _context.Stocks
                .Include(s => s.Product)
                .Where(s => s.Quantity <= threshold)
                .OrderBy(s => s.Quantity)
                .ToListAsync();
        }

        public async Task<Dictionary<string, decimal>> GetStockValueByLocation()
        {
            var stocks = await _context.Stocks
                .Include(s => s.Product)
                .Where(s => s.Quantity > 0)
                .ToListAsync();            return stocks
                .GroupBy(s => s.Location)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(s => s.Quantity * s.Product.ProductPrice)
                );
        }

        public async Task<decimal> GetTotalStockValue()
        {
            return await _context.Stocks
                .Include(s => s.Product)
                .Where(s => s.Quantity > 0)
                .SumAsync(s => s.Quantity * s.Product.ProductPrice);
        }

        public async Task<IEnumerable<Stock>> SearchStock(
            string searchTerm,
            string location = null,
            bool? isDeadStock = null)
        {
            var query = _context.Stocks
                .Include(s => s.Product)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(s => 
                    s.Product.ProductName.Contains(searchTerm) ||
                    s.Product.Barcode.Contains(searchTerm) ||
                    s.Location.Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(s => s.Location == location);
            }

            if (isDeadStock.HasValue)
            {
                var cutoffDate = DateTime.Now.AddDays(-DEAD_STOCK_DAYS);
                if (isDeadStock.Value)
                {
                    query = query.Where(s => s.LastUpdated <= cutoffDate && s.Quantity > 0);
                }
                else
                {
                    query = query.Where(s => s.LastUpdated > cutoffDate || s.Quantity == 0);
                }
            }

            return await query
                .OrderBy(s => s.Product.ProductName)
                .ThenBy(s => s.Location)
                .ToListAsync();
        }

        public async Task<bool> UpdateStockQuantity(
            int productId, 
            string location, 
            decimal quantityChange,
            bool isSale = false)
        {
            var stock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.ProductID == productId && 
                                        s.Location == location);

            if (stock == null) return false;

            stock.Quantity += quantityChange;
            stock.LastUpdated = DateTime.Now;
            
            if (isSale)
            {
                stock.LastSold = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<string>> GetAllLocations()
        {
            return await _context.Stocks
                .Select(s => s.Location)
                .Distinct()
                .ToListAsync();
        }

        public async Task<Dictionary<string, decimal>> GetStockValueByMetalType()
        {
            var stocks = await _context.Stocks
                .Include(s => s.Product)
                .Where(s => s.Quantity > 0)
                .ToListAsync();

            var result = new Dictionary<string, decimal>();
            foreach (var group in stocks.GroupBy(s => s.Product.MetalType))
            {
                decimal totalValue = 0;
                foreach (var stock in group)
                {
                    // Get current rate as a decimal without awaiting
                    var currentRate = _rateService.GetCurrentRate(
                        stock.Product.MetalType,
                        stock.Product.Purity);

                    // Calculate directly without awaiting
                    if (currentRate > 0)
                    {
                        totalValue += stock.Quantity * stock.Product.NetWeight * currentRate;
                    }
                }
                result[group.Key] = totalValue;
            }

            return result;
        }

        public async Task<Dictionary<string, decimal>> GetStockWeightByMetalType()
        {
            var stocks = await _context.Stocks
                .Include(s => s.Product)
                .Where(s => s.Quantity > 0)
                .ToListAsync();

            return stocks.GroupBy(s => s.Product.MetalType)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(s => s.Quantity * s.Product.NetWeight)
                );
        }

        public async Task<decimal> GetCurrentStockValue()
        {
            var stocks = await _context.Stocks
                .Include(s => s.Product)
                .Where(s => s.Quantity > 0)
                .ToListAsync();

            decimal totalValue = 0;
            foreach (var stock in stocks)
            {
                // Get the current rate without awaiting the decimal value
                var currentRate = _rateService.GetCurrentRate(
                    stock.Product.MetalType,
                    stock.Product.Purity);

                // Calculate all values directly since GetCurrentRate returns a decimal
                if (currentRate > 0)
                {
                    decimal metalValue = stock.Quantity * stock.Product.NetWeight * currentRate;
                    decimal makingValue = metalValue * (stock.Product.MakingCharges / 100);
                    decimal stoneValue = stock.Quantity * stock.Product.StoneValue;
                    totalValue += metalValue + makingValue + stoneValue;
                }
            }

            return totalValue;
        }

        public async Task<Dictionary<string, decimal>> GetMetalPurityBreakdown(string metalType)
        {
            var stocks = await _context.Stocks
                .Include(s => s.Product)
                .Where(s => s.Quantity > 0 && s.Product.MetalType == metalType)
                .ToListAsync();

            return stocks.GroupBy(s => s.Product.Purity)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(s => s.Quantity * s.Product.NetWeight)
                );
        }

        public async Task<IEnumerable<Stock>> GetHallmarkPendingStock()
        {
            return await _context.Stocks
                .Include(s => s.Product)
                .Where(s => s.Quantity > 0 && 
                           string.IsNullOrEmpty(s.Product.HallmarkNumber))
                .OrderBy(s => s.AddedDate)
                .ToListAsync();
        }

        public async Task<bool> UpdateHallmarkNumber(
            int stockId, 
            string hallmarkNumber)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var stock = await _context.Stocks
                    .Include(s => s.Product)
                    .FirstOrDefaultAsync(s => s.StockID == stockId);

                if (stock == null) return false;

                stock.Product.HallmarkNumber = hallmarkNumber;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<IEnumerable<Stock>> GetStockByCategoryValue(decimal minValue)
        {
            return await _context.Stocks
                .Include(s => s.Product)
                .Where(s => s.Quantity > 0 &&                           (s.Quantity * s.Product.ProductPrice) >= minValue)
                .OrderByDescending(s => s.Quantity * s.Product.ProductPrice)
                .ToListAsync();
        }

        public async Task<IEnumerable<Stock>> GetStockWithStoneDetails()
        {
            return await _context.Stocks
                .Include(s => s.Product)
                .Where(s => s.Quantity > 0 && 
                           s.Product.StoneWeight > 0)
                .OrderByDescending(s => s.Product.StoneValue)
                .ToListAsync();
        }        public async Task<bool> ReduceStock(int productId, decimal quantity, decimal unitPrice = 0, string referenceId = null, string transactionType = "Sale")
        {
            // Check if there's already an active transaction
            bool inExistingTransaction = _context.Database.CurrentTransaction != null;
            var transaction = inExistingTransaction ? null : await _context.Database.BeginTransactionAsync();
            
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null || product.StockQuantity < (int)quantity) return false;

                // Update product stock quantity
                product.StockQuantity -= (int)quantity;
                await _context.SaveChangesAsync();

                // Use product's price if not specified
                if (unitPrice <= 0)
                {
                    unitPrice = product.ProductPrice;
                }

                // Calculate total amount
                decimal totalAmount = quantity * unitPrice;

                // Add stock ledger entry
                var ledgerEntry = new StockLedger
                {
                    ProductID = productId,
                    TransactionDate = DateTime.Now,
                    Quantity = -quantity, // Negative for reduction
                    UnitPrice = unitPrice,
                    TotalAmount = totalAmount,
                    TransactionType = transactionType,
                    ReferenceID = referenceId,
                    Notes = $"Stock reduced by {quantity} units"
                };
                
                await _context.StockLedgers.AddAsync(ledgerEntry);
                await _context.SaveChangesAsync();
                
                // Only commit if we created the transaction
                if (transaction != null) {
                    await transaction.CommitAsync();
                }
                return true;
            }            catch
            {
                // Only rollback if we created the transaction
                if (transaction != null) {
                    await transaction.RollbackAsync();
                }
                return false;
            }
        }

        public async Task<bool> IncreaseStock(int productId, decimal quantity, decimal unitPrice = 0, string referenceId = null, string transactionType = "Purchase")
        {
            // Check if there's already an active transaction
            bool inExistingTransaction = _context.Database.CurrentTransaction != null;
            var transaction = inExistingTransaction ? null : await _context.Database.BeginTransactionAsync();
            
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null) return false;

                // Update product stock quantity
                product.StockQuantity += (int)quantity;
                await _context.SaveChangesAsync();

                // Use product's price if not specified
                if (unitPrice <= 0)
                {
                    unitPrice = product.ProductPrice;
                }

                // Calculate total amount
                decimal totalAmount = quantity * unitPrice;

                // Add stock ledger entry
                var ledgerEntry = new StockLedger
                {
                    ProductID = productId,
                    TransactionDate = DateTime.Now,
                    Quantity = quantity, // Positive for addition
                    UnitPrice = unitPrice,
                    TotalAmount = totalAmount,
                    TransactionType = transactionType,
                    ReferenceID = referenceId,
                    Notes = $"Stock increased by {quantity} units"
                };
                
                await _context.StockLedgers.AddAsync(ledgerEntry);
                await _context.SaveChangesAsync();
                
                // Only commit if we created the transaction
                if (transaction != null) {
                    await transaction.CommitAsync();
                }
                return true;
            }
            catch
            {
                // Only rollback if we created the transaction
                if (transaction != null) {
                    await transaction.RollbackAsync();
                }
                return false;
            }
        }

        // Add stock items with unique IDs when receiving inventory
        public async Task<List<StockItem>> AddStockItems(int productId, int stockId, int quantity)
        {
            var stockItems = new List<StockItem>();
            
            try
            {
                // Get the current count of stock items for this product to generate sequence numbers
                int currentCount = await _context.StockItems
                    .Where(si => si.ProductID == productId)
                    .CountAsync();

                // Create stock item entries for each individual item
                for (int i = 0; i < quantity; i++)
                {
                    var stockItem = new StockItem
                    {
                        ProductID = productId,
                        StockID = stockId,
                        StockItemCode = StockItem.GenerateStockItemCode(productId, currentCount + i + 1),
                        AddedDate = DateTime.Now,
                        Status = "Available"
                    };
                    
                    _context.StockItems.Add(stockItem);
                    stockItems.Add(stockItem);
                }
                
                await _context.SaveChangesAsync();
                return stockItems;
            }
            catch (Exception ex)
            {                // Log the exception
                await _context.LogEntries.AddAsync(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    LogLevel = "Error",
                    Message = $"Failed to add stock items: {ex.Message}",
                    Source = "StockService.AddStockItems"
                });
                await _context.SaveChangesAsync();
                return null;
            }
        }

        // Get stock items by product
        public async Task<IEnumerable<StockItem>> GetStockItemsByProduct(int productId, string status = null)
        {
            var query = _context.StockItems
                .Include(si => si.Product)
                .Include(si => si.Stock)
                .Where(si => si.ProductID == productId);
                
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(si => si.Status == status);
            }
            
            return await query.ToListAsync();
        }

        // Update stock item status
        public async Task<bool> UpdateStockItemStatus(string stockItemCode, string newStatus, int? orderId = null)
        {
            try
            {
                var stockItem = await _context.StockItems
                    .FirstOrDefaultAsync(si => si.StockItemCode == stockItemCode);
                    
                if (stockItem == null) return false;
                
                stockItem.Status = newStatus;
                
                if (newStatus == "Sold")
                {
                    stockItem.SoldDate = DateTime.Now;
                    stockItem.OrderID = orderId;
                }
                
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Decrease stock when order is created
        public async Task<bool> DecreaseStockForOrder(int productId, int quantity, int orderId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Get available stock items for this product
                var stockItems = await _context.StockItems
                    .Where(si => si.ProductID == productId && si.Status == "Available")
                    .Take(quantity)
                    .ToListAsync();
                    
                if (stockItems.Count < quantity)
                {
                    await transaction.RollbackAsync();
                    return false; // Not enough stock
                }
                
                // Mark the stock items as sold
                foreach (var item in stockItems)
                {
                    item.Status = "Sold";
                    item.SoldDate = DateTime.Now;
                    item.OrderID = orderId;
                }
                
                // Update the stock quantity
                var stockToUpdate = await _context.Stocks
                    .FirstOrDefaultAsync(s => s.ProductID == productId);
                    
                if (stockToUpdate != null)
                {
                    stockToUpdate.QuantityPurchased -= quantity;
                    stockToUpdate.LastUpdated = DateTime.Now;
                    stockToUpdate.LastSold = DateTime.Now;
                }
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        // Generate stock item status report
        public async Task<Dictionary<string, int>> GetStockItemStatusReport(int productId = 0)
        {
            var query = _context.StockItems.AsQueryable();
            
            if (productId > 0)
            {
                query = query.Where(si => si.ProductID == productId);
            }
            
            return await query
                .GroupBy(si => si.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);
        }

        // New method to add stock with purchase recording
        public async Task<Stock> AddStockWithPurchaseRecord(Stock stock)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Add the stock record first
                stock.AddedDate = DateTime.Now;
                stock.LastUpdated = DateTime.Now;
                
                // Validate the product exists
                var product = await _context.Products.FindAsync(stock.ProductID);
                if (product == null)
                {
                    await transaction.RollbackAsync();
                    return null;
                }

                await _context.Stocks.AddAsync(stock);
                await _context.SaveChangesAsync();
                
                // Generate individual StockItems
                await GenerateStockItemsForProduct(stock.ProductID, stock.StockID, stock.QuantityPurchased);

                await transaction.CommitAsync();
                return stock;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error adding stock with purchase record: {ex.Message}");
                return null;
            }
        }
        
        // Generate StockItems for a product
        private async Task<bool> GenerateStockItemsForProduct(int productId, int stockId, decimal quantity)
        {
            try
            {
                // Get the product to access its product code
                var product = await _context.Products.FindAsync(productId);
                if (product == null) return false;
                
                // Get the product code (using first 8 chars of product name if no code exists)
                string productCode = !string.IsNullOrEmpty(product.Barcode) 
                    ? product.Barcode 
                    : product.ProductName.Length > 8 
                        ? product.ProductName.Substring(0, 8).ToUpper().Replace(" ", "") 
                        : product.ProductName.ToUpper().Replace(" ", "");
                
                // Get current count of stock items for this product
                int currentCount = await _context.StockItems
                    .Where(si => si.ProductID == productId)
                    .CountAsync();
                
                // Create individual stock items based on quantity
                for (int i = 0; i < (int)quantity; i++)
                {                    // Format: ${productId}_${PRODUCTCODE}_${count:0001}
                    string itemCode = $"{productId}_{productCode}_{(currentCount + i + 1).ToString("0000")}";
                    
                    var stockItem = new StockItem
                    {
                        ProductID = productId,
                        StockID = stockId,
                        StockItemCode = itemCode,
                        Status = "Available",
                        AddedDate = DateTime.Now
                    };
                    
                    await _context.StockItems.AddAsync(stockItem);
                }
                
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating stock items: {ex.Message}");
                return false;
            }
        }
        
        // Create expense entry for purchase
        public async Task<Expense> CreateExpenseForPurchase(Stock stock, string createdBy)
        {
            try
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductID == stock.ProductID);
                    
                if (product == null) return null;
                
                var supplier = await _context.Suppliers
                    .FirstOrDefaultAsync(s => s.SupplierID == stock.SupplierID);
                
                var expense = new Expense
                {
                    Description = $"Purchase of {stock.QuantityPurchased} units of {product.ProductName}",
                    ExpenseDate = stock.PurchaseDate,
                    Amount = stock.TotalAmount,
                    Category = "Purchase",
                    PaymentMethod = stock.PaymentStatus == "Paid" ? "Cash" : "Credit",
                    ReferenceNumber = $"STK-{stock.StockID}",
                    Recipient = supplier?.SupplierName ?? "Unknown Supplier",
                    Notes = $"Stock purchase recorded on {DateTime.Now}. Created by {createdBy}"
                };
                
                await _context.Expenses.AddAsync(expense);
                await _context.SaveChangesAsync();
                
                return expense;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating expense for purchase: {ex.Message}");
                return null;
            }
        }
    }
}