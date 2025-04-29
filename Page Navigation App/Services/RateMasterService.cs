using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Page_Navigation_App.Services
{
    public class RateMasterService
    {
        private readonly AppDbContext _context;

        public RateMasterService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<RateMaster> AddRate(RateMaster rate)
        {
            // Deactivate previous active rate for the same metal and purity
            var previousRate = await _context.RateMaster
                .Where(r => r.MetalType == rate.MetalType && 
                           r.Purity == rate.Purity && 
                           r.IsActive)
                .FirstOrDefaultAsync();

            if (previousRate != null)
            {
                previousRate.IsActive = false;
                _context.RateMaster.Update(previousRate);
            }

            rate.IsActive = true;
            rate.EffectiveDate = DateTime.Now;
            await _context.RateMaster.AddAsync(rate);
            await _context.SaveChangesAsync();
            return rate;
        }

        public async Task<RateMaster> GetCurrentRate(string metalType, string purity)
        {
            return await _context.RateMaster
                .Where(r => r.MetalType == metalType && 
                           r.Purity == purity && 
                           r.IsActive)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<RateMaster>> GetRateHistory(
            string metalType, 
            string purity, 
            DateTime startDate, 
            DateTime endDate)
        {
            return await _context.RateMaster
                .Where(r => r.MetalType == metalType &&
                           r.Purity == purity &&
                           r.EffectiveDate >= startDate &&
                           r.EffectiveDate <= endDate)
                .OrderByDescending(r => r.EffectiveDate)
                .ToListAsync();
        }

        public async Task<Dictionary<string, decimal>> GetAllCurrentRates()
        {
            var rates = await _context.RateMaster
                .Where(r => r.IsActive)
                .ToListAsync();

            return rates.ToDictionary(
                r => $"{r.MetalType}-{r.Purity}",
                r => r.Rate
            );
        }

        public async Task<decimal> CalculatePurchaseAmount(
            string metalType, 
            string purity, 
            decimal weight)
        {
            var rate = await GetCurrentRate(metalType, purity);
            if (rate == null)
                throw new InvalidOperationException($"No active rate found for {metalType} {purity}");

            return weight * rate.PurchaseRate;
        }

        public async Task<decimal> CalculateSaleAmount(
            string metalType, 
            string purity, 
            decimal weight)
        {
            var rate = await GetCurrentRate(metalType, purity);
            if (rate == null)
                throw new InvalidOperationException($"No active rate found for {metalType} {purity}");

            return weight * rate.SaleRate;
        }

        public async Task<bool> UpdateCurrentRate(
            string metalType, 
            string purity, 
            decimal newRate, 
            string updatedBy,
            string notes = null)
        {
            var currentRate = await GetCurrentRate(metalType, purity);
            if (currentRate == null)
                return false;

            // Create new rate entry
            var newRateEntry = new RateMaster
            {
                MetalType = metalType,
                Purity = purity,
                Rate = newRate,
                PurchaseRate = newRate * 0.98m, // 2% margin for purchase
                SaleRate = newRate * 1.02m, // 2% margin for sale
                EffectiveDate = DateTime.Now,
                UpdatedBy = updatedBy,
                IsActive = true,
                Notes = notes
            };

            // Deactivate current rate
            currentRate.IsActive = false;
            _context.RateMaster.Update(currentRate);

            // Add new rate
            await _context.RateMaster.AddAsync(newRateEntry);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}