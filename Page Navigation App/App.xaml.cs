using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Windows;
using System;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Page_Navigation_App;
using Page_Navigation_App.Data;
using Page_Navigation_App.Services;
using Page_Navigation_App.View;
using Page_Navigation_App.ViewModel;

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

            // Business Services - Scoped lifetime
            services.AddScoped<CustomerService>();
            services.AddScoped<ProductService>();
            services.AddScoped<OrderService>();
            services.AddScoped<FinanceService>();
            services.AddScoped<SupplierService>();
            services.AddScoped<RateMasterService>();
            services.AddScoped<RepairJobService>();
            services.AddScoped<StockService>();
            services.AddScoped<ExchangeService>();
            services.AddScoped<CategoryService>();
            services.AddScoped<UserService>();
            services.AddScoped<PrintService>();

            // ViewModels - Transient lifetime except for NavigationVM
            services.AddSingleton<NavigationVM>();
            services.AddTransient<HomeVM>();
            services.AddTransient<CustomerVM>();
            services.AddTransient<ProductVM>();
            services.AddTransient<OrderVM>();
            services.AddTransient<TransactionVM>();
            services.AddTransient<SupplierVM>();
            services.AddTransient<RateMasterVM>();
            services.AddTransient<RepairJobVM>();
            services.AddTransient<StockVM>();
            services.AddTransient<CategoryVM>();
            services.AddTransient<UserVM>();
            services.AddTransient<ReportVM>();
            services.AddTransient<SettingsVM>();
            
            // Login ViewModel
            services.AddTransient<LoginViewModel>();

            // Register Windows
            services.AddSingleton<MainWindow>();
            services.AddTransient<LoginWindow>();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Ensure database is created and migrations are applied
            using (var scope = ServiceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await dbContext.Database.MigrateAsync();
                
                // Seed default admin user if needed
                var authService = scope.ServiceProvider.GetRequiredService<AuthenticationService>();
                await authService.SeedDefaultUserAsync();
            }

            // Get main window but don't show it yet
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            
            // Set main window as the application's main window but hide it
            MainWindow = mainWindow;
            mainWindow.Hide();
            
            // Show login window
            var loginViewModel = ServiceProvider.GetRequiredService<LoginViewModel>();
            var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
            loginWindow.Show();
        }
    }
}
