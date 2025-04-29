using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;

namespace Page_Navigation_App.Services
{
    public class ConfigurationService
    {
        private readonly AppDbContext _context;
        private Dictionary<string, string> _cache;

        public ConfigurationService(AppDbContext context)
        {
            _context = context;
            _cache = new Dictionary<string, string>();
        }

        public async Task<bool> SetValue(string key, string value)
        {
            try
            {
                var config = await _context.Configurations.FindAsync(key);
                if (config == null)
                {
                    config = new Configuration
                    {
                        Key = key,
                        Value = value,
                        LastUpdated = DateTime.Now
                    };
                    await _context.Configurations.AddAsync(config);
                }
                else
                {
                    config.Value = value;
                    config.LastUpdated = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                _cache[key] = value;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetValue(string key, string defaultValue = null)
        {
            if (_cache.ContainsKey(key))
                return _cache[key];

            var config = await _context.Configurations.FindAsync(key);
            var value = config?.Value ?? defaultValue;
            
            if (value != null)
                _cache[key] = value;

            return value;
        }

        public async Task<T> GetTypedValue<T>(string key, T defaultValue = default)
        {
            var value = await GetValue(key);
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            try
            {
                return JsonSerializer.Deserialize<T>(value);
            }
            catch
            {
                return defaultValue;
            }
        }

        public async Task<bool> SetTypedValue<T>(string key, T value)
        {
            var jsonValue = JsonSerializer.Serialize(value);
            return await SetValue(key, jsonValue);
        }

        // Tax Configuration
        public async Task<decimal> GetGSTRate()
        {
            return await GetTypedValue<decimal>("GSTRate", 3.0m); // Default 3%
        }

        public async Task<(decimal CGST, decimal SGST)> GetSplitGSTRates()
        {
            var gstRate = await GetGSTRate();
            return (gstRate / 2, gstRate / 2); // Split equally for CGST and SGST
        }

        // Loyalty Program Configuration
        public async Task<int> GetPointsPerHundredRupees()
        {
            return await GetTypedValue<int>("PointsPerHundredRupees", 1);
        }

        public async Task<decimal> GetPointValue()
        {
            return await GetTypedValue<decimal>("PointValue", 0.25m);
        }

        // Business Information
        public async Task<BusinessInfo> GetBusinessInfo()
        {
            return await GetTypedValue<BusinessInfo>("BusinessInfo", new BusinessInfo
            {
                Name = "Jewelry Shop",
                Address = "Default Address",
                Phone = "",
                Email = "",
                GSTNumber = "",
                Website = ""
            });
        }

        // Notification Settings
        public async Task<NotificationSettings> GetNotificationSettings()
        {
            return await GetTypedValue<NotificationSettings>("NotificationSettings", 
                new NotificationSettings
                {
                    EnableSMS = true,
                    EnableWhatsApp = true,
                    EnableEmailNotifications = false,
                    SendBirthdayWishes = true,
                    SendAnniversaryWishes = true,
                    SendRepairUpdates = true,
                    SendOrderConfirmations = true,
                    SendPaymentReminders = true
                });
        }

        // Print Settings
        public async Task<PrintSettings> GetPrintSettings()
        {
            return await GetTypedValue<PrintSettings>("PrintSettings", 
                new PrintSettings
                {
                    DefaultPrinter = "",
                    InvoiceCopies = 2,
                    PrintLogo = true,
                    PrintQRCode = true,
                    FooterText = "Thank you for your business!",
                    PaperSize = "A4"
                });
        }

        // Backup Settings
        public async Task<BackupSettings> GetBackupSettings()
        {
            return await GetTypedValue<BackupSettings>("BackupSettings", 
                new BackupSettings
                {
                    AutoBackup = true,
                    BackupFrequencyHours = 24,
                    BackupPath = "Backups",
                    KeepBackupsForDays = 30,
                    LastBackupTime = null
                });
        }

        // Rate Settings
        public async Task<RateSettings> GetRateSettings()
        {
            return await GetTypedValue<RateSettings>("RateSettings", 
                new RateSettings
                {
                    AutoUpdateRates = false,
                    UpdateFrequencyMinutes = 60,
                    PurchaseRateMargin = 2.0m,
                    SaleRateMargin = 2.0m,
                    DefaultWastagePercentage = 8.0m
                });
        }

        public async Task<Dictionary<string, string>> GetAllSettings()
        {
            return await _context.Configurations
                .ToDictionaryAsync(c => c.Key, c => c.Value);
        }

        public async Task ResetToDefaults()
        {
            _cache.Clear();
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM Configurations");
            await _context.SaveChangesAsync();
        }
    }

    public class Configuration
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class BusinessInfo
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string GSTNumber { get; set; }
        public string Website { get; set; }
    }

    public class NotificationSettings
    {
        public bool EnableSMS { get; set; }
        public bool EnableWhatsApp { get; set; }
        public bool EnableEmailNotifications { get; set; }
        public bool SendBirthdayWishes { get; set; }
        public bool SendAnniversaryWishes { get; set; }
        public bool SendRepairUpdates { get; set; }
        public bool SendOrderConfirmations { get; set; }
        public bool SendPaymentReminders { get; set; }
    }

    public class PrintSettings
    {
        public string DefaultPrinter { get; set; }
        public int InvoiceCopies { get; set; }
        public bool PrintLogo { get; set; }
        public bool PrintQRCode { get; set; }
        public string FooterText { get; set; }
        public string PaperSize { get; set; }
    }

    public class BackupSettings
    {
        public bool AutoBackup { get; set; }
        public int BackupFrequencyHours { get; set; }
        public string BackupPath { get; set; }
        public int KeepBackupsForDays { get; set; }
        public DateTime? LastBackupTime { get; set; }
    }

    public class RateSettings
    {
        public bool AutoUpdateRates { get; set; }
        public int UpdateFrequencyMinutes { get; set; }
        public decimal PurchaseRateMargin { get; set; }
        public decimal SaleRateMargin { get; set; }
        public decimal DefaultWastagePercentage { get; set; }
    }
}