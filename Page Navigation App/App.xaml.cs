using Microsoft.Extensions.DependencyInjection;
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

            InitializeComponent();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register DbContext with SQLite
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite("Data Source=StockInventory.db"),
                ServiceLifetime.Scoped);

            // Configure backup path
            string backupPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");

            // Core Services - Scoped lifetime
            services.AddScoped<LogService>();
            services.AddScoped<SecurityService>();
            services.AddScoped<ConfigurationService>();
            services.AddScoped<ReportService>();
            services.AddScoped<BackupService>(provider => 
                new BackupService(
                    provider.GetRequiredService<AppDbContext>(),
                    backupPath
                ));

            // Business Services - Scoped lifetime
            services.AddScoped<CustomerService>();
            services.AddScoped<ProductService>();
            services.AddScoped<OrderService>();
            services.AddScoped<FinanceService>();
            services.AddScoped<SupplierService>();
            services.AddScoped<RateMasterService>();
            services.AddScoped<RepairJobService>();
            services.AddScoped<StockService>(provider => new StockService(
                provider.GetRequiredService<AppDbContext>(),
                provider.GetRequiredService<RateMasterService>()
            ));
            services.AddScoped<ExchangeService>(provider => new ExchangeService(
                provider.GetRequiredService<AppDbContext>(),
                provider.GetRequiredService<StockService>(),
                provider.GetRequiredService<OrderService>()
            ));
            services.AddScoped<CategoryService>();
            services.AddScoped<UserService>();

            // Add PrintService with all required dependencies
            services.AddScoped<PrintService>(provider => new PrintService(
                provider.GetRequiredService<OrderService>(),
                provider.GetRequiredService<RepairJobService>(),
                provider.GetRequiredService<RateMasterService>(),
                provider.GetRequiredService<ProductService>(),
                provider.GetRequiredService<CustomerService>()
            ));

            // ViewModels - Transient lifetime except for NavigationVM
            services.AddSingleton<NavigationVM>();
            services.AddTransient<HomeVM>();
            services.AddTransient<CustomerVM>();
            services.AddTransient<ProductVM>(provider => new ProductVM(
                provider.GetRequiredService<ProductService>(),
                provider.GetRequiredService<CategoryService>(),
                provider.GetRequiredService<SupplierService>(),
                provider.GetRequiredService<RateMasterService>()
            ));
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

            // Register MainWindow as Singleton
            services.AddSingleton<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
    }
}
