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
                .ToListAsync();

            return stocks
                .GroupBy(s => s.Location)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(s => s.Quantity * s.Product.BasePrice)
                );
        }

        public async Task<decimal> GetTotalStockValue()
        {
            return await _context.Stocks
                .Include(s => s.Product)
                .Where(s => s.Quantity > 0)
                .SumAsync(s => s.Quantity * s.Product.BasePrice);
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
                    var currentRate = await _rateService.GetCurrentRate(
                        stock.Product.MetalType,
                        stock.Product.Purity);

                    if (currentRate != null)
                    {
                        totalValue += stock.Quantity * stock.Product.NetWeight * currentRate.Rate;
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
                var currentRate = await _rateService.GetCurrentRate(
                    stock.Product.MetalType,
                    stock.Product.Purity);

                if (currentRate != null)
                {
                    decimal metalValue = stock.Quantity * stock.Product.NetWeight * currentRate.Rate;
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
                    .ThenInclude(p => p.Category)
                .Where(s => s.Quantity > 0 && 
                           (s.Quantity * s.Product.BasePrice) >= minValue)
                .OrderByDescending(s => s.Quantity * s.Product.BasePrice)
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
        }

        public async Task<bool> ReduceStock(int productId, decimal quantity)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null || product.StockQuantity < quantity) return false;

                // Update product stock quantity
                product.StockQuantity -= quantity;
                await _context.SaveChangesAsync();

                // Add stock ledger entry
                var ledgerEntry = new StockLedger
                {
                    ProductID = productId,
                    TransactionDate = DateTime.Now,
                    Quantity = -quantity, // Negative for reduction
                    TransactionType = "Sale",
                    Notes = $"Stock reduced by {quantity} units"
                };
                
                await _context.StockLedgers.AddAsync(ledgerEntry);
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

        public async Task<bool> IncreaseStock(int productId, decimal quantity, string referenceId = null, string transactionType = "Purchase")
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null) return false;

                // Update product stock quantity
                product.StockQuantity += quantity;
                await _context.SaveChangesAsync();

                // Add stock ledger entry
                var ledgerEntry = new StockLedger
                {
                    ProductID = productId,
                    TransactionDate = DateTime.Now,
                    Quantity = quantity, // Positive for addition
                    TransactionType = transactionType,
                    ReferenceID = referenceId,
                    Notes = $"Stock increased by {quantity} units"
                };
                
                await _context.StockLedgers.AddAsync(ledgerEntry);
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
    }
}