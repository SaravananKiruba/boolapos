﻿using Microsoft.Extensions.DependencyInjection;
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

            // Core Services - Scoped lifetime
            services.AddScoped<LogService>();
            services.AddScoped<ReportService>();
            
            // Register newly implemented services
            services.AddScoped<TaggingService>();

            // Business Services - Scoped lifetime
            services.AddScoped<CustomerService>();
            services.AddScoped<ProductService>();
            services.AddScoped<OrderService>();
            services.AddScoped<FinanceService>();
            services.AddScoped<SupplierService>();
            services.AddScoped<RateMasterService>();
            services.AddScoped<PrintService>();
            
            // New workflow services
            services.AddScoped<StockService>();
            services.AddScoped<PurchaseOrderService>();

            // ViewModels - Transient lifetime except for NavigationVM
            services.AddSingleton<NavigationVM>();
            services.AddTransient<HomeVM>(provider => new HomeVM(
                provider.GetRequiredService<ProductService>(),
                provider.GetRequiredService<OrderService>(),
                provider.GetRequiredService<CustomerService>()
            ));
            services.AddTransient<CustomerVM>();
            services.AddTransient<ProductVM>();
            services.AddTransient<OrderVM>();
            services.AddTransient<TransactionVM>();  
            services.AddTransient<SupplierVM>();
            services.AddTransient<RateMasterVM>();
            services.AddTransient<ReportVM>();
            
            // New workflow ViewModels
            services.AddTransient<PurchaseOrderVM>();
            services.AddTransient<StockVM>();

            // Register Windows
            services.AddSingleton<MainWindow>();
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
                        
                        // Setup automatic backup schedule
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing application: {ex.Message}", 
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Show main window directly (no login required)
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
    }
}
