using System.Windows;
using Page_Navigation_App.ViewModel;
using Page_Navigation_App.Services;
using Page_Navigation_App.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Windows.Controls;

namespace Page_Navigation_App.View
{
    public partial class LoginWindow
    {
        private readonly ServiceProvider _serviceProvider;

        public LoginWindow()
        {
            // Setup dependency injection
            var services = new ServiceCollection();
            
            // Register database context
            services.AddSingleton(provider => 
                new AppDbContextFactory().CreateDbContext(new string[] {}));
            
            // Register all services with their dependencies
            services.AddSingleton<LogService>();
            services.AddSingleton<ConfigurationService>();
            services.AddSingleton<ILogger<BackupService>>(provider => 
                provider.GetService<LoggerFactory>()?.CreateLogger<BackupService>());
            services.AddSingleton<IOptions<AppSettings>>(provider => 
                Options.Create(new AppSettings()));
            
            // Register services resolving circular dependencies
            services.AddSingleton<SecurityService>(provider => 
                new SecurityService(
                    provider.GetService<AppDbContext>(),
                    provider.GetService<LogService>(),
                    provider.GetService<ConfigurationService>()));
                    
            services.AddSingleton<StockLedgerService>();
            services.AddSingleton<RateMasterService>();
            
            services.AddSingleton<StockService>(provider => 
                new StockService(
                    provider.GetService<AppDbContext>(),
                    provider.GetService<RateMasterService>()));
                    
            services.AddSingleton<FinanceService>(provider => 
                new FinanceService(
                    provider.GetService<AppDbContext>(),
                    provider.GetService<LogService>()));
                    
            services.AddSingleton<ProductService>();
            
            services.AddSingleton<OrderService>(provider => 
                new OrderService(
                    provider.GetService<AppDbContext>(),
                    provider.GetService<StockService>(),
                    provider.GetService<RateMasterService>()));
                    
            services.AddSingleton<CustomerService>(provider => 
                new CustomerService(
                    provider.GetService<AppDbContext>(),
                    provider.GetService<FinanceService>()));
                    
            services.AddSingleton<CategoryService>(provider => 
                new CategoryService(
                    provider.GetService<AppDbContext>(),
                    provider.GetService<ProductService>()));
                    
            services.AddSingleton<SupplierService>(provider => 
                new SupplierService(
                    provider.GetService<AppDbContext>(),
                    provider.GetService<StockService>(),
                    provider.GetService<StockLedgerService>()));
                    
            services.AddSingleton<RepairJobService>();
            services.AddSingleton<BackupService>();
            services.AddSingleton<AuthenticationService>();
            services.AddSingleton<UserService>();
            
            // Register view models
            services.AddSingleton<HomeVM>();
            services.AddSingleton<CustomerVM>();
            services.AddSingleton<ProductVM>();
            services.AddSingleton<OrderVM>();
            services.AddSingleton<TransactionVM>();
            services.AddSingleton<SupplierVM>();
            services.AddSingleton<RateMasterVM>();
            services.AddSingleton<RepairJobVM>();
            services.AddSingleton<StockVM>();
            services.AddSingleton<CategoryVM>();
            services.AddSingleton<UserVM>();
            services.AddSingleton<ReportVM>();
            services.AddSingleton<SettingsVM>();
            services.AddSingleton<NavigationVM>();
            services.AddSingleton<LoginViewModel>();
            
            // Build service provider
            _serviceProvider = services.BuildServiceProvider();
            
            // Initialize component
            InitializeComponent();
            
            // Set DataContext
            this.DataContext = _serviceProvider.GetService<LoginViewModel>();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel)
            {
                // Update the property in ViewModel when password changes
                viewModel.HasPassword = !string.IsNullOrEmpty(PasswordBox.Password);
            }
        }
    }
}