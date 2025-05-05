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
    /// Service to manage daily rates for gold, silver, and other precious metals
    /// </summary>
    public class RateManagementService
    {
        private readonly AppDbContext _context;
        private readonly LogService _logService;
        
        // Constants for default making charges
        private const decimal DEFAULT_GOLD_MAKING_CHARGE = 12; // Percentage
        private const decimal DEFAULT_SILVER_MAKING_CHARGE = 15; // Percentage
        private const decimal DEFAULT_PLATINUM_MAKING_CHARGE = 20; // Percentage

        public RateManagementService(
            AppDbContext context,
            LogService logService)
        {
            _context = context;
            _logService = logService;
        }

        /// <summary>
        /// Get the current rate for a specific metal type and purity
        /// </summary>
        public async Task<RateMaster> GetCurrentRateAsync(string metalType, string purity)
        {
            try
            {
                // Get the latest rate entry for the specified metal and purity
                var rate = await _context.RateMasters
                    .Where(r => r.MetalType.Equals(metalType, StringComparison.OrdinalIgnoreCase) && 
                                r.Purity.Equals(purity, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(r => r.UpdatedDate)
                    .FirstOrDefaultAsync();
                
                if (rate == null)
                {
                    await _logService.LogWarningAsync($"No rate found for {metalType} {purity}");
                }
                
                return rate;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error getting current rate: {ex.Message}", exception: ex);
                return null;
            }
        }

        /// <summary>
        /// Get all current rates for all metal types
        /// </summary>
        public async Task<List<RateMaster>> GetAllCurrentRatesAsync()
        {
            try
            {
                // Get a list of all unique metal type and purity combinations
                var metalPurityCombos = await _context.RateMasters
                    .Select(r => new { r.MetalType, r.Purity })
                    .Distinct()
                    .ToListAsync();
                
                var currentRates = new List<RateMaster>();
                
                // For each combination, get the latest rate
                foreach (var combo in metalPurityCombos)
                {
                    var latestRate = await _context.RateMasters
                        .Where(r => r.MetalType.Equals(combo.MetalType, StringComparison.OrdinalIgnoreCase) && 
                                    r.Purity.Equals(combo.Purity, StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(r => r.UpdatedDate)
                        .FirstOrDefaultAsync();
                    
                    if (latestRate != null)
                    {
                        currentRates.Add(latestRate);
                    }
                }
                
                return currentRates;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error getting all current rates: {ex.Message}", exception: ex);
                return new List<RateMaster>();
            }
        }

        /// <summary>
        /// Update or create a new rate for a metal type and purity
        /// </summary>
        public async Task<bool> UpdateRateAsync(string metalType, string purity, decimal ratePerGram, decimal makingChargePercentage = 0)
        {
            try
            {
                // Get default making charge based on metal type if not provided
                if (makingChargePercentage <= 0)
                {
                    makingChargePercentage = GetDefaultMakingCharge(metalType);
                }
                
                // Create new rate entry
                var newRate = new RateMaster
                {
                    MetalType = metalType,
                    Purity = purity,
                    RatePerGram = ratePerGram,
                    MakingChargePercentage = makingChargePercentage,
                    UpdatedDate = DateTime.Now,
                    IsActive = true
                };
                
                // Add the new rate
                _context.RateMasters.Add(newRate);
                await _context.SaveChangesAsync();
                
                await _logService.LogInformationAsync($"Updated rate for {metalType} {purity}: ₹{ratePerGram}/g, Making: {makingChargePercentage}%");
                
                // Update product prices based on new rate
                await UpdateProductPricesAsync(metalType, purity);
                
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error updating rate: {ex.Message}", exception: ex);
                return false;
            }
        }

        /// <summary>
        /// Update all product prices based on new metal rates
        /// </summary>
        public async Task<bool> UpdateProductPricesAsync(string metalType, string purity)
        {
            try
            {
                // Get the current rate
                var currentRate = await GetCurrentRateAsync(metalType, purity);
                
                if (currentRate == null)
                {
                    await _logService.LogErrorAsync($"Cannot update product prices: No rate found for {metalType} {purity}");
                    return false;
                }
                
                // Get all products of this metal type and purity
                var products = await _context.Products
                    .Where(p => p.MetalType.Equals(metalType, StringComparison.OrdinalIgnoreCase) && 
                                p.Purity.Equals(purity, StringComparison.OrdinalIgnoreCase))
                    .ToListAsync();
                
                if (products.Count == 0)
                {
                    await _logService.LogInformationAsync($"No products found for {metalType} {purity}");
                    return true;
                }
                
                foreach (var product in products)
                {
                    // Update base price (metal value)
                    decimal metalValue = product.GrossWeight * currentRate.RatePerGram;
                    
                    // Apply wastage if applicable
                    if (product.Wastage > 0)
                    {
                        metalValue += (metalValue * product.Wastage / 100);
                    }
                    
                    // Apply making charges
                    decimal makingCharge = metalValue * (currentRate.MakingChargePercentage / 100);
                    
                    // For diamond or gemstone jewelry, add stone value
                    decimal stoneValue = product.StoneValue;
                    
                    // Calculate final price
                    decimal finalPrice = metalValue + makingCharge + stoneValue;
                    
                    // Save the price components
                    product.MetalPrice = metalValue;
                    product.MakingCharge = makingCharge;
                    product.StoneValue = stoneValue;
                    product.FinalPrice = finalPrice;
                    product.LastPriceUpdate = DateTime.Now;
                    
                    _context.Products.Update(product);
                }
                
                await _context.SaveChangesAsync();
                
                await _logService.LogInformationAsync($"Updated prices for {products.Count} products of {metalType} {purity}");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error updating product prices: {ex.Message}", exception: ex);
                return false;
            }
        }

        /// <summary>
        /// Calculate product price based on current rates
        /// </summary>
        public async Task<ProductPriceCalculation> CalculateProductPriceAsync(decimal grossWeight, string metalType, string purity, decimal wastage = 0, decimal stoneValue = 0)
        {
            try
            {
                // Get the current rate
                var currentRate = await GetCurrentRateAsync(metalType, purity);
                
                if (currentRate == null)
                {
                    await _logService.LogErrorAsync($"Cannot calculate price: No rate found for {metalType} {purity}");
                    return null;
                }
                
                // Calculate metal value
                decimal metalValue = grossWeight * currentRate.RatePerGram;
                
                // Apply wastage if applicable
                if (wastage > 0)
                {
                    metalValue += (metalValue * wastage / 100);
                }
                
                // Apply making charges
                decimal makingCharge = metalValue * (currentRate.MakingChargePercentage / 100);
                
                // Calculate final price
                decimal finalPrice = metalValue + makingCharge + stoneValue;
                
                return new ProductPriceCalculation
                {
                    MetalType = metalType,
                    Purity = purity,
                    GrossWeight = grossWeight,
                    RatePerGram = currentRate.RatePerGram,
                    Wastage = wastage,
                    MetalValue = metalValue,
                    MakingChargePercentage = currentRate.MakingChargePercentage,
                    MakingCharge = makingCharge,
                    StoneValue = stoneValue,
                    FinalPrice = finalPrice,
                    CalculationDate = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error calculating product price: {ex.Message}", exception: ex);
                return null;
            }
        }

        /// <summary>
        /// Get historical rates for a specific metal type and purity
        /// </summary>
        public async Task<List<RateMaster>> GetHistoricalRatesAsync(string metalType, string purity, DateTime fromDate, DateTime toDate)
        {
            try
            {
                return await _context.RateMasters
                    .Where(r => r.MetalType.Equals(metalType, StringComparison.OrdinalIgnoreCase) && 
                                r.Purity.Equals(purity, StringComparison.OrdinalIgnoreCase) &&
                                r.UpdatedDate >= fromDate &&
                                r.UpdatedDate <= toDate)
                    .OrderBy(r => r.UpdatedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error getting historical rates: {ex.Message}", exception: ex);
                return new List<RateMaster>();
            }
        }

        /// <summary>
        /// Apply exchange offer based on old item's metal value and new item's price
        /// </summary>
        public async Task<ExchangeOffer> CalculateExchangeOfferAsync(decimal oldItemWeight, string oldItemMetalType, string oldItemPurity, int newItemProductId)
        {
            try
            {
                // Get the current rate for old item
                var oldItemRate = await GetCurrentRateAsync(oldItemMetalType, oldItemPurity);
                
                if (oldItemRate == null)
                {
                    await _logService.LogErrorAsync($"Cannot calculate exchange: No rate found for {oldItemMetalType} {oldItemPurity}");
                    return null;
                }
                
                // Calculate old item's metal value (typically with a small deduction percentage)
                decimal deductionPercentage = 2; // 2% typical deduction for exchange
                decimal oldItemValue = oldItemWeight * oldItemRate.RatePerGram * (1 - (deductionPercentage / 100));
                
                // Get the new item
                var newItem = await _context.Products.FindAsync(newItemProductId);
                
                if (newItem == null)
                {
                    await _logService.LogErrorAsync($"Cannot calculate exchange: New product not found (ID: {newItemProductId})");
                    return null;
                }
                
                // Calculate final amount to pay
                decimal amountToPay = Math.Max(0, newItem.FinalPrice - oldItemValue);
                
                return new ExchangeOffer
                {
                    OldItemWeight = oldItemWeight,
                    OldItemMetalType = oldItemMetalType,
                    OldItemPurity = oldItemPurity,
                    OldItemRatePerGram = oldItemRate.RatePerGram,
                    OldItemValue = oldItemValue,
                    DeductionPercentage = deductionPercentage,
                    
                    NewItemProductId = newItemProductId,
                    NewItemName = newItem.ProductName,
                    NewItemPrice = newItem.FinalPrice,
                    
                    AmountToPay = amountToPay,
                    ExchangeDate = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error calculating exchange offer: {ex.Message}", exception: ex);
                return null;
            }
        }

        /// <summary>
        /// Helper method to get default making charge based on metal type
        /// </summary>
        private decimal GetDefaultMakingCharge(string metalType)
        {
            if (string.IsNullOrEmpty(metalType))
                return DEFAULT_GOLD_MAKING_CHARGE;
                
            if (metalType.Contains("Gold", StringComparison.OrdinalIgnoreCase))
                return DEFAULT_GOLD_MAKING_CHARGE;
                
            if (metalType.Contains("Silver", StringComparison.OrdinalIgnoreCase))
                return DEFAULT_SILVER_MAKING_CHARGE;
                
            if (metalType.Contains("Platinum", StringComparison.OrdinalIgnoreCase))
                return DEFAULT_PLATINUM_MAKING_CHARGE;
                
            return DEFAULT_GOLD_MAKING_CHARGE; // Default
        }
    }

    /// <summary>
    /// Product price calculation result
    /// </summary>
    public class ProductPriceCalculation
    {
        public string MetalType { get; set; }
        public string Purity { get; set; }
        public decimal GrossWeight { get; set; }
        public decimal RatePerGram { get; set; }
        public decimal Wastage { get; set; }
        public decimal MetalValue { get; set; }
        public decimal MakingChargePercentage { get; set; }
        public decimal MakingCharge { get; set; }
        public decimal StoneValue { get; set; }
        public decimal FinalPrice { get; set; }
        public DateTime CalculationDate { get; set; }
    }

    /// <summary>
    /// Exchange offer calculation result
    /// </summary>
    public class ExchangeOffer
    {
        public decimal OldItemWeight { get; set; }
        public string OldItemMetalType { get; set; }
        public string OldItemPurity { get; set; }
        public decimal OldItemRatePerGram { get; set; }
        public decimal OldItemValue { get; set; }
        public decimal DeductionPercentage { get; set; }
        
        public int NewItemProductId { get; set; }
        public string NewItemName { get; set; }
        public decimal NewItemPrice { get; set; }
        
        public decimal AmountToPay { get; set; }
        public DateTime ExchangeDate { get; set; }
    }
}