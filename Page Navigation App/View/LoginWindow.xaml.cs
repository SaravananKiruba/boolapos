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
using System.Threading.Tasks;

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
            try
            {
                InitializeComponent();
                
                // Setup dependency injection
                var services = new ServiceCollection();
                
                // Register database context
                services.AddSingleton(provider => 
                    new AppDbContextFactory().CreateDbContext(new string[] {}));
                
                // Register core services
                RegisterServices(services);
                
                // Register view models
                RegisterViewModels(services);
                
                // Build service provider
                _serviceProvider = services.BuildServiceProvider();
                
                // Set DataContext
                this.DataContext = _serviceProvider.GetService<LoginViewModel>();
                
                // Run database validation on startup
                Task.Run(async () => await ValidateDatabaseConnectionAsync());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing login window: {ex.Message}\n\nThe application may not function correctly.", 
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Fall back to manual initialization if necessary
                SafeInitializeComponent();
            }
        }

        private void RegisterServices(ServiceCollection services)
        {
            // Core services
            services.AddSingleton<LogService>();
            services.AddSingleton<ConfigurationService>();
            services.AddSingleton<ILogger<BackupService>>(provider => 
                provider.GetService<LoggerFactory>()?.CreateLogger<BackupService>());
            services.AddSingleton<IOptions<AppSettings>>(provider => 
                Options.Create(new AppSettings()));
            
            // Security and authentication
            services.AddSingleton<SecurityService>();
            services.AddSingleton<AuthenticationService>();
            services.AddSingleton<UserService>();
            
            // Business services
            services.AddSingleton<StockLedgerService>();
            services.AddSingleton<RateMasterService>();
            services.AddSingleton<StockService>();            services.AddSingleton<FinanceService>();
            services.AddSingleton<ProductService>();
            services.AddSingleton<OrderService>();
            services.AddSingleton<CustomerService>();
            services.AddSingleton<SupplierService>();
            services.AddSingleton<BackupService>();
        }
        
        private void RegisterViewModels(ServiceCollection services)
        {
            // Navigation
            services.AddSingleton<NavigationVM>();
            
            // Feature ViewModels
            services.AddSingleton<HomeVM>();
            services.AddSingleton<CustomerVM>();
            services.AddSingleton<ProductVM>();
            services.AddSingleton<OrderVM>();            services.AddSingleton<TransactionVM>();
            services.AddSingleton<SupplierVM>();
            services.AddSingleton<RateMasterVM>();
            services.AddSingleton<StockVM>();
            services.AddSingleton<UserVM>();
            services.AddSingleton<ReportVM>();
            services.AddSingleton<SettingsVM>();
            
            // Login ViewModel
            services.AddSingleton<LoginViewModel>();
        }
          private async Task ValidateDatabaseConnectionAsync()
        {
            // Use Task.Run to ensure this runs on a background thread with its own DbContext
            await Task.Run(async () => {
                try
                {
                    var authService = _serviceProvider.GetService<AuthenticationService>();
                    if (authService == null)
                    {
                        throw new Exception("Failed to initialize authentication service");
                    }
                    
                    // Verify database connection and ensure it has the default user
                    await authService.SeedDefaultUserAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Database validation error: {ex.Message}\n\nThe application will continue, but login functionality may be limited.",
                            "Database Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            
                        if (DataContext is LoginViewModel viewModel)
                        {
                            viewModel.ErrorMessage = "Database connection error. Login may not work.";
                        }
                    });
                }
            });
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
        
        // Fallback initialization method
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
                                if (_passwordBox != null)
                                {
                                    _passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
                                }
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
            // Bind username textbox to viewmodel
            var usernameBinding = new System.Windows.Data.Binding("Username")
            {
                UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
            };
            usernameBox.SetBinding(TextBox.TextProperty, usernameBinding);
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
                Margin = new Thickness(0, 0, 0, 30),
                Password = "Admin@123" // Default password
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
            // Bind login button to viewmodel command
            var commandBinding = new System.Windows.Data.Binding("LoginCommand");
            loginButton.SetBinding(Button.CommandProperty, commandBinding);
            
            // Bind IsEnabled to CanLoginEnabled
            var enabledBinding = new System.Windows.Data.Binding("CanLoginEnabled");
            loginButton.SetBinding(Button.IsEnabledProperty, enabledBinding);
            
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