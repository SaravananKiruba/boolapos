using System;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;

namespace Page_Navigation_App.Services
{
    public class ConfigurationService
    {
        private readonly AppDbContext _context;

        public ConfigurationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<T> GetTypedValue<T>(string key, T defaultValue)
        {
            var setting = await _context.Settings.FindAsync(key);
            if (setting == null)
                return defaultValue;

            return JsonSerializer.Deserialize<T>(setting.Value);
        }

        public async Task<NotificationSettings> GetNotificationSettings()
        {
            return await GetTypedValue("NotificationSettings", new NotificationSettings
            {
                EnableSMS = true,
                EnableWhatsApp = true,
                EnableEmailNotifications = true,
                SendBirthdayWishes = true,
                SendAnniversaryWishes = true,
                SendOrderConfirmations = true,
                SendPaymentReminders = true,
                SendRepairUpdates = true
            });
        }

        public async Task<BusinessInfo> GetBusinessInfo()
        {
            return await GetTypedValue("BusinessInfo", new BusinessInfo
            {
                Name = "Jewelry Shop",
                Address = "Main Street",
                Phone = "1234567890",
                Email = "contact@jewelryshop.com"
            });
        }
    }

    public class BusinessInfo
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
    }

    public class Setting
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}