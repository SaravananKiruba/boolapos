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

                // Format: MMYY-CATID-XXXX where:
                // MMYY = Month and Year
                // CATID = Category ID
                // XXXX = Sequential number
                
                string monthYear = DateTime.Now.ToString("MMyy");
                string categoryId = product.CategoryID.ToString("D2");
                
                // Find latest tag with same prefix
                string prefix = $"{monthYear}-{categoryId}-";
                var latestProduct = await _context.Products
                    .Where(p => p.TagNumber != null && p.TagNumber.StartsWith(prefix))
                    .OrderByDescending(p => p.TagNumber)
                    .FirstOrDefaultAsync();
                
                int sequence = 1;
                if (latestProduct != null && latestProduct.TagNumber.Length > prefix.Length)
                {
                    if (int.TryParse(latestProduct.TagNumber.Substring(prefix.Length), out int lastSequence))
                    {
                        sequence = lastSequence + 1;
                    }
                }
                
                string tagNumber = $"{prefix}{sequence:D4}";
                
                // Assign tag number to product
                product.TagNumber = tagNumber;
                product.Barcode = tagNumber; // Use same number for barcode
                
                await _context.SaveChangesAsync();
                await _logService.LogInformationAsync($"Tag {tagNumber} assigned to product {product.ProductName}");
                
                return tagNumber;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error generating tag number: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Looks up a product by its tag number
        /// </summary>
        public async Task<Product> FindByTagNumberAsync(string tagNumber)
        {
            try
            {
                return await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .FirstOrDefaultAsync(p => p.TagNumber == tagNumber);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error finding product by tag: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Generates and prints a barcode/tag label
        /// </summary>
        public async Task<bool> GenerateTagLabelAsync(int productId)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.ProductID == productId);
                    
                if (product == null)
                    return false;
                    
                // Ensure product has a tag number
                if (string.IsNullOrEmpty(product.TagNumber))
                {
                    await GenerateTagNumberAsync(productId);
                    // Refresh product data
                    product = await _context.Products
                        .Include(p => p.Category)
                        .FirstOrDefaultAsync(p => p.ProductID == productId);
                }
                
                // This would connect to a printer service
                // For now, just log it
                await _logService.LogInformationAsync($"Tag label generated for product {product.ProductName} with tag {product.TagNumber}");
                
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error generating tag label: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets a list of products with their RFID/Barcode tags
        /// </summary>
        public async Task<List<Product>> GetTaggedProductsAsync()
        {
            try
            {
                return await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .Where(p => !string.IsNullOrEmpty(p.TagNumber))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error getting tagged products: {ex.Message}");
                return new List<Product>();
            }
        }
    }
}