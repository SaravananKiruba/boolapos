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
            // Register DbContext with connection string for SQLite
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite("Data Source=StockInventory.db"));

            // Register Services
            services.AddTransient<CustomerService>();
            services.AddTransient<ProductService>();
            services.AddTransient<OrderService>();
            services.AddTransient<FinanceService>();
            services.AddTransient<VendorService>();

            services.AddTransient<VendorVM>();


            // Register ViewModels
            services.AddSingleton<NavigationVM>();
            services.AddTransient<HomeVM>();
            services.AddTransient<CustomerVM>();
            services.AddTransient<ProductVM>();
            services.AddTransient<OrderVM>(provider => new OrderVM(provider.GetRequiredService<OrderService>()));
            services.AddTransient<TransactionVM>();

            // Register Views
            services.AddTransient<Customers>();

            // Register MainWindow
            services.AddTransient<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Resolve MainWindow from the ServiceProvider and show it
            var mainWindow = ServiceProvider.GetService<MainWindow>();
            mainWindow?.Show();
        }
    }
}
