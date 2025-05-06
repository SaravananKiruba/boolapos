using Microsoft.Extensions.Options;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Page_Navigation_App.Services
{
    /// <summary>
    /// Service to manage application configuration and settings
    /// </summary>
    public class ConfigurationService
    {
        private readonly AppDbContext _dbContext;
        private readonly AppSettings _appSettings;

        public ConfigurationService(AppDbContext dbContext, IOptions<AppSettings> appSettings)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _appSettings = appSettings?.Value ?? throw new ArgumentNullException(nameof(appSettings));
        }

        /// <summary>
        /// Get a setting value by key, with optional default value
        /// </summary>
        public T GetSetting<T>(string key, T defaultValue = default)
        {
            var setting = _dbContext.Settings.FirstOrDefault(s => s.Key == key);
            
            if (setting == null)
                return defaultValue;
                
            try
            {
                return (T)Convert.ChangeType(setting.Value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Get a setting value by key asynchronously, with optional default value
        /// </summary>
        public async Task<T> GetSettingAsync<T>(string key, T defaultValue = default)
        {
            var setting = await Task.Run(() => _dbContext.Settings.FirstOrDefault(s => s.Key == key));
            
            if (setting == null)
                return defaultValue;
                
            try
            {
                return (T)Convert.ChangeType(setting.Value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Save or update a setting
        /// </summary>
        public void SaveSetting(string key, string value)
        {
            var setting = _dbContext.Settings.FirstOrDefault(s => s.Key == key);
            
            if (setting == null)
            {
                setting = new Setting
                {
                    Key = key,
                    Value = value,
                    LastUpdated = DateTime.Now
                };
                _dbContext.Settings.Add(setting);
            }
            else
            {
                setting.Value = value;
                setting.LastUpdated = DateTime.Now;
            }
            
            _dbContext.SaveChanges();
        }

        /// <summary>
        /// Save or update a setting asynchronously
        /// </summary>
        public async Task SaveSettingAsync(string key, string value)
        {
            var setting = await Task.Run(() => _dbContext.Settings.FirstOrDefault(s => s.Key == key));
            
            if (setting == null)
            {
                setting = new Setting
                {
                    Key = key,
                    Value = value,
                    LastUpdated = DateTime.Now
                };
                _dbContext.Settings.Add(setting);
            }
            else
            {
                setting.Value = value;
                setting.LastUpdated = DateTime.Now;
            }
            
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Get all application settings
        /// </summary>
        public Dictionary<string, string> GetAllSettings()
        {
            return _dbContext.Settings.ToDictionary(s => s.Key, s => s.Value);
        }

        /// <summary>
        /// Get settings for the SettingsVM
        /// </summary>
        public async Task<List<Setting>> GetSettings()
        {
            return await Task.Run(() => _dbContext.Settings.ToList());
        }

        /// <summary>
        /// Get email settings for the application
        /// </summary>
        public async Task<EmailSettings> GetEmailSettings()
        {
            return await Task.Run(() => new EmailSettings
            {
                SmtpServer = GetSetting<string>("SmtpServer", "smtp.gmail.com"),
                SmtpPort = GetSetting<int>("SmtpPort", 587),
                SmtpUsername = GetSetting<string>("SmtpUsername", ""),
                SmtpPassword = GetSetting<string>("SmtpPassword", ""),
                EnableSsl = GetSetting<bool>("SmtpEnableSsl", true),
                SenderEmail = GetSetting<string>("SenderEmail", ""),
                SenderName = GetSetting<string>("SenderName", "Boola POS")
            });
        }

        /// <summary>
        /// Update business information
        /// </summary>
        public async Task<bool> UpdateBusinessInfo(BusinessInfo businessInfo)
        {
            try
            {
                await SaveSettingAsync("BusinessName", businessInfo.BusinessName);
                await SaveSettingAsync("BusinessAddress", businessInfo.Address);
                await SaveSettingAsync("BusinessPhone", businessInfo.Phone);
                await SaveSettingAsync("BusinessEmail", businessInfo.Email);
                await SaveSettingAsync("BusinessWebsite", businessInfo.Website);
                await SaveSettingAsync("BusinessGSTNumber", businessInfo.GSTNumber);
                await SaveSettingAsync("BusinessLogoPath", businessInfo.LogoPath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Update application settings
        /// </summary>
        public async Task<bool> UpdateSettings(List<Setting> settings)
        {
            try
            {
                foreach (var setting in settings)
                {
                    await SaveSettingAsync(setting.Key, setting.Value);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Update email settings
        /// </summary>
        public async Task<bool> UpdateEmailSettings(EmailSettings emailSettings)
        {
            try
            {
                await SaveSettingAsync("SmtpServer", emailSettings.SmtpServer);
                await SaveSettingAsync("SmtpPort", emailSettings.SmtpPort.ToString());
                await SaveSettingAsync("SmtpUsername", emailSettings.SmtpUsername);
                await SaveSettingAsync("SmtpPassword", emailSettings.SmtpPassword);
                await SaveSettingAsync("SmtpEnableSsl", emailSettings.EnableSsl.ToString());
                await SaveSettingAsync("SenderEmail", emailSettings.SenderEmail);
                await SaveSettingAsync("SenderName", emailSettings.SenderName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get business information asynchronously
        /// </summary>
        public async Task<BusinessInfo> GetBusinessInfoAsync()
        {
            return await Task.Run(() => GetBusinessInfo());
        }

        /// <summary>
        /// Get backup directory for the application
        /// </summary>
        public string GetBackupDirectory()
        {
            var backupDir = _appSettings.BackupFolder;
            
            if (string.IsNullOrEmpty(backupDir))
            {
                backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            }
            
            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }
            
            return backupDir;
        }

        /// <summary>
        /// Get export directory for the application
        /// </summary>
        public string GetExportDirectory()
        {
            var exportDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exports");
            
            if (!Directory.Exists(exportDir))
            {
                Directory.CreateDirectory(exportDir);
            }
            
            return exportDir;
        }

        /// <summary>
        /// Check if auto backup is enabled
        /// </summary>
        public bool GetAutoBackupEnabled()
        {
            return _appSettings.EnableAutoBackup;
        }

        /// <summary>
        /// Get the auto backup interval in hours
        /// </summary>
        public int GetAutoBackupInterval()
        {
            return _appSettings.AutoBackupIntervalHours;
        }

        /// <summary>
        /// Get business information
        /// </summary>
        public BusinessInfo GetBusinessInfo()
        {
            // Try to get from database first
            var businessNameSetting = _dbContext.Settings.FirstOrDefault(s => s.Key == "BusinessName");
            
            if (businessNameSetting != null)
            {
                return new BusinessInfo
                {
                    BusinessName = GetSetting<string>("BusinessName", "Boola POS"),
                    Address = GetSetting<string>("BusinessAddress", ""),
                    Phone = GetSetting<string>("BusinessPhone", ""),
                    Email = GetSetting<string>("BusinessEmail", ""),
                    Website = GetSetting<string>("BusinessWebsite", ""),
                    GSTNumber = GetSetting<string>("BusinessGSTNumber", ""),
                    LogoPath = GetSetting<string>("BusinessLogoPath", "")
                };
            }
            
            // Default business info
            return new BusinessInfo
            {
                BusinessName = "Boola POS",
                Address = "123 Main Street, City",
                Phone = "+1234567890",
                Email = "info@boolapos.com",
                Website = "www.boolapos.com",
                GSTNumber = "GST123456789",
                LogoPath = "Images/BOOLA LOGO.png"
            };
        }
    }
}