using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;

namespace Page_Navigation_App.Services
{
    /// <summary>
    /// Service to manage application configuration and settings
    /// </summary>
    public class ConfigurationService
    {
        private readonly AppDbContext _context;
        private readonly LogService _logService;
        private readonly AppSettings _appSettings;
        private Dictionary<string, string> _cachedSettings;

        public ConfigurationService(
            AppDbContext context,
            LogService logService,
            IOptions<AppSettings> appSettings)
        {
            _context = context;
            _logService = logService;
            _appSettings = appSettings.Value;
            _cachedSettings = new Dictionary<string, string>();
        }

        /// <summary>
        /// Initialize the configuration service and load settings
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                // Load settings into cache
                await RefreshSettingsCacheAsync();
                
                // Ensure business info exists
                await EnsureBusinessInfoExistsAsync();
                
                await _logService.LogInformationAsync("Configuration service initialized");
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync("Error initializing configuration service", exception: ex);
                throw;
            }
        }

        /// <summary>
        /// Refresh the settings cache from the database
        /// </summary>
        public async Task RefreshSettingsCacheAsync()
        {
            try
            {
                var settings = await _context.Settings.ToListAsync();
                
                _cachedSettings.Clear();
                foreach (var setting in settings)
                {
                    _cachedSettings[setting.Key] = setting.Value;
                }
                
                await _logService.LogInformationAsync($"Settings cache refreshed with {_cachedSettings.Count} settings");
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync("Error refreshing settings cache", exception: ex);
                throw;
            }
        }

        /// <summary>
        /// Get a setting value by key, with optional default value
        /// </summary>
        public string GetSetting(string key, string defaultValue = "")
        {
            if (_cachedSettings.TryGetValue(key, out string value))
            {
                return value;
            }
            
            return defaultValue;
        }

        /// <summary>
        /// Get a setting value as a specific type
        /// </summary>
        public T GetSetting<T>(string key, T defaultValue = default)
        {
            if (_cachedSettings.TryGetValue(key, out string value))
            {
                try
                {
                    if (typeof(T) == typeof(int))
                    {
                        return (T)(object)int.Parse(value);
                    }
                    else if (typeof(T) == typeof(decimal))
                    {
                        return (T)(object)decimal.Parse(value);
                    }
                    else if (typeof(T) == typeof(bool))
                    {
                        return (T)(object)bool.Parse(value);
                    }
                    else if (typeof(T) == typeof(DateTime))
                    {
                        return (T)(object)DateTime.Parse(value);
                    }
                    else
                    {
                        return (T)(object)value;
                    }
                }
                catch
                {
                    return defaultValue;
                }
            }
            
            return defaultValue;
        }

        /// <summary>
        /// Save or update a setting
        /// </summary>
        public async Task<bool> SaveSettingAsync(string key, string value)
        {
            try
            {
                var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == key);
                
                if (setting == null)
                {
                    setting = new Setting
                    {
                        Key = key,
                        Value = value,
                        Description = "Added through ConfigurationService"
                    };
                    
                    await _context.Settings.AddAsync(setting);
                }
                else
                {
                    setting.Value = value;
                    setting.UpdatedDate = DateTime.Now;
                    _context.Settings.Update(setting);
                }
                
                await _context.SaveChangesAsync();
                
                // Update cache
                _cachedSettings[key] = value;
                
                await _logService.LogInformationAsync($"Setting saved: {key}");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error saving setting {key}", exception: ex);
                return false;
            }
        }

        /// <summary>
        /// Delete a setting
        /// </summary>
        public async Task<bool> DeleteSettingAsync(string key)
        {
            try
            {
                var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == key);
                
                if (setting != null)
                {
                    _context.Settings.Remove(setting);
                    await _context.SaveChangesAsync();
                    
                    // Remove from cache
                    _cachedSettings.Remove(key);
                    
                    await _logService.LogInformationAsync($"Setting deleted: {key}");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error deleting setting {key}", exception: ex);
                return false;
            }
        }

        /// <summary>
        /// Get business information
        /// </summary>
        public async Task<BusinessInfo> GetBusinessInfoAsync()
        {
            try
            {
                var businessInfo = await _context.BusinessInfo.FirstOrDefaultAsync();
                
                if (businessInfo == null)
                {
                    businessInfo = await EnsureBusinessInfoExistsAsync();
                }
                
                return businessInfo;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync("Error getting business info", exception: ex);
                return new BusinessInfo
                {
                    BusinessName = "Default Jewelry Shop",
                    Address = "Please update business information",
                    Phone = "000-000-0000",
                    Email = "info@example.com"
                };
            }
        }

        /// <summary>
        /// Update business information
        /// </summary>
        public async Task<bool> UpdateBusinessInfoAsync(BusinessInfo businessInfo)
        {
            try
            {
                var existing = await _context.BusinessInfo.FirstOrDefaultAsync();
                
                if (existing != null)
                {
                    // Update existing record
                    existing.BusinessName = businessInfo.BusinessName;
                    existing.Address = businessInfo.Address;
                    existing.City = businessInfo.City;
                    existing.State = businessInfo.State;
                    existing.PostalCode = businessInfo.PostalCode;
                    existing.Country = businessInfo.Country;
                    existing.Phone = businessInfo.Phone;
                    existing.AlternatePhone = businessInfo.AlternatePhone;
                    existing.Email = businessInfo.Email;
                    existing.Website = businessInfo.Website;
                    existing.GSTNumber = businessInfo.GSTNumber;
                    existing.LogoPath = businessInfo.LogoPath;
                    existing.InvoicePrefix = businessInfo.InvoicePrefix;
                    existing.BankDetails = businessInfo.BankDetails;
                    existing.TermsAndConditions = businessInfo.TermsAndConditions;
                    existing.UpdatedDate = DateTime.Now;
                    
                    _context.BusinessInfo.Update(existing);
                }
                else
                {
                    // Create new record
                    businessInfo.CreatedDate = DateTime.Now;
                    await _context.BusinessInfo.AddAsync(businessInfo);
                }
                
                await _context.SaveChangesAsync();
                await _logService.LogInformationAsync("Business information updated");
                
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync("Error updating business info", exception: ex);
                return false;
            }
        }

        /// <summary>
        /// Ensure business info exists, create default if not
        /// </summary>
        private async Task<BusinessInfo> EnsureBusinessInfoExistsAsync()
        {
            var businessInfo = await _context.BusinessInfo.FirstOrDefaultAsync();
            
            if (businessInfo == null)
            {
                businessInfo = new BusinessInfo
                {
                    BusinessName = "Jewelry Shop Management System",
                    Address = "123 Main Street",
                    City = "Mumbai",
                    State = "Maharashtra",
                    PostalCode = "400001",
                    Country = "India",
                    Phone = "+91-9876543210",
                    Email = "info@jsms.example.com",
                    Website = "www.jsms.example.com",
                    GSTNumber = "27AADCB2230M1ZT",
                    InvoicePrefix = "JSMS",
                    TermsAndConditions = "Standard terms and conditions apply.",
                    CreatedDate = DateTime.Now,
                    CurrencySymbol = "₹",
                    CurrencyCode = "INR"
                };
                
                await _context.BusinessInfo.AddAsync(businessInfo);
                await _context.SaveChangesAsync();
                
                // Also ensure currency setting is set to INR
                await SaveSettingAsync("Currency", "INR");
                
                await _logService.LogInformationAsync("Default business information created with INR currency");
            }
            else if (string.IsNullOrEmpty(businessInfo.CurrencyCode) || businessInfo.CurrencyCode != "INR")
            {
                // Update if currency is not set or is not INR
                businessInfo.CurrencyCode = "INR";
                businessInfo.CurrencySymbol = "₹";
                _context.BusinessInfo.Update(businessInfo);
                await _context.SaveChangesAsync();
                
                // Also ensure currency setting is set to INR
                await SaveSettingAsync("Currency", "INR");
                
                await _logService.LogInformationAsync("Business information updated to use INR currency");
            }
            
            return businessInfo;
        }

        /// <summary>
        /// Get email settings
        /// </summary>
        public async Task<EmailSettings> GetEmailSettingsAsync()
        {
            try
            {
                return await _context.EmailSettings.FirstOrDefaultAsync() 
                    ?? new EmailSettings
                    {
                        SmtpServer = "smtp.example.com",
                        Port = 587,
                        UseSSL = true,
                        Username = "user@example.com",
                        Password = "password",
                        FromEmail = "noreply@jsms.example.com",
                        FromName = "Jewelry Shop Management System"
                    };
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync("Error getting email settings", exception: ex);
                return new EmailSettings();
            }
        }

        /// <summary>
        /// Save email settings
        /// </summary>
        public async Task<bool> SaveEmailSettingsAsync(EmailSettings settings)
        {
            try
            {
                var existing = await _context.EmailSettings.FirstOrDefaultAsync();
                
                if (existing != null)
                {
                    existing.SmtpServer = settings.SmtpServer;
                    existing.Port = settings.Port;
                    existing.UseSSL = settings.UseSSL;
                    existing.Username = settings.Username;
                    existing.Password = settings.Password;
                    existing.FromEmail = settings.FromEmail;
                    existing.FromName = settings.FromName;
                    
                    _context.EmailSettings.Update(existing);
                }
                else
                {
                    await _context.EmailSettings.AddAsync(settings);
                }
                
                await _context.SaveChangesAsync();
                await _logService.LogInformationAsync("Email settings updated");
                
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync("Error saving email settings", exception: ex);
                return false;
            }
        }

        /// <summary>
        /// Get all application settings
        /// </summary>
        public List<Setting> GetSettings()
        {
            try
            {
                return _context.Settings.OrderBy(s => s.Key).ToList();
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error getting all settings: {ex.Message}");
                return new List<Setting>();
            }
        }

        /// <summary>
        /// Get business information (synchronous version)
        /// </summary>
        public BusinessInfo GetBusinessInfo()
        {
            // Call the async version and get the result synchronously
            return GetBusinessInfoAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Get email settings (synchronous version)
        /// </summary>
        public EmailSettings GetEmailSettings()
        {
            // Call the async version and get the result synchronously
            return GetEmailSettingsAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Update settings collection
        /// </summary>
        public bool UpdateSettings(List<Setting> settings)
        {
            try
            {
                foreach (var setting in settings)
                {
                    var existingSetting = _context.Settings.FirstOrDefault(s => s.ID == setting.ID);
                    if (existingSetting != null)
                    {
                        existingSetting.Value = setting.Value;
                        existingSetting.Description = setting.Description;
                        existingSetting.UpdatedDate = DateTime.Now;
                        _context.Settings.Update(existingSetting);
                    }
                }

                _context.SaveChanges();
                
                // Refresh the cache
                RefreshSettingsCacheAsync().GetAwaiter().GetResult();
                
                _logService.LogInformation($"Updated {settings.Count} settings");
                return true;
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error updating settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Update business information (synchronous version)
        /// </summary>
        public bool UpdateBusinessInfo(BusinessInfo businessInfo)
        {
            // Call the async version and get the result synchronously
            return UpdateBusinessInfoAsync(businessInfo).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Update email settings (synchronous version)
        /// </summary>
        public bool UpdateEmailSettings(EmailSettings settings)
        {
            // Call the async version and get the result synchronously
            return SaveEmailSettingsAsync(settings).GetAwaiter().GetResult();
        }
    }
}