using Microsoft.Extensions.DependencyInjection;
using Page_Navigation_App.Data;
using Page_Navigation_App.Services;
using Page_Navigation_App.View;
using Page_Navigation_App.ViewModel;
using System.Windows;
using System;
using Microsoft.EntityFrameworkCore;

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
                ServiceLifetime.Scoped); // Use Scoped for DbContext

            // Core Services - Scoped lifetime
            services.AddScoped<LogService>();
            services.AddScoped<SecurityService>();
            services.AddScoped<ConfigurationService>();
            services.AddScoped<NotificationService>();
            services.AddScoped<ReportService>();

            // Business Services - Scoped lifetime
            services.AddScoped<CustomerService>();
            services.AddScoped<ProductService>();
            services.AddScoped<OrderService>();
            services.AddScoped<FinanceService>();
            services.AddScoped<SupplierService>();
            services.AddScoped<RateMasterService>();
            services.AddScoped<RepairJobService>();
            services.AddScoped<StockService>();
            services.AddScoped<CategoryService>();
            services.AddScoped<UserService>();

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
