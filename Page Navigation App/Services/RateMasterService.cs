using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            // Deactivate current active rate for this metal type and purity
            var currentRate = await _context.RateMaster
                .Where(r => r.MetalType == rate.MetalType &&
                           r.Purity == rate.Purity &&
                           r.IsActive)
                .FirstOrDefaultAsync();

            if (currentRate != null)
            {
                currentRate.IsActive = false;
            }

            await _context.RateMaster.AddAsync(rate);
            await _context.SaveChangesAsync();

            // Notify subscribers about rate change
            await NotifyRateChange(rate);

            return rate;
        }

        public async Task<bool> UpdateRate(RateMaster rate)
        {
            var existingRate = await _context.RateMaster.FindAsync(rate.RateID);
            if (existingRate == null) return false;

            _context.Entry(existingRate).CurrentValues.SetValues(rate);
            await _context.SaveChangesAsync();

            // Notify subscribers about rate change
            await NotifyRateChange(rate);

            return true;
        }

        public async Task<IEnumerable<RateMaster>> GetCurrentRates()
        {
            return await _context.RateMaster
                .Where(r => r.IsActive)
                .OrderBy(r => r.MetalType)
                .ThenBy(r => r.Purity)
                .ToListAsync();
        }

        public async Task<IEnumerable<RateMaster>> GetAllCurrentRates()
        {
            return await _context.RateMaster
                .Where(r => r.IsActive)
                .OrderBy(r => r.MetalType)
                .ThenBy(r => r.Purity)
                .ToListAsync();
        }

        public async Task<IEnumerable<RateMaster>> GetRateHistory(
            string metalType,
            string purity,
            DateTime fromDate)
        {
            return await _context.RateMaster
                .Where(r => r.MetalType == metalType &&
                           r.Purity == purity &&
                           r.EffectiveDate >= fromDate)
                .OrderByDescending(r => r.EffectiveDate)
                .ToListAsync();
        }

        public async Task<RateMaster> GetCurrentRate(string metalType, string purity)
        {
            return await _context.RateMaster
                .Where(r => r.MetalType == metalType &&
                           r.Purity == purity &&
                           r.IsActive)
                .FirstOrDefaultAsync();
        }

        private async Task NotifyRateChange(RateMaster newRate)
        {
            // Get Settings
            var settings = await _context.Settings.FirstOrDefaultAsync();
            if (settings == null) return;

            // Since we don't have specific SMS/WhatsApp notification settings in our Setting model,
            // we'll use the generic OrderNotifications setting for now
            bool sendNotifications = settings.OrderNotifications;

            if (sendNotifications)
            {
                var message = $"Rate Update: {newRate.MetalType} {newRate.Purity} - ₹{newRate.Rate:N2}/g";
                
                // Get customers who opted for rate notifications
                var customers = await _context.Customers
                    .Where(c => c.NotifyRateChanges)
                    .ToListAsync();

                foreach (var customer in customers)
                {
                    if (!string.IsNullOrEmpty(customer.PhoneNumber))
                    {
                        // TODO: Implement SMS notification
                        // await _smsService.SendSMS(customer.PhoneNumber, message);
                    }

                    if (!string.IsNullOrEmpty(customer.WhatsAppNumber))
                    {
                        // TODO: Implement WhatsApp notification
                        // await _whatsAppService.SendMessage(customer.WhatsAppNumber, message);
                    }
                }
            }
        }
    }
}