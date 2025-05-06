using Microsoft.Extensions.Logging;
using Page_Navigation_App.Model;
using Page_Navigation_App.Services;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Page_Navigation_App.ViewModel
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly AuthenticationService _authService;
        private readonly LogService _logService;
        private string _username = "admin";
        private string _loginMessage;
        private bool _isLoggingIn;
        private bool _hasPassword;
        private string _errorMessage;

        public event PropertyChangedEventHandler PropertyChanged;

        public LoginViewModel(AuthenticationService authService, LogService logService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));

            LoginCommand = new RelayCommand<PasswordBox>(ExecuteLogin, CanLogin);
        }

        public string Username
        {
            get => _username;
            set
            {
                if (_username != value)
                {
                    _username = value;
                    OnPropertyChanged();
                }
            }
        }

        public string LoginMessage
        {
            get => _loginMessage;
            set
            {
                if (_loginMessage != value)
                {
                    _loginMessage = value;
                    OnPropertyChanged();
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
                    // Update login message if there's an error
                    if (!string.IsNullOrEmpty(value))
                    {
                        LoginMessage = value;
                    }
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

        public bool CanLoginEnabled => !IsLoggingIn && HasPassword;

        public ICommand LoginCommand { get; }

        private bool CanLogin(PasswordBox passwordBox)
        {
            return !IsLoggingIn && passwordBox?.Password?.Length > 0;
        }

        private async void ExecuteLogin(PasswordBox passwordBox)
        {
            if (passwordBox == null)
            {
                LoginMessage = "Password box not found. Please restart the application.";
                return;
            }

            try
            {
                IsLoggingIn = true;
                LoginMessage = "Logging in...";
                
                bool isAuthenticated = await Task.Run(() => _authService.AuthenticateUser(Username, passwordBox.Password));

                if (isAuthenticated)
                {
                    LoginMessage = string.Empty;
                    _logService.LogInfo($"User {Username} logged in successfully");

                    // Close login window and show main window
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            var mainWindow = App.ServiceProvider.GetService(typeof(MainWindow)) as Window;
                            var currentWindow = GetCurrentWindow();

                            if (mainWindow != null)
                            {
                                mainWindow.Show();
                                currentWindow?.Close();
                            }
                            else
                            {
                                throw new Exception("Main window not found in service provider");
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error opening main window: {ex.Message}\nPlease restart the application.",
                                "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    });
                }
                else
                {
                    LoginMessage = "Invalid username or password. Please try again.";
                    _logService.LogWarning($"Failed login attempt for username: {Username}");
                    passwordBox.Password = string.Empty;
                    passwordBox.Focus();
                }
            }
            catch (Exception ex)
            {
                LoginMessage = $"Login error: {ex.Message}";
                _logService.LogError($"Login error: {ex.Message}");
            }
            finally
            {
                IsLoggingIn = false;
            }
        }

        private Window GetCurrentWindow()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    return window;
                }
            }
            return null;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Simple relay command implementation
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }
    }
}