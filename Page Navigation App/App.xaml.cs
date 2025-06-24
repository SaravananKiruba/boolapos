using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Windows;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Page_Navigation_App;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using Page_Navigation_App.Services;
using Page_Navigation_App.View;
using Page_Navigation_App.ViewModel;
using Page_Navigation_App.Utilities;

namespace Page_Navigation_App
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        public App()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Configure app settings
            services.Configure<AppSettings>(options => {
                options.BackupFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
                options.EncryptionKey = "JSMS_SecureEncryptionKey_2025";
                options.EnableAutoBackup = true;
                options.AutoBackupIntervalHours = 24;
            });

            // Add logging services
            services.AddLogging(configure => {
                // Using a simpler logging configuration without specific providers
                configure.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
            });

            // Register DbContext with SQLite
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite("Data Source=StockInventory.db"),
                ServiceLifetime.Scoped);

            // Authentication Service
            services.AddScoped<AuthenticationService>();

            // Core Services - Scoped lifetime
            services.AddScoped<LogService>();
            services.AddScoped<SecurityService>();
            services.AddScoped<ConfigurationService>();
            services.AddScoped<ReportService>();
            services.AddScoped<BackupService>();
            
            // Register newly implemented services
            services.AddScoped<HUIDTrackingService>();
            services.AddScoped<TaggingService>();
            services.AddScoped<GSTComplianceService>();
            
            // Register StockIntegrationService
            services.AddScoped<StockIntegrationService>();

            // Business Services - Scoped lifetime
            services.AddScoped<CustomerService>();
            services.AddScoped<ProductService>();
            services.AddScoped<OrderService>();
            services.AddScoped<FinanceService>();
            services.AddScoped<StockLedgerService>();
            services.AddScoped<SupplierService>();
            services.AddScoped<RateMasterService>();
            services.AddScoped<StockService>();
            services.AddScoped<ExchangeService>();
            services.AddScoped<UserService>();
            services.AddScoped<PrintService>();
            services.AddScoped<PurchaseOrderService>();

            // ViewModels - Transient lifetime except for NavigationVM
            services.AddSingleton<NavigationVM>();
            services.AddTransient<HomeVM>(provider => new HomeVM(
                provider.GetRequiredService<ProductService>(),
                provider.GetRequiredService<OrderService>(),
                provider.GetRequiredService<CustomerService>(),
                provider.GetRequiredService<StockLedgerService>()
            ));
            services.AddTransient<CustomerVM>();
            services.AddTransient<ProductVM>();
            services.AddTransient<OrderVM>();
            services.AddTransient<TransactionVM>();  
            services.AddTransient<SupplierVM>();
            services.AddTransient<RateMasterVM>();
            services.AddTransient<StockVM>();
            services.AddTransient<UserVM>();
            services.AddTransient<ReportVM>();
            services.AddTransient<SettingsVM>();
            services.AddTransient<PurchaseOrderVM>();
            services.AddTransient<StockIntegrationVM>();

            // Login ViewModel
            services.AddTransient<LoginViewModel>();

            // Register Windows
            services.AddSingleton<MainWindow>();
            services.AddTransient<LoginWindow>();
        }        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Run database initialization in a background thread to avoid
                // blocking the UI and causing DbContext threading issues
                await Task.Run(async () => {
                    // Ensure database is created and migrations are applied
                    using (var scope = ServiceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        await dbContext.Database.MigrateAsync().ConfigureAwait(false);
                        
                        // Seed default admin user if needed
                        var authService = scope.ServiceProvider.GetRequiredService<AuthenticationService>();
                        await authService.SeedDefaultUserAsync().ConfigureAwait(false);
                        
                        // Seed test users with different roles
                        await TestDataSeeder.SeedTestUsersAsync(dbContext, authService).ConfigureAwait(false);
                        
                        // Setup automatic backup schedule
                        var backupService = scope.ServiceProvider.GetRequiredService<BackupService>();
                        await backupService.ScheduleAutomaticBackupsAsync().ConfigureAwait(false);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing application: {ex.Message}", 
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Get main window but don't show it yet
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            
            // Set main window as the application's main window but hide it
            MainWindow = mainWindow;
            mainWindow.Hide();
            
            // Show login window
            var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
            loginWindow.Show();
        }
    }
}
