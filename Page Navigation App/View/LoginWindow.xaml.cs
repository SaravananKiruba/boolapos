using System.Windows;
using Page_Navigation_App.ViewModel;
using Page_Navigation_App.Services;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using MahApps.Metro.Controls;
using System.IO;
using System.Xml;

namespace Page_Navigation_App.View
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : MetroWindow
    {
        private readonly ServiceProvider _serviceProvider;
        private PasswordBox _passwordBox;

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
                    provider.GetService<ConfigurationService>(),
                    provider.GetService<IOptions<AppSettings>>()));
                    
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
            
            // Since InitializeComponent() is missing, we need to handle window initialization manually
            SafeInitializeComponent();
            
            // Set DataContext
            this.DataContext = _serviceProvider.GetService<LoginViewModel>();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel && sender is PasswordBox pwBox)
            {
                // Update the property in ViewModel when password changes
                viewModel.HasPassword = !string.IsNullOrEmpty(pwBox.Password);
            }
        }
        
        // Custom initialization method to replace the missing InitializeComponent()
        private void SafeInitializeComponent()
        {
            try
            {
                // Set basic window properties
                this.Title = "Boola POS Login";
                this.Width = 600;
                this.Height = 550;
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                this.ResizeMode = ResizeMode.NoResize;
                
                // Try to load the XAML manually if possible
                string xamlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "View", "LoginWindow.xaml");
                if (File.Exists(xamlPath))
                {
                    try
                    {
                        using (var stream = new FileStream(xamlPath, FileMode.Open, FileAccess.Read))
                        {
                            var content = XamlReader.Load(stream) as FrameworkElement;
                            if (content != null)
                            {
                                this.Content = content;
                                _passwordBox = FindName("PasswordBox") as PasswordBox;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        CreateLoginUIManually();
                        MessageBox.Show($"Using basic login UI: {ex.Message}");
                    }
                }
                else
                {
                    CreateLoginUIManually();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing login window: {ex.Message}");
            }
        }
        
        // Create a basic login UI programmatically if the XAML file can't be loaded
        private void CreateLoginUIManually()
        {
            // Create a simple login form programmatically
            var grid = new Grid();
            
            // Add row definitions
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            
            // Header
            var headerPanel = new StackPanel { Margin = new Thickness(0, 30, 0, 0) };
            var headerText = new TextBlock
            {
                Text = "Boola POS Login",
                FontSize = 32,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            headerPanel.Children.Add(headerText);
            
            var subHeaderText = new TextBlock
            {
                Text = "Login to your account",
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 20)
            };
            headerPanel.Children.Add(subHeaderText);
            grid.Children.Add(headerPanel);
            Grid.SetRow(headerPanel, 0);
            
            // Login form
            var formPanel = new StackPanel
            {
                Width = 350,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            // Username
            var usernameLabel = new TextBlock
            {
                Text = "Username",
                Margin = new Thickness(0, 0, 0, 5),
                FontWeight = FontWeights.Medium
            };
            formPanel.Children.Add(usernameLabel);
            
            var usernameBox = new TextBox
            {
                Height = 40,
                FontSize = 14,
                Padding = new Thickness(10, 0, 10, 0),
                Margin = new Thickness(0, 0, 0, 20)
            };
            formPanel.Children.Add(usernameBox);
            
            // Password
            var passwordLabel = new TextBlock
            {
                Text = "Password",
                Margin = new Thickness(0, 0, 0, 5),
                FontWeight = FontWeights.Medium
            };
            formPanel.Children.Add(passwordLabel);
            
            _passwordBox = new PasswordBox
            {
                Name = "PasswordBox",
                Height = 40,
                FontSize = 14,
                Padding = new Thickness(10, 0, 10, 0),
                Margin = new Thickness(0, 0, 0, 30)
            };
            _passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
            formPanel.Children.Add(_passwordBox);
            
            // Login button
            var loginButton = new Button
            {
                Content = "LOGIN",
                Height = 45,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15)
            };
            formPanel.Children.Add(loginButton);
            
            // Debug info
            var debugInfoText = new TextBlock
            {
                Text = "Default username: admin / password: Admin@123",
                FontSize = 11,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };
            formPanel.Children.Add(debugInfoText);
            
            grid.Children.Add(formPanel);
            Grid.SetRow(formPanel, 1);
            
            // Footer
            var exitButton = new Button
            {
                Content = "EXIT APPLICATION",
                Height = 35,
                Width = 150,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };
            exitButton.Click += ExitButton_Click;
            
            var footerPanel = new Border
            {
                Height = 60,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 20, 0, 0),
                Child = exitButton
            };
            grid.Children.Add(footerPanel);
            Grid.SetRow(footerPanel, 2);
            
            // Set the content of the window
            this.Content = grid;
        }
    }
}