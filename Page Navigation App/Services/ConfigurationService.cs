using System.Threading.Tasks;
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

        // Business Info methods
        public async Task<BusinessInfo> GetBusinessInfo()
        {
            return await _context.BusinessInfo.FirstOrDefaultAsync() ?? new BusinessInfo();
        }

        public async Task UpdateBusinessInfo(BusinessInfo info)
        {
            var existing = await _context.BusinessInfo.FirstOrDefaultAsync();
            if (existing != null)
            {
                _context.Entry(existing).CurrentValues.SetValues(info);
            }
            else
            {
                await _context.BusinessInfo.AddAsync(info);
            }
            await _context.SaveChangesAsync();
        }

        // Settings methods
        public async Task<Setting> GetSettings()
        {
            return await _context.Settings.FirstOrDefaultAsync() ?? new Setting();
        }

        public async Task UpdateSettings(Setting settings)
        {
            var existing = await _context.Settings.FirstOrDefaultAsync();
            if (existing != null)
            {
                _context.Entry(existing).CurrentValues.SetValues(settings);
            }
            else
            {
                await _context.Settings.AddAsync(settings);
            }
            await _context.SaveChangesAsync();
        }

        // Email settings methods
        public async Task<EmailSettings> GetEmailSettings()
        {
            return await _context.EmailSettings.FirstOrDefaultAsync() ?? new EmailSettings();
        }

        public async Task UpdateEmailSettings(EmailSettings settings)
        {
            var existing = await _context.EmailSettings.FirstOrDefaultAsync();
            if (existing != null)
            {
                _context.Entry(existing).CurrentValues.SetValues(settings);
            }
            else
            {
                await _context.EmailSettings.AddAsync(settings);
            }
            await _context.SaveChangesAsync();
        }
    }
}