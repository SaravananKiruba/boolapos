using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Page_Navigation_App.Model;

namespace Page_Navigation_App.Services
{
    public class BackupSchedulerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackupSchedulerService> _logger;
        private readonly AppSettings _appSettings;
        private Timer _timer;

        public BackupSchedulerService(
            IServiceProvider serviceProvider,
            ILogger<BackupSchedulerService> logger,
            IOptions<AppSettings> appSettings)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _appSettings = appSettings.Value;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Backup Scheduler Service is starting");

            if (!_appSettings.EnableAutoBackup)
            {
                _logger.LogInformation("Automatic backups are disabled in settings");
                return Task.CompletedTask;
            }

            // Convert hours to milliseconds
            int intervalMs = _appSettings.AutoBackupIntervalHours * 60 * 60 * 1000;
            
            // Create a timer that triggers every interval
            _timer = new Timer(
                DoBackup,
                null,
                TimeSpan.Zero,                          // Start immediately
                TimeSpan.FromMilliseconds(intervalMs)   // Then run every interval
            );

            return Task.CompletedTask;
        }

        private async void DoBackup(object state)
        {
            try
            {
                _logger.LogInformation("Scheduled backup started");
                
                // Create a new scope to get a scoped BackupService
                using (var scope = _serviceProvider.CreateScope())
                {
                    var backupService = scope.ServiceProvider.GetRequiredService<BackupService>();
                    string backupPath = await backupService.CreateBackupAsync("Scheduled automatic backup");
                    _logger.LogInformation($"Scheduled backup completed: {backupPath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing scheduled backup");
            }
        }

        public override Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Backup Scheduler Service is stopping");
            
            _timer?.Change(Timeout.Infinite, 0);
            
            return base.StopAsync(stoppingToken);
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }
    }
}