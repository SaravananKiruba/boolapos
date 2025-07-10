using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;

namespace Page_Navigation_App.Services
{
    /// <summary>
    /// Service to manage barcode generation, validation and scanning for the POS system.
    /// Centralizes all barcode operations for consistency across the application.
    /// </summary>
    public class BarcodeService
    {
        private readonly AppDbContext _context;
        private readonly LogService _logService;

        public BarcodeService(AppDbContext context, LogService logService)
        {
            _context = context;
            _logService = logService;
        }

        /// <summary>
        /// Generates a unique barcode for a product
        /// </summary>
        /// <param name="productId">ID of the product</param>
        /// <returns>A unique barcode string</returns>
        public async Task<string> GenerateProductBarcodeAsync(int productId)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                    throw new ArgumentException($"Product with ID {productId} not found");

                // Format: PR-{ProductID}-{YYYYMMDD}
                string barcode = $"PR-{productId:D4}-{DateTime.Now:yyyyMMdd}";
                
                // Update the product
                product.Barcode = barcode;
                await _context.SaveChangesAsync();
                
                await _logService.LogInformationAsync($"Generated product barcode: {barcode} for product {product.ProductName}");
                return barcode;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error generating product barcode: {ex.Message}", exception: ex);
                throw;
            }
        }

        /// <summary>
        /// Generates a unique barcode for an individual stock item and saves it
        /// </summary>
        /// <param name="stockItemId">ID of the stock item to update</param>
        /// <returns>A unique barcode string for the stock item</returns>
        public async Task<string> GenerateAndSaveStockItemBarcodeAsync(int stockItemId)
        {
            try
            {
                var stockItem = await _context.StockItems.FindAsync(stockItemId);
                if (stockItem == null)
                    throw new ArgumentException($"StockItem with ID {stockItemId} not found");

                string barcode;
                do
                {
                    // Format: ITM-{ProductID}-{YYYYMMDD}-{RandomNumber}
                    string timestamp = DateTime.Now.ToString("yyyyMMdd");
                    string uniquePart = new Random().Next(1000, 9999).ToString();
                    barcode = $"ITM-{stockItem.ProductID:D4}-{timestamp}-{uniquePart}";
                } while (await _context.StockItems.AnyAsync(si => si.Barcode == barcode));
                
                // CRITICAL FIX: Actually save the barcode to the database
                stockItem.Barcode = barcode;
                await _context.SaveChangesAsync();
                
                await _logService.LogInformationAsync($"Generated and saved stock item barcode: {barcode} for stock item ID {stockItemId}");
                return barcode;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error generating stock item barcode: {ex.Message}", exception: ex);
                throw;
            }
        }

        /// <summary>
        /// Generates a unique barcode for an individual stock item (legacy method)
        /// </summary>
        /// <param name="productId">ID of the product</param>
        /// <returns>A unique barcode string for the stock item</returns>
        public async Task<string> GenerateStockItemBarcodeAsync(int productId)
        {
            string barcode;
            do
            {
                // Format: ITM-{ProductID}-{YYYYMMDD}-{RandomNumber}
                string timestamp = DateTime.Now.ToString("yyyyMMdd");
                string uniquePart = new Random().Next(1000, 9999).ToString();
                barcode = $"ITM-{productId:D4}-{timestamp}-{uniquePart}";
            } while (await _context.StockItems.AnyAsync(si => si.Barcode == barcode));
            
            await _logService.LogInformationAsync($"Generated stock item barcode: {barcode} for product ID {productId}");
            return barcode;
        }

        /// <summary>
        /// Validates if a barcode is properly formatted and exists in the system
        /// </summary>
        /// <param name="barcode">The barcode to validate</param>
        /// <returns>True if the barcode is valid</returns>
        public async Task<bool> ValidateBarcodeAsync(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return false;

            // Check if it's a product barcode
            if (barcode.StartsWith("PR-"))
            {
                return await _context.Products.AnyAsync(p => p.Barcode == barcode);
            }
            
            // Check if it's a stock item barcode
            if (barcode.StartsWith("ITM-"))
            {
                return await _context.StockItems.AnyAsync(si => si.Barcode == barcode);
            }

            return false;
        }

        /// <summary>
        /// Looks up a product by barcode
        /// </summary>
        /// <param name="barcode">The product barcode</param>
        /// <returns>The product if found, null otherwise</returns>
        public async Task<Product> FindProductByBarcodeAsync(string barcode)
        {
            try
            {
                return await _context.Products
                    .FirstOrDefaultAsync(p => p.Barcode == barcode);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error finding product by barcode: {ex.Message}", exception: ex);
                return null;
            }
        }

        /// <summary>
        /// Looks up a stock item by barcode
        /// </summary>
        /// <param name="barcode">The stock item barcode</param>
        /// <returns>The stock item if found, null otherwise</returns>
        public async Task<StockItem> FindStockItemByBarcodeAsync(string barcode)
        {
            try
            {
                return await _context.StockItems
                    .Include(si => si.Product)
                    .FirstOrDefaultAsync(si => si.Barcode == barcode);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error finding stock item by barcode: {ex.Message}", exception: ex);
                return null;
            }
        }

        /// <summary>
        /// Simulates scanning a barcode with hardware
        /// To be replaced with actual hardware integration
        /// </summary>
        /// <returns>The scanned barcode</returns>
        public async Task<string> ScanBarcodeAsync()
        {
            // This is a placeholder for actual barcode scanner integration
            // In a real implementation, this would interact with scanner hardware
            await _logService.LogInformationAsync("Barcode scan simulated");
            return "SIMULATED-SCAN";
        }

        /// <summary>
        /// Tests if barcode scanning hardware is connected
        /// </summary>
        /// <returns>True if scanner is connected</returns>
        public async Task<bool> TestScannerConnectionAsync()
        {
            // This is a placeholder for actual hardware testing
            await _logService.LogInformationAsync("Testing barcode scanner connection");
            return true;
        }
    }
}
