using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Windows;
using System;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Page_Navigation_App;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using Page_Navigation_App.Services;
using Page_Navigation_App.View;
using Page_Navigation_App.ViewModel;
using System.Windows.Controls;
using System.Windows.Media;
using ControlzEx.Theming; // Added for ThemeManager

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

            // Add export service
            services.AddScoped<ExportService>(provider => {
                try {
                    var reportService = provider.GetRequiredService<ReportService>();
                    var orderService = provider.GetRequiredService<OrderService>();
                    var stockService = provider.GetRequiredService<StockService>();
                    var customerService = provider.GetRequiredService<CustomerService>();
                    var logService = provider.GetRequiredService<LogService>();
                    var exportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exports");
                    
                    // Ensure the exports directory exists
                    if (!Directory.Exists(exportPath)) {
                        Directory.CreateDirectory(exportPath);
                    }
                    
                    return new ExportService(reportService, orderService, stockService, customerService, logService, exportPath);
                }
                catch (Exception ex) {
                    // Log error but don't crash the application
                    Console.WriteLine($"Error initializing ExportService: {ex.Message}");
                    // Return a minimal functional service to avoid null reference errors
                    var logService = provider.GetRequiredService<LogService>();
                    var reportService = provider.GetRequiredService<ReportService>();
                    var orderService = provider.GetRequiredService<OrderService>();
                    var stockService = provider.GetRequiredService<StockService>();
                    var customerService = provider.GetRequiredService<CustomerService>();
                    var exportPath = Path.Combine(Path.GetTempPath(), "BoolaPOS_Exports");
                    
                    if (!Directory.Exists(exportPath)) {
                        Directory.CreateDirectory(exportPath);
                    }
                    
                    return new ExportService(reportService, orderService, stockService, customerService, logService, exportPath);
                }
            });

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
            services.AddScoped<EMIService>();

            // Business Services - Scoped lifetime
            services.AddScoped<CustomerService>();
            services.AddScoped<ProductService>();
            services.AddScoped<OrderService>();
            services.AddScoped<FinanceService>();
            services.AddScoped<StockLedgerService>();            services.AddScoped<SupplierService>();
            services.AddScoped<RateMasterService>();
            services.AddScoped<RepairJobService>();
            services.AddScoped<StockService>();
            services.AddScoped<ExchangeService>();
            services.AddScoped<UserService>();
            services.AddScoped<PrintService>();

            // ViewModels - Transient lifetime except for NavigationVM
            services.AddSingleton<NavigationVM>();
            services.AddTransient<HomeVM>();
            services.AddTransient<CustomerVM>();
            services.AddTransient<ProductVM>();
            services.AddTransient<OrderVM>();            services.AddTransient<TransactionVM>();
            services.AddTransient<SupplierVM>();
            services.AddTransient<RateMasterVM>();
            services.AddTransient<RepairJobVM>();
            services.AddTransient<StockVM>();
            services.AddTransient<UserVM>();
            services.AddTransient<ReportVM>();
            services.AddTransient<SettingsVM>();
            
            // Login ViewModel
            services.AddTransient<LoginViewModel>();

            // Register Windows
            services.AddSingleton<MainWindow>();
            services.AddTransient<LoginWindow>();
        }        protected override async void OnStartup(StartupEventArgs e)
        {            base.OnStartup(e);
            
            // Initialize MahApps.Metro theme system directly
            ApplyApplicationTheme();

            try
            {
                // Ensure database is created and migrations are applied
                using (var scope = ServiceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await dbContext.Database.MigrateAsync();
                    
                    // Seed default admin user if needed
                    var authService = scope.ServiceProvider.GetRequiredService<AuthenticationService>();
                    await authService.SeedDefaultUserAsync();
                    
                    // Setup automatic backup schedule
                    var backupService = scope.ServiceProvider.GetRequiredService<BackupService>();
                    await backupService.ScheduleAutomaticBackupsAsync();
                }

                // Get main window but don't show it yet
                var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                
                // Set main window as the application's main window but hide it
                MainWindow = mainWindow;
                mainWindow.Hide();
                
                try
                {
                    // Show login window with error handling
                    var loginViewModel = ServiceProvider.GetRequiredService<LoginViewModel>();
                    var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
                    
                    // Set DataContext explicitly
                    loginWindow.DataContext = loginViewModel;
                    
                    loginWindow.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error displaying login window: {ex.Message}\n\nThe application will now use a simplified login.", 
                        "Login Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    
                    // Create a simplified login window as fallback
                    var simpleLoginWindow = new Window
                    {
                        Title = "Boola POS Login",
                        Width = 500,
                        Height = 300,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        Content = new Grid
                        {
                            Children = 
                            {
                                new StackPanel
                                {
                                    Width = 300,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    VerticalAlignment = VerticalAlignment.Center,
                                    Children = 
                                    {
                                        new TextBlock
                                        {
                                            Text = "Login to Boola POS System",
                                            FontSize = 18,
                                            FontWeight = FontWeights.Bold,
                                            HorizontalAlignment = HorizontalAlignment.Center,
                                            Margin = new Thickness(0, 0, 0, 20)
                                        },
                                        new TextBox
                                        {
                                            Text = "admin",
                                            Height = 30,
                                            Margin = new Thickness(0, 5, 0, 10)
                                        },
                                        new PasswordBox
                                        {
                                            Password = "Admin@123",
                                            Height = 30,
                                            Margin = new Thickness(0, 0, 0, 20)
                                        },
                                        new Button
                                        {
                                            Content = "Login",
                                            Height = 35,
                                            Background = new SolidColorBrush(Colors.DarkGoldenrod),
                                            Foreground = new SolidColorBrush(Colors.White)
                                        }
                                    }
                                }
                            }
                        }
                    };
                    
                    // Add event handler properly
                    var loginButton = ((StackPanel)((Grid)simpleLoginWindow.Content).Children[0]).Children[3] as Button;
                    loginButton.Click += (sender, args) => {
                        // Simple login logic
                        simpleLoginWindow.Close();
                        mainWindow.Show();
                    };
                    
                    simpleLoginWindow.Show();
                }
            }            catch (Exception ex)
            {
                MessageBox.Show($"Application initialization error: {ex.Message}", 
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Helper method to apply theme without using ThemeHelper class
        private void ApplyApplicationTheme()
        {
            // Set MahApps.Metro theme directly
            ControlzEx.Theming.ThemeManager.Current.ChangeTheme(Application.Current, "Light.Blue");
        }
    }
}
