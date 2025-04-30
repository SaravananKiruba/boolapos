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
                BusinessName = "Jewelry Shop",
                Address = "Main Street",
                Phone = "1234567890",
                Email = "contact@jewelryshop.com"
            });
        }

        public async Task UpdateBusinessInfo(BusinessInfo info)
        {
            var serialized = JsonSerializer.Serialize(info);
            var setting = await _context.Settings.FindAsync("BusinessInfo");
            
            if (setting == null)
            {
                setting = new Model.Setting { Key = "BusinessInfo", Value = serialized };
                _context.Settings.Add(setting);
            }
            else
            {
                setting.Value = serialized;
            }
            
            await _context.SaveChangesAsync();
        }

        public async Task<Setting> GetSettings()
        {
            return await GetTypedValue("AppSettings", new Setting
            {
                OrderNotifications = true,
                LowStockAlerts = true,
                PaymentReminders = true,
                LowStockThreshold = 10,
                BackupPath = string.Empty
            });
        }

        public async Task UpdateSettings(Setting settings)
        {
            var serialized = JsonSerializer.Serialize(settings);
            var setting = await _context.Settings.FindAsync("AppSettings");
            
            if (setting == null)
            {
                setting = new Model.Setting { Key = "AppSettings", Value = serialized };
                _context.Settings.Add(setting);
            }
            else
            {
                setting.Value = serialized;
            }
            
            await _context.SaveChangesAsync();
        }
    }

    public class BusinessInfo
    {
        public string BusinessName { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string TaxId { get; set; }
    }

    public class Setting
    {
        public bool OrderNotifications { get; set; }
        public bool LowStockAlerts { get; set; }
        public bool PaymentReminders { get; set; }
        public int LowStockThreshold { get; set; }
        public string BackupPath { get; set; }
    }
}