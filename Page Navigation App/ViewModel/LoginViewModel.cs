using System;
using System.Security;
using System.Windows;
using System.Windows.Input;
using Page_Navigation_App.Utilities;
using Page_Navigation_App.Services;
using System.Threading.Tasks;

namespace Page_Navigation_App.ViewModel
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly AuthenticationService _authService;
        private readonly NavigationVM _navigationVM;
        
        private string _username;
        private bool _isLoggingIn;
        private string _errorMessage;
        
        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
                ClearErrorMessage();
            }
        }
        
        public bool IsLoggingIn
        {
            get => _isLoggingIn;
            set
            {
                _isLoggingIn = value;
                OnPropertyChanged();
            }
        }
        
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoginCommand { get; }
        
        public LoginViewModel(AuthenticationService authService, NavigationVM navigationVM)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _navigationVM = navigationVM ?? throw new ArgumentNullException(nameof(navigationVM));
            
            LoginCommand = new RelayCommand<object>(
                async param => await LoginAsync(param as SecureString), 
                param => CanLogin(param as SecureString)
            );
        }
        
        private bool CanLogin(SecureString password)
        {
            return !IsLoggingIn && !string.IsNullOrWhiteSpace(Username) && password != null && password.Length > 0;
        }
        
        private async Task LoginAsync(SecureString securePassword)
        {
            try
            {
                IsLoggingIn = true;
                ErrorMessage = string.Empty;
                
                // Convert SecureString to string for authentication
                string password = ConvertSecureStringToString(securePassword);
                
                var user = await _authService.AuthenticateAsync(Username, password);
                
                if (user != null)
                {
                    // Successful login
                    _navigationVM.CurrentUser = user.FullName;
                    
                    // Show main window and close login window
                    Application.Current.MainWindow.Show();
                    
                    // Close the login window
                    CloseLoginWindow();
                }
                else
                {
                    ErrorMessage = "Invalid username or password";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Login error: {ex.Message}";
            }
            finally
            {
                IsLoggingIn = false;
            }
        }
        
        private string ConvertSecureStringToString(SecureString secureString)
        {
            if (secureString == null)
                return string.Empty;
                
            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return System.Runtime.InteropServices.Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }
        
        private void ClearErrorMessage()
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ErrorMessage = string.Empty;
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
    }
}