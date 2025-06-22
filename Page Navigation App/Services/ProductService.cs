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
                .ToListAsync();            return products
                .GroupBy(p => p.MetalType)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(p => p.Stocks.Sum(s => s.Quantity * p.ProductPrice))
                );
        }        public async Task<IEnumerable<Product>> GetProductsByPriceRange(
            decimal minPrice,
            decimal maxPrice)
        {
            return await _context.Products
                .Include(p => p.Stocks)
                .Where(p => p.ProductPrice >= minPrice && p.ProductPrice <= maxPrice && p.IsActive)
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
        
        /// <summary>
        /// Update a product price based on the current rate per gram
        /// </summary>
        /// <param name="productId">The ID of the product to update</param>
        /// <param name="ratePerGram">The current rate per gram for the metal</param>
        public async Task<bool> UpdateProductPrice(int productId, decimal ratePerGram)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null) return false;
                
                // Calculate the price using our enhanced formula
                product.CalculateProductPrice(ratePerGram);
                
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating product price: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Update all product prices for a specific metal type and purity
        /// </summary>
        /// <param name="metalType">The metal type</param>
        /// <param name="purity">The purity level</param>
        /// <param name="ratePerGram">The current rate per gram for the metal</param>
        public async Task<int> UpdateAllProductPrices(string metalType, string purity, decimal ratePerGram)
        {
            try
            {
                var products = await _context.Products
                    .Where(p => p.MetalType == metalType &&
                               p.Purity == purity &&
                               p.IsActive)
                    .ToListAsync();
                
                foreach (var product in products)
                {
                    product.CalculateProductPrice(ratePerGram);
                }
                
                await _context.SaveChangesAsync();
                return products.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating product prices: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// Get price breakdown for a product
        /// </summary>
        /// <param name="productId">The ID of the product</param>
        /// <param name="ratePerGram">The current rate per gram for the metal</param>
        /// <returns>A string with the price breakdown</returns>
        public async Task<string> GetProductPriceBreakdown(int productId, decimal ratePerGram)
        {
            var product = await GetProductById(productId);
            if (product == null) return "Product not found";
            
            return product.GetPriceBreakdown(ratePerGram);
        }

        // Create product with initial stock
        public async Task<Product> AddProductWithInitialStock(Product product, decimal initialQuantity, string location = "Main")
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // First add the product
                var addedProduct = await AddProduct(product);
                
                if (addedProduct == null)
                {
                    await transaction.RollbackAsync();
                    return null;
                }
                
                if (initialQuantity > 0)
                {
                    // Create stock record
                    var stock = new Stock
                    {
                        ProductID = addedProduct.ProductID,
                        SupplierID = addedProduct.SupplierID,
                        QuantityPurchased = initialQuantity,
                        PurchaseRate = addedProduct.ProductPrice * 0.7m, // Estimate purchase rate as 70% of product price
                        TotalAmount = addedProduct.ProductPrice * 0.7m * initialQuantity,
                        Location = location,
                        PaymentStatus = "Paid",
                        PurchaseDate = DateTime.Now,
                        LastUpdated = DateTime.Now,
                        AddedDate = DateTime.Now
                    };
                    
                    await _context.Stocks.AddAsync(stock);
                    await _context.SaveChangesAsync();
                    
                    // Generate individual StockItems
                    await GenerateStockItemsForProduct(addedProduct.ProductID, stock.StockID, initialQuantity);
                }
                
                await transaction.CommitAsync();
                return addedProduct;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error adding product with initial stock: {ex.Message}");
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
    }
}