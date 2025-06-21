using System;
using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Page_Navigation_App.Utilities;
using Page_Navigation_App.Services;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using Page_Navigation_App.Model; // Added to reference the User class

namespace Page_Navigation_App.ViewModel
{
    public class LoginViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private readonly AuthenticationService _authService;
        private readonly NavigationVM _navigationVM;
        
        private string _username = "admin"; // Default to admin for easier login
        private bool _isLoggingIn;
        private string _errorMessage;
        private bool _hasPassword = true; // Set default to true
        
        public string Username
        {
            get => _username;
            set
            {
                if (_username != value)
                {
                    _username = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanLoginEnabled));
                    ClearErrorMessage();
                }
            }
        }
        
        public bool IsLoggingIn
        {
            get => _isLoggingIn;
            set
            {
                if (_isLoggingIn != value)
                {
                    _isLoggingIn = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanLoginEnabled));
                    
                    // Debug output to verify state changes
                    Debug.WriteLine($"IsLoggingIn set to: {value}");
                    
                    // Force UI update
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => { }));
                }
            }
        }
        
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasPassword
        {
            get => _hasPassword;
            set
            {
                if (_hasPassword != value)
                {
                    _hasPassword = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanLoginEnabled));
                }
            }
        }

        // Property to enable/disable the login button based on form state
        public bool CanLoginEnabled => !IsLoggingIn && !string.IsNullOrWhiteSpace(Username) && HasPassword;

        public ICommand LoginCommand { get; private set; }
        
        public LoginViewModel(AuthenticationService authService, NavigationVM navigationVM)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _navigationVM = navigationVM ?? throw new ArgumentNullException(nameof(navigationVM));
            
            // Create direct login command
            LoginCommand = new SimpleRelayCommand(ExecuteLogin, CanExecuteLogin);
            
            // Initialize with test credentials for easier testing
            Username = "admin";
            ErrorMessage = "";
            IsLoggingIn = false;
              // Check database connection immediately
            // Use ConfigureAwait(false) to avoid deadlocks and continue on any thread instead of Task.Run
            // This avoids multiple threads using the same DbContext instance
            _ = CheckDatabaseConnectionAsync();
        }
          private async Task CheckDatabaseConnectionAsync()
        {
            try
            {
                // Use ConfigureAwait(false) to avoid returning to the UI thread
                // which helps prevent thread synchronization issues with DbContext
                await _authService.SeedDefaultUserAsync().ConfigureAwait(false);
                Debug.WriteLine("Database connection successful, default user seeded if needed.");
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ErrorMessage = "Database connection error. Please check your configuration.";
                    Debug.WriteLine($"Database connection error: {ex.Message}");
                });
            }
        }
        
        private bool CanExecuteLogin()
        {
            return !IsLoggingIn && !string.IsNullOrWhiteSpace(Username) && HasPassword;
        }
          private async void ExecuteLogin()
        {
            try
            {
                // Update UI immediately to show we're logging in
                IsLoggingIn = true;
                ErrorMessage = "";
                
                // Get password from view
                var passwordBox = FindPasswordBox();
                if (passwordBox == null)
                {
                    ErrorMessage = "Could not access password field";
                    MessageBox.Show("Could not access password field. Please try again.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    IsLoggingIn = false;
                    return;
                }
                
                string password = passwordBox.Password;
                
                // For debugging - use default password if empty
                if (string.IsNullOrEmpty(password) && Username.ToLower() == "admin")
                {
                    password = "Admin@123";
                    Debug.WriteLine("Using default password for admin user");
                }
                
                Debug.WriteLine($"Attempting login with username: {Username}");
                
                // Add a small delay to show the loading animation
                await Task.Delay(500);
                
                // Store username and password locally to use in the task
                string username = Username;
                string pwd = password;
                
                // Use Task.Run to ensure authentication happens on a background thread with its own DbContext
                var user = await Task.Run(async () => {
                    // This will run in a separate thread with its own DbContext scope
                    return await AuthenticateWithRetryAsync(username, pwd);
                });
                
                if (user != null)
                {
                    // Successful login
                    Debug.WriteLine($"Login successful for user: {user.Username}");
                    _navigationVM.CurrentUser = user.FullName;
                    
                    // Show main window
                    Application.Current.Dispatcher.Invoke(() => 
                    {
                        MessageBox.Show($"Welcome back, {user.FullName}!", "Login Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // Ensure the MainWindow is available
                        if (Application.Current.MainWindow != null)
                        {
                            Application.Current.MainWindow.Show();
                            
                            // Close the login window
                            CloseLoginWindow();
                        }
                        else
                        {
                            MessageBox.Show("Error accessing main application window. Please restart the application.", 
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    });
                }
                else
                {
                    ErrorMessage = "Invalid username or password";
                    MessageBox.Show("Invalid username or password. Please try again.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Login error: {ex.Message}";
                MessageBox.Show($"Login error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Login exception: {ex}");
            }
            finally
            {
                IsLoggingIn = false;
            }
        }
          private async Task<User> AuthenticateWithRetryAsync(string username, string password, int maxRetries = 2)
        {
            User user = null;
            int attempts = 0;
            
            while (user == null && attempts < maxRetries)
            {
                try
                {
                    // Use ConfigureAwait(false) to avoid context switching back to the UI thread
                    // This helps prevent DbContext threading issues
                    user = await _authService.AuthenticateAsync(username, password).ConfigureAwait(false);
                    
                    if (user == null && attempts == 0)
                    {
                        // If first attempt fails, try to seed the database and retry
                        Debug.WriteLine("Authentication failed, checking if database needs seeding...");
                        await _authService.SeedDefaultUserAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Authentication attempt {attempts + 1} failed: {ex.Message}");
                    
                    if (attempts == maxRetries - 1)
                        throw; // Re-throw on last attempt
                }
                
                attempts++;
                
                if (user == null && attempts < maxRetries)
                    await Task.Delay(500).ConfigureAwait(false); // Small delay between retries
            }
            
            return user;
        }
        
        private System.Windows.Controls.PasswordBox FindPasswordBox()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    return FindPasswordBoxInWindow(window);
                }
            }
            return null;
        }

        private System.Windows.Controls.PasswordBox FindPasswordBoxInWindow(DependencyObject parent)
        {
            // Try to cast to a FrameworkElement first to use FindName
            if (parent is FrameworkElement element)
            {
                var passwordBox = element.FindName("PasswordBox") as System.Windows.Controls.PasswordBox;
                if (passwordBox != null)
                    return passwordBox;
            }
            
            // Continue with visual tree traversal
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var result = FindPasswordBoxInWindow(child);
                if (result != null)
                    return result;
            }
            
            return null;
        }
        
        private void ClearErrorMessage()
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ErrorMessage = "";
            }
        }
        
        private void CloseLoginWindow()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    window.Close();
                    break;
                }
            }        }
        
        // Use 'new' keyword to explicitly hide the base class implementation
        public new event PropertyChangedEventHandler PropertyChanged;

        // Use 'new' keyword to explicitly hide the base class implementation
        protected new virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // A simpler relay command implementation
    public class SimpleRelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public SimpleRelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }
    }
}