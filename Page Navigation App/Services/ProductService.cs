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
        }

        // Create
        public async Task<Product> AddProduct(Product product)
        {
            try 
            {
                // Generate barcode: MMPPWWWW where:
                // MM = Metal type code (GO=Gold, SI=Silver, PL=Platinum)
                // PP = Purity code (18=18k, 22=22k, 24=24k)
                // WWWW = Random number
                string metalCode = product.MetalType.Substring(0, 2).ToUpper();
                string purityCode = product.Purity.Replace("k", "");
                string randomCode = new Random().Next(1000, 9999).ToString();
                product.Barcode = $"{metalCode}{purityCode}{randomCode}";

                product.IsActive = true;

                await _context.Products.AddAsync(product);
                await _context.SaveChangesAsync();
                return product;
            }
            catch
            {
                return null;
            }
        }

        // Read
        public async Task<Product> GetProductById(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Subcategory)
                .Include(p => p.Supplier)
                .Include(p => p.Stocks)
                .FirstOrDefaultAsync(p => p.ProductID == id);
        }

        public async Task<Product> GetProductByBarcode(string barcode)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Subcategory)
                .Include(p => p.Supplier)
                .Include(p => p.Stocks)
                .FirstOrDefaultAsync(p => p.Barcode == barcode);
        }

        public async Task<IEnumerable<Product>> GetAllProducts(bool includeInactive = false)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Subcategory)
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
            string purity = null,
            int? categoryId = null,
            int? subcategoryId = null)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Subcategory)
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

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryID == categoryId.Value);
            }

            if (subcategoryId.HasValue)
            {
                query = query.Where(p => p.SubcategoryID == subcategoryId.Value);
            }

            return await query.ToListAsync();
        }

        // Update
        public async Task<bool> UpdateProduct(Product product)
        {
            try
            {
                var existingProduct = await _context.Products.FindAsync(product.ProductID);
                if (existingProduct == null) return false;

                // Don't update barcode
                product.Barcode = existingProduct.Barcode;
                
                _context.Entry(existingProduct).CurrentValues.SetValues(product);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
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
        public async Task<Dictionary<string, decimal>> GetProductValueByCategory()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Stocks)
                .Where(p => p.IsActive && p.Stocks.Any(s => s.Quantity > 0))
                .ToListAsync();

            return products
                .GroupBy(p => p.Category.CategoryName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(p => p.Stocks.Sum(s => s.Quantity * p.BasePrice))
                );
        }

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
                .Include(p => p.Category)
                .Include(p => p.Subcategory)
                .Include(p => p.Stocks)
                .Where(p => p.BasePrice >= minPrice && p.BasePrice <= maxPrice && p.IsActive)
                .ToListAsync();
        }

        public async Task UpdateMakingCharges(
            int categoryId,
            decimal makingCharges,
            bool applyToSubcategories = false)
        {
            var products = await _context.Products
                .Where(p => p.CategoryID == categoryId && p.IsActive)
                .ToListAsync();

            foreach (var product in products)
            {
                product.MakingCharges = makingCharges;
            }

            if (applyToSubcategories)
            {
                var subcategoryProducts = await _context.Products
                    .Where(p => p.Subcategory.CategoryID == categoryId && p.IsActive)
                    .ToListAsync();

                foreach (var product in subcategoryProducts)
                {
                    product.MakingCharges = makingCharges;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateWastagePercentage(
            int categoryId,
            decimal wastagePercentage,
            bool applyToSubcategories = false)
        {
            var products = await _context.Products
                .Where(p => p.CategoryID == categoryId && p.IsActive)
                .ToListAsync();

            foreach (var product in products)
            {
                product.WastagePercentage = wastagePercentage;
            }

            if (applyToSubcategories)
            {
                var subcategoryProducts = await _context.Products
                    .Where(p => p.Subcategory.CategoryID == categoryId && p.IsActive)
                    .ToListAsync();

                foreach (var product in subcategoryProducts)
                {
                    product.WastagePercentage = wastagePercentage;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Product>> FilterProducts(string searchTerm)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Subcategory)
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
                .Include(p => p.Category)
                .Include(p => p.Subcategory)
                .Where(p => p.MetalType == metalType && 
                           p.Purity == purity && 
                           p.IsActive)
                .ToListAsync();
        }
    }
}