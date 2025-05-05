using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Page_Navigation_App.Model;

namespace Page_Navigation_App.Services
{
    /// <summary>
    /// Service that was previously handling email communications - now kept as a placeholder
    /// </summary>
    public class EmailService
    {
        private readonly LogService _logService;

        public EmailService(LogService logService)
        {
            _logService = logService;
        }

        /// <summary>
        /// Initialize service
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                await _logService.LogInformationAsync("Email functionality has been disabled");
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync("Error initializing service", exception: ex);
            }
        }
    }
}