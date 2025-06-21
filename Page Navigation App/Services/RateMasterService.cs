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
    /// Service to manage daily metal rates and price calculations for jewelry
    /// </summary>
    public class RateMasterService
    {
        private readonly AppDbContext _context;
        private readonly LogService _logService;

        public RateMasterService(AppDbContext context, LogService logService)
        {
            _context = context;
            _logService = logService;
        }

        /// <summary>
        /// Get the latest rate for a specific metal type and purity
        /// </summary>
        public async Task<decimal> GetLatestRateAsync(string metalType, string purity)
        {
            try
            {
                var rate = await _context.RateMaster
                    .Where(r => r.MetalType == metalType && r.Purity == purity)
                    .OrderByDescending(r => r.RateDate)
                    .FirstOrDefaultAsync();
                
                return rate?.Rate ?? 0;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error getting latest rate: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Get current rate for a specific metal type and purity (synchronous version)
        /// </summary>
        public decimal GetCurrentRate(string metalType, string purity)
        {
            // Call the async version and get the result synchronously
            return GetLatestRateAsync(metalType, purity).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Add a new rate for a metal type
        /// </summary>
        public async Task<bool> AddRateAsync(RateMaster rate)
        {
            try
            {
                rate.RateDate = DateTime.Now;
                await _context.RateMaster.AddAsync(rate);
                await _context.SaveChangesAsync();
                
                await _logService.LogInformationAsync($"New rate added for {rate.MetalType} {rate.Purity}: {rate.Rate}");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error adding rate: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Add a new rate (synchronous version)
        /// </summary>
        public bool AddRate(RateMaster rate)
        {
            // Call the async version and get the result synchronously
            return AddRateAsync(rate).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Update metal rates for all products
        /// </summary>
        public async Task<int> UpdateProductPricesAsync()
        {
            try
            {
                int updatedCount = 0;
                var products = await _context.Products.ToListAsync();
                
                foreach (var product in products)
                {
                    var currentRate = await GetLatestRateAsync(product.MetalType, product.Purity);
                    if (currentRate <= 0)
                        continue;

                    // Calculate metal value (basePrice) using the metal's weight and rate
                    decimal basePrice = product.NetWeight * currentRate;
                    
                    // Apply wastage
                    decimal wastageWeight = product.NetWeight * (product.WastagePercentage / 100);
                    decimal wastageValue = wastageWeight * currentRate;
                    
                    // Apply making charges
                    decimal makingValue = basePrice * (product.MakingCharges / 100);
                    
                    // Final price is base + wastage + making + stone value
                    decimal finalPrice = basePrice + wastageValue + makingValue + product.StoneValue;
                    
                    // Apply value addition if any
                    if (product.ValueAdditionPercentage > 0)
                    {
                        finalPrice += finalPrice * (product.ValueAdditionPercentage / 100);
                    }
                    
                    product.BasePrice = basePrice;
                    product.FinalPrice = finalPrice;
                    
                    updatedCount++;
                }
                
                await _context.SaveChangesAsync();
                await _logService.LogInformationAsync($"Updated prices for {updatedCount} products");
                
                return updatedCount;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error updating product prices: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Calculate price for a product based on current rates
        /// </summary>
        public async Task<(decimal BasePrice, decimal FinalPrice)> CalculateProductPriceAsync(
            decimal weight, 
            string metalType, 
            string purity, 
            decimal wastagePercentage, 
            decimal makingCharges, 
            decimal stoneValue = 0, 
            decimal valueAddition = 0)
        {
            try
            {
                var currentRate = await GetLatestRateAsync(metalType, purity);
                if (currentRate <= 0)
                    return (0, 0);
                
                // Calculate metal value (basePrice)
                decimal basePrice = weight * currentRate;
                
                // Apply wastage
                decimal wastageWeight = weight * (wastagePercentage / 100);
                decimal wastageValue = wastageWeight * currentRate;
                
                // Apply making charges
                decimal makingValue = basePrice * (makingCharges / 100);
                
                // Final price is base + wastage + making + stone value
                decimal finalPrice = basePrice + wastageValue + makingValue + stoneValue;
                
                // Apply value addition if any
                if (valueAddition > 0)
                {
                    finalPrice += finalPrice * (valueAddition / 100);
                }
                
                return (basePrice, finalPrice);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error calculating product price: {ex.Message}");
                return (0, 0);
            }
        }

        /// <summary>
        /// Get all current rates for all metal types
        /// </summary>
        public List<RateMaster> GetCurrentRates()
        {
            try
            {
                // Get the latest rate for each metal type and purity combination
                var query = _context.RateMaster
                    .GroupBy(r => new { r.MetalType, r.Purity })
                    .Select(g => g.OrderByDescending(r => r.RateDate).FirstOrDefault());
                
                return query.ToList();
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error getting current rates: {ex.Message}");
                return new List<RateMaster>();
            }
        }

        /// <summary>
        /// Get rate history for reporting
        /// </summary>
        public async Task<List<RateMaster>> GetRateHistoryAsync(
            string metalType = null, 
            string purity = null, 
            DateTime? fromDate = null, 
            DateTime? toDate = null)
        {
            try
            {
                var query = _context.RateMaster.AsQueryable();
                
                if (!string.IsNullOrEmpty(metalType))
                    query = query.Where(r => r.MetalType == metalType);
                
                if (!string.IsNullOrEmpty(purity))
                    query = query.Where(r => r.Purity == purity);
                
                if (fromDate.HasValue)
                    query = query.Where(r => r.RateDate >= fromDate.Value);
                
                if (toDate.HasValue)
                    query = query.Where(r => r.RateDate <= toDate.Value);
                
                return await query
                    .OrderByDescending(r => r.RateDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error getting rate history: {ex.Message}");
                return new List<RateMaster>();
            }
        }

        /// <summary>
        /// Get rate history (synchronous version)
        /// </summary>
        public List<RateMaster> GetRateHistory(
            string metalType = null, 
            string purity = null, 
            DateTime? fromDate = null, 
            DateTime? toDate = null)
        {
            // Call the async version and get the result synchronously
            return GetRateHistoryAsync(metalType, purity, fromDate, toDate).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Update an existing rate
        /// </summary>
        public bool UpdateRate(RateMaster rate)
        {
            try
            {
                var existingRate = _context.RateMaster.Find(rate.RateID);
                if (existingRate == null)
                    return false;
                  existingRate.Rate = rate.Rate;
                existingRate.MetalType = rate.MetalType;
                existingRate.Purity = rate.Purity;
                existingRate.Description = rate.Description ?? string.Empty;
                existingRate.UpdatedBy = rate.UpdatedBy;
                existingRate.UpdatedDate = DateTime.Now;
                
                _context.SaveChanges();
                _logService.LogInformation($"Rate updated for {rate.MetalType} {rate.Purity}: {rate.Rate}");
                return true;
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error updating rate: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Calculate exchange value of old jewelry
        /// </summary>
        public async Task<decimal> CalculateExchangeValueAsync(
            decimal weight, 
            string metalType, 
            string purity, 
            decimal purityDeduction = 0)
        {
            try
            {
                var currentRate = await GetLatestRateAsync(metalType, purity);
                if (currentRate <= 0)
                    return 0;
                
                // Apply purity deduction to account for wear and tear
                decimal adjustedRate = currentRate * (1 - (purityDeduction / 100));
                
                // Calculate exchange value
                decimal exchangeValue = weight * adjustedRate;
                
                return exchangeValue;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error calculating exchange value: {ex.Message}");
                return 0;
            }
        }
    }
}