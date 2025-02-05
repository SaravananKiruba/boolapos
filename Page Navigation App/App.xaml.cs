using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Page_Navigation_App.Data;
using Page_Navigation_App.Services; // ✅ Added Services namespace
using Page_Navigation_App.View;
using Page_Navigation_App.ViewModel;
using System;
using System.Windows;

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
            services.AddTransient<CustomerService>(); // ✅ Registering CustomerService

            // Register ViewModels
            services.AddSingleton<NavigationVM>();
            services.AddTransient<HomeVM>();
            services.AddTransient<CustomerVM>(); // ✅ Registering CustomerVM without DbContext dependency
            services.AddTransient<ProductVM>();
            services.AddTransient<OrderVM>();
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
