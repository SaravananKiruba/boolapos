using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

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

        public async Task<Category> AddCategory(Category category)
        {
            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<Subcategory> AddSubcategory(Subcategory subcategory)
        {
            var category = await _context.Categories.FindAsync(subcategory.CategoryID);
            if (category == null) return null;

            // If special rates not specified, inherit from category
            if (!subcategory.SpecialMakingCharges.HasValue)
                subcategory.SpecialMakingCharges = category.DefaultMakingCharges;
            
            if (!subcategory.SpecialWastage.HasValue)
                subcategory.SpecialWastage = category.DefaultWastage;

            await _context.Subcategories.AddAsync(subcategory);
            await _context.SaveChangesAsync();
            return subcategory;
        }

        public async Task<bool> UpdateCategoryRates(
            int categoryId, 
            decimal makingCharges, 
            decimal wastage,
            bool updateExistingProducts = false)
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

        public async Task<bool> UpdateSubcategoryRates(
            int subcategoryId,
            decimal? makingCharges,
            decimal? wastage,
            bool updateExistingProducts = false)
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

        public async Task<IEnumerable<Category>> GetAllCategories(bool includeSubcategories = false)
        {
            var query = _context.Categories.AsQueryable();
            
            if (includeSubcategories)
            {
                query = query.Include(c => c.Subcategories);
            }

            return await query
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Subcategory>> GetSubcategoriesByCategory(int categoryId)
        {
            return await _context.Subcategories
                .Where(s => s.CategoryID == categoryId && s.IsActive)
                .OrderBy(s => s.SubcategoryName)
                .ToListAsync();
        }

        public async Task<Dictionary<string, decimal>> GetCategoryWiseProductCount()
        {
            var categories = await _context.Categories
                .Include(c => c.Products)
                .Where(c => c.IsActive)
                .ToListAsync();

            return categories.ToDictionary(
                c => c.CategoryName,
                c => (decimal)c.Products.Count
            );
        }

        public async Task<bool> DeactivateCategory(int categoryId)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null) return false;

            category.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateSubcategory(int subcategoryId)
        {
            var subcategory = await _context.Subcategories.FindAsync(subcategoryId);
            if (subcategory == null) return false;

            subcategory.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IDictionary<string, decimal>> GetCategoryWiseValues()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Stocks)
                .Where(p => p.Category.IsActive && p.Stocks.Any(s => s.Quantity > 0))
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