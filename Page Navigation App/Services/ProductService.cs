using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Page_Navigation_App.Services
{
    public class ProductService
    {
        private readonly AppDbContext _context;

        public ProductService(AppDbContext context)
        {
            _context = context;
        }        // Create
        public async Task<Product> AddProduct(Product product)
        {
            try 
            {
                // Generate barcode
                string metalCode = product.MetalType.Substring(0, 2).ToUpper();
                string purityCode = product.Purity.Replace("k", "");
                string randomCode = new Random().Next(1000, 9999).ToString();
                product.Barcode = $"{metalCode}{purityCode}{randomCode}";

                product.IsActive = true;
                
                // Ensure SupplierID is set
                if (product.SupplierID <= 0)
                {
                    // Set a default supplier ID if available
                    var firstSupplier = await _context.Suppliers.FirstOrDefaultAsync();
                    if (firstSupplier != null)
                    {
                        product.SupplierID = firstSupplier.SupplierID;
                    }
                    else
                    {
                        throw new InvalidOperationException("No supplier available. Please add a supplier first.");
                    }
                }
                
                // Explicitly setting Supplier to null to avoid EF conflicts when adding
                product.Supplier = null;

                await _context.Products.AddAsync(product);
                await _context.SaveChangesAsync();
                return product;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding product: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return null;
            }
        }

        // Read
        public async Task<Product> GetProductById(int id)
        {
            return await _context.Products
                .Include(p => p.Supplier)
                .Include(p => p.Stocks)
                .FirstOrDefaultAsync(p => p.ProductID == id);
        }

        public async Task<Product> GetProductByBarcode(string barcode)
        {
            return await _context.Products
                .Include(p => p.Supplier)
                .Include(p => p.Stocks)
                .FirstOrDefaultAsync(p => p.Barcode == barcode);
        }

        public async Task<IEnumerable<Product>> GetAllProducts(bool includeInactive = false)
        {
            var query = _context.Products               
                .Include(p => p.Supplier)
                .Include(p => p.Stocks)
                .AsQueryable();

            if (!includeInactive)
                query = query.Where(p => p.IsActive);

            return await query.OrderBy(p => p.ProductName).ToListAsync();
        }

    public async Task<IEnumerable<Product>> SearchProducts(
            string searchTerm = null,
            string metalType = null,
            string purity = null)
        {
            var query = _context.Products
                .Include(p => p.Stocks)
                .Where(p => p.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p =>
                    p.ProductName.Contains(searchTerm) ||
                    p.Description.Contains(searchTerm) ||
                    p.Barcode.Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(metalType))
            {
                query = query.Where(p => p.MetalType == metalType);
            }

            if (!string.IsNullOrWhiteSpace(purity))
            {
                query = query.Where(p => p.Purity == purity);
            }

            return await query.ToListAsync();
        }        // Update
        public async Task<bool> UpdateProduct(Product product)
        {
            try
            {
                var existingProduct = await _context.Products.FindAsync(product.ProductID);
                if (existingProduct == null) return false;

                // Don't update barcode
                product.Barcode = existingProduct.Barcode;
                
                // Ensure SupplierID is set
                if (product.SupplierID <= 0)
                {
                    // Keep existing supplier if available
                    product.SupplierID = existingProduct.SupplierID;
                    
                    // If still not set, try to get a default
                    if (product.SupplierID <= 0)
                    {
                        var firstSupplier = await _context.Suppliers.FirstOrDefaultAsync();
                        if (firstSupplier != null)
                        {
                            product.SupplierID = firstSupplier.SupplierID;
                        }
                        else
                        {
                            throw new InvalidOperationException("No supplier available. Please add a supplier first.");
                        }
                    }
                }
                
                // Explicitly setting Supplier to null to avoid EF conflicts when updating
                product.Supplier = null;
                
                _context.Entry(existingProduct).CurrentValues.SetValues(product);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating product: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        // Delete (Soft Delete)
        public async Task<bool> DeleteProduct(int productId)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null) return false;

                product.IsActive = false;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Additional helper methods

        public async Task<Dictionary<string, decimal>> GetProductValueByMetal()
        {
            var products = await _context.Products
                .Include(p => p.Stocks)
                .Where(p => p.IsActive && p.Stocks.Any(s => s.Quantity > 0))
                .ToListAsync();

            return products
                .GroupBy(p => p.MetalType)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(p => p.Stocks.Sum(s => s.Quantity * p.BasePrice))
                );
        }

        public async Task<IEnumerable<Product>> GetProductsByPriceRange(
            decimal minPrice,
            decimal maxPrice)
        {
            return await _context.Products
                .Include(p => p.Stocks)
                .Where(p => p.BasePrice >= minPrice && p.BasePrice <= maxPrice && p.IsActive)
                .ToListAsync();
        }

    public async Task UpdateMakingCharges(
            decimal makingCharges)
        {
            var products = await _context.Products
                .Where(p => p.IsActive)
                .ToListAsync();

            foreach (var product in products)
            {
                product.MakingCharges = makingCharges;
            }

            await _context.SaveChangesAsync();
        }

    public async Task UpdateWastagePercentage(
            decimal wastagePercentage)
        {
            var products = await _context.Products
                .Where(p => p.IsActive)
                .ToListAsync();

            foreach (var product in products)
            {
                product.WastagePercentage = wastagePercentage;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Product>> FilterProducts(string searchTerm)
        {
            return await _context.Products
                .Include(p => p.Supplier)
                .Where(p => 
                    p.IsActive && (
                    p.ProductName.Contains(searchTerm) ||
                    p.Barcode.Contains(searchTerm) ||
                    p.Description.Contains(searchTerm)))
                .OrderBy(p => p.ProductName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByMetal(string metalType, string purity)
        {
            return await _context.Products
                .Where(p => p.MetalType == metalType && 
                           p.Purity == purity && 
                           p.IsActive)
                .ToListAsync();
        }
    }
}