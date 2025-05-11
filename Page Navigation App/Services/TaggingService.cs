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
    /// Service to manage product tagging (barcode/RFID) for inventory tracking
    /// </summary>
    public class TaggingService
    {
        private readonly AppDbContext _context;
        private readonly LogService _logService;

        public TaggingService(AppDbContext context, LogService logService)
        {
            _context = context;
            _logService = logService;
        }

        /// <summary>
        /// Generates a unique tag number for a product
        /// </summary>
        public async Task<string> GenerateTagNumberAsync(int productId)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                    return null;

                // Format: MMYY-PROD-XXXX where:
                // MMYY = Month and Year
                // PROD = Product identifier
                // XXXX = Sequential number
                
                string monthYear = DateTime.Now.ToString("MMyy");
                string prodId = product.ProductID.ToString("D3");
                
                // Find latest tag with same prefix
                string prefix = $"{monthYear}-{prodId}-";
                var latestProduct = await _context.Products
                    .Where(p => p.TagNumber != null && p.TagNumber.StartsWith(prefix))
                    .OrderByDescending(p => p.TagNumber)
                    .FirstOrDefaultAsync();
                
                int sequence = 1;
                if (latestProduct != null && latestProduct.TagNumber.Length > prefix.Length)
                {
                    string sequencePart = latestProduct.TagNumber.Substring(prefix.Length);
                    int.TryParse(sequencePart, out sequence);
                    sequence++;
                }
                
                string tagNumber = $"{prefix}{sequence:D4}";
                
                // Update product with tag number
                product.TagNumber = tagNumber;
                await _context.SaveChangesAsync();
                
                await _logService.LogInfoAsync($"Generated tag number {tagNumber} for product ID {productId}");
                return tagNumber;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error generating tag number: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets product information by tag number
        /// </summary>
        public async Task<Product> GetProductByTagAsync(string tagNumber)
        {
            try
            {
                return await _context.Products
                    .Include(p => p.Supplier)
                    .FirstOrDefaultAsync(p => p.TagNumber == tagNumber);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error retrieving product by tag: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Prints tag for product (simulated)
        /// </summary>
        public async Task<bool> PrintTagAsync(int productId)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Supplier)
                    .FirstOrDefaultAsync(p => p.ProductID == productId);
                
                if (product == null)
                    return false;
                    
                if (string.IsNullOrEmpty(product.TagNumber))
                {
                    product.TagNumber = await GenerateTagNumberAsync(productId);
                }
                
                // Refresh product data
                product = await _context.Products
                    .Include(p => p.Supplier)
                    .FirstOrDefaultAsync(p => p.ProductID == productId);
                
                // Simulate tag printing here (in a real app would connect to a printer)
                await _logService.LogInfoAsync($"Tag printed for {product.ProductName}, Tag: {product.TagNumber}");
                
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error printing tag: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Verifies tag authenticity
        /// </summary>
        public async Task<bool> VerifyTagAsync(string tagNumber)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Supplier)
                    .FirstOrDefaultAsync(p => p.TagNumber == tagNumber);
                
                return product != null;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error verifying tag: {ex.Message}");
                return false;
            }
        }
    }
}