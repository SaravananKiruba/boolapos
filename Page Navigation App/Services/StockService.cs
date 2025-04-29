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
        private const int DEAD_STOCK_DAYS = 180; // 6 months

        public StockService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Stock> AddStock(Stock stock)
        {
            stock.AddedDate = DateTime.Now;
            stock.LastUpdated = DateTime.Now;
            await _context.Stocks.AddAsync(stock);
            await _context.SaveChangesAsync();
            return stock;
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
                        LastUpdated = DateTime.Now,
                        PurchasePrice = sourceStock.PurchasePrice
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
                throw;
            }
        }

        public async Task<IEnumerable<Stock>> GetDeadStock()
        {
            var cutoffDate = DateTime.Now.AddDays(-DEAD_STOCK_DAYS);
            return await _context.Stocks
                .Include(s => s.Product)
                .Where(s => s.AddedDate <= cutoffDate && 
                           s.LastSold == null &&
                           s.Quantity > 0)
                .ToListAsync();
        }

        public async Task<IEnumerable<Stock>> GetLowStock(decimal threshold)
        {
            return await _context.Stocks
                .Include(s => s.Product)
                .Where(s => s.Quantity <= threshold)
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
            var stocks = await _context.Stocks
                .Include(s => s.Product)
                .Where(s => s.Quantity > 0)
                .ToListAsync();

            return stocks.Sum(s => s.Quantity * s.Product.BasePrice);
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
                    s.Product.Description.Contains(searchTerm) ||
                    s.Product.Barcode.Contains(searchTerm));
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
                    query = query.Where(s => 
                        s.AddedDate <= cutoffDate && 
                        s.LastSold == null &&
                        s.Quantity > 0);
                }
                else
                {
                    query = query.Where(s => 
                        s.AddedDate > cutoffDate || 
                        s.LastSold != null);
                }
            }

            return await query.ToListAsync();
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
    }
}