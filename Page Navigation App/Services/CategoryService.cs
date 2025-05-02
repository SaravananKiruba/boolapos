using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Page_Navigation_App.Services
{
    public class CategoryService
    {
        private readonly AppDbContext _context;
        private readonly ProductService _productService;

        public CategoryService(AppDbContext context, ProductService productService)
        {
            _context = context;
            _productService = productService;
        }

        // Create operations
        public async Task<Category> AddCategory(Category category)
        {
            try
            {
                category.IsActive = true;
                
                await _context.Categories.AddAsync(category);
                await _context.SaveChangesAsync();
                return category;
            }
            catch
            {
                return null;
            }
        }

        public async Task<Subcategory> AddSubcategory(Subcategory subcategory)
        {
            try
            {
                var category = await _context.Categories.FindAsync(subcategory.CategoryID);
                if (category == null) return null;

                // If special rates not specified, inherit from category
                if (!subcategory.SpecialMakingCharges.HasValue)
                    subcategory.SpecialMakingCharges = category.DefaultMakingCharges;
                
                if (!subcategory.SpecialWastage.HasValue)
                    subcategory.SpecialWastage = category.DefaultWastage;

                subcategory.IsActive = true;

                await _context.Subcategories.AddAsync(subcategory);
                await _context.SaveChangesAsync();
                return subcategory;
            }
            catch
            {
                return null;
            }
        }

        // Read operations
        public async Task<Category> GetCategoryById(int categoryId)
        {
            return await _context.Categories
                .Include(c => c.Subcategories)
                .FirstOrDefaultAsync(c => c.CategoryID == categoryId);
        }

        public async Task<IEnumerable<Category>> GetAllCategories(bool includeSubcategories = false, bool includeInactive = false)
        {
            var query = _context.Categories.AsQueryable();
            
            if (includeSubcategories)
                query = query.Include(c => c.Subcategories);

            if (!includeInactive)
                query = query.Where(c => c.IsActive);

            return await query.OrderBy(c => c.CategoryName).ToListAsync();
        }

        public async Task<IEnumerable<Subcategory>> GetSubcategoriesByCategory(int categoryId, bool includeInactive = false)
        {
            var query = _context.Subcategories
                .Where(s => s.CategoryID == categoryId);

            if (!includeInactive)
                query = query.Where(s => s.IsActive);

            return await query.OrderBy(s => s.SubcategoryName).ToListAsync();
        }

        // Update operations
        public async Task<bool> UpdateCategory(Category category)
        {
            try
            {
                var existingCategory = await _context.Categories.FindAsync(category.CategoryID);
                if (existingCategory == null) return false;

                _context.Entry(existingCategory).CurrentValues.SetValues(category);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateSubcategory(Subcategory subcategory)
        {
            try
            {
                var existingSubcategory = await _context.Subcategories.FindAsync(subcategory.SubcategoryID);
                if (existingSubcategory == null) return false;

                _context.Entry(existingSubcategory).CurrentValues.SetValues(subcategory);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateCategoryRates(
            int categoryId, 
            decimal makingCharges, 
            decimal wastage,
            bool updateExistingProducts = false)
        {
            try
            {
                var category = await _context.Categories.FindAsync(categoryId);
                if (category == null) return false;

                category.DefaultMakingCharges = makingCharges;
                category.DefaultWastage = wastage;

                if (updateExistingProducts)
                {
                    await _productService.UpdateMakingCharges(categoryId, makingCharges, true);
                    await _productService.UpdateWastagePercentage(categoryId, wastage, true);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateSubcategoryRates(
            int subcategoryId,
            decimal? makingCharges,
            decimal? wastage,
            bool updateExistingProducts = false)
        {
            try
            {
                var subcategory = await _context.Subcategories
                    .Include(s => s.Products)
                    .FirstOrDefaultAsync(s => s.SubcategoryID == subcategoryId);

                if (subcategory == null) return false;

                subcategory.SpecialMakingCharges = makingCharges;
                subcategory.SpecialWastage = wastage;

                if (updateExistingProducts && subcategory.Products != null)
                {
                    foreach (var product in subcategory.Products)
                    {
                        if (makingCharges.HasValue)
                            product.MakingCharges = makingCharges.Value;
                        if (wastage.HasValue)
                            product.WastagePercentage = wastage.Value;
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Delete operations (Soft Delete)
        public async Task<bool> DeleteCategory(int categoryId)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Subcategories)
                    .FirstOrDefaultAsync(c => c.CategoryID == categoryId);

                if (category == null) return false;

                // Soft delete the category and all its subcategories
                category.IsActive = false;

                foreach (var subcategory in category.Subcategories)
                {
                    subcategory.IsActive = false;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteSubcategory(int subcategoryId)
        {
            try
            {
                var subcategory = await _context.Subcategories.FindAsync(subcategoryId);
                if (subcategory == null) return false;

                subcategory.IsActive = false;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Search operations
        public async Task<IEnumerable<Category>> SearchCategories(string searchTerm)
        {
            return await _context.Categories
                .Include(c => c.Subcategories)
                .Where(c => c.IsActive && 
                    (c.CategoryName.Contains(searchTerm) || 
                     c.Description.Contains(searchTerm)))
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Subcategory>> SearchSubcategories(string searchTerm)
        {
            return await _context.Subcategories
                .Include(s => s.Category)
                .Where(s => s.IsActive && 
                    (s.SubcategoryName.Contains(searchTerm) || 
                     s.Description.Contains(searchTerm)))
                .OrderBy(s => s.SubcategoryName)
                .ToListAsync();
        }

        // Analytics methods
        public async Task<Dictionary<string, decimal>> GetCategoryWiseProductCount()
        {
            var categories = await _context.Categories
                .Include(c => c.Products)
                .Where(c => c.IsActive)
                .ToListAsync();

            return categories.ToDictionary(
                c => c.CategoryName,
                c => (decimal)c.Products.Count(p => p.IsActive)
            );
        }

        public async Task<IDictionary<string, decimal>> GetCategoryWiseValues()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Stocks)
                .Where(p => p.Category.IsActive && p.IsActive && p.Stocks.Any(s => s.Quantity > 0))
                .ToListAsync();

            return products
                .GroupBy(p => p.Category.CategoryName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(p => p.Stocks.Sum(s => s.Quantity * p.BasePrice))
                );
        }
    }
}