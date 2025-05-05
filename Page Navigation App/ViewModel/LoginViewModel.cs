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
                }
                
                Debug.WriteLine($"Attempting login with username: {Username}");
                
                // Attempt authentication
                var user = await _authService.AuthenticateAsync(Username, password);
                
                if (user != null)
                {
                    // Successful login
                    _navigationVM.CurrentUser = user.FullName;
                    
                    // Show main window
                    Application.Current.Dispatcher.Invoke(() => 
                    {
                        MessageBox.Show("Login successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        Application.Current.MainWindow.Show();
                        
                        // Close the login window
                        CloseLoginWindow();
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
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
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