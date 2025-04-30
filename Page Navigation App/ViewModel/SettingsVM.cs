using Microsoft.Win32;
using Page_Navigation_App.Services;
using Page_Navigation_App.Utilities;
using System.Windows;
using System.Windows.Input;

namespace Page_Navigation_App.ViewModel
{
    public class SettingsVM : ViewModelBase
    {
        private readonly ConfigurationService _configService;
        private readonly BackupService _backupService;

        public SettingsVM(ConfigurationService configService, BackupService backupService)
        {
            _configService = configService;
            _backupService = backupService;

            LoadSettings();

            SaveSettingsCommand = new RelayCommand<object>(_ => SaveSettings());
            BrowseCommand = new RelayCommand<object>(_ => BrowseBackupLocation());
            BackupNowCommand = new RelayCommand<object>(_ => BackupNow());
        }

        #region Properties

        private string _businessName;
        public string BusinessName
        {
            get => _businessName;
            set
            {
                _businessName = value;
                OnPropertyChanged();
            }
        }

        private string _address;
        public string Address
        {
            get => _address;
            set
            {
                _address = value;
                OnPropertyChanged();
            }
        }

        private string _phone;
        public string Phone
        {
            get => _phone;
            set
            {
                _phone = value;
                OnPropertyChanged();
            }
        }

        private string _email;
        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged();
            }
        }

        private string _website;
        public string Website
        {
            get => _website;
            set
            {
                _website = value;
                OnPropertyChanged();
            }
        }

        private string _taxId;
        public string TaxId
        {
            get => _taxId;
            set
            {
                _taxId = value;
                OnPropertyChanged();
            }
        }

        private bool _orderNotifications;
        public bool OrderNotifications
        {
            get => _orderNotifications;
            set
            {
                _orderNotifications = value;
                OnPropertyChanged();
            }
        }

        private bool _lowStockAlerts;
        public bool LowStockAlerts
        {
            get => _lowStockAlerts;
            set
            {
                _lowStockAlerts = value;
                OnPropertyChanged();
            }
        }

        private bool _paymentReminders;
        public bool PaymentReminders
        {
            get => _paymentReminders;
            set
            {
                _paymentReminders = value;
                OnPropertyChanged();
            }
        }

        private int _lowStockThreshold;
        public int LowStockThreshold
        {
            get => _lowStockThreshold;
            set
            {
                _lowStockThreshold = value;
                OnPropertyChanged();
            }
        }

        private string _backupPath;
        public string BackupPath
        {
            get => _backupPath;
            set
            {
                _backupPath = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commands
        public ICommand SaveSettingsCommand { get; }
        public ICommand BrowseCommand { get; }
        public ICommand BackupNowCommand { get; }
        #endregion

        #region Methods
        private async void LoadSettings()
        {
            var businessInfo = await _configService.GetBusinessInfo();
            var settings = await _configService.GetSettings();

            // Business Information
            BusinessName = businessInfo.BusinessName;
            Address = businessInfo.Address;
            Phone = businessInfo.Phone;
            Email = businessInfo.Email;
            Website = businessInfo.Website;
            TaxId = businessInfo.TaxId;

            // Notification Settings
            OrderNotifications = settings.OrderNotifications;
            LowStockAlerts = settings.LowStockAlerts;
            PaymentReminders = settings.PaymentReminders;
            LowStockThreshold = settings.LowStockThreshold;

            // Backup Settings
            BackupPath = settings.BackupPath;
        }

        private async void SaveSettings()
        {
            try
            {
                await _configService.UpdateBusinessInfo(new BusinessInfo
                {
                    BusinessName = BusinessName,
                    Address = Address,
                    Phone = Phone,
                    Email = Email,
                    Website = Website,
                    TaxId = TaxId
                });

                await _configService.UpdateSettings(new Setting
                {
                    OrderNotifications = OrderNotifications,
                    LowStockAlerts = LowStockAlerts,
                    PaymentReminders = PaymentReminders,
                    LowStockThreshold = LowStockThreshold,
                    BackupPath = BackupPath
                });

                MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BrowseBackupLocation()
        {
            var dialog = new SaveFileDialog
            {
                Title = "Select Backup Location",
                Filter = "Database Backup (*.bak)|*.bak",
                DefaultExt = ".bak"
            };

            if (dialog.ShowDialog() == true)
            {
                BackupPath = dialog.FileName;
            }
        }

        private async void BackupNow()
        {
            try
            {
                if (string.IsNullOrEmpty(BackupPath))
                {
                    MessageBox.Show("Please select a backup location first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await _backupService.CreateBackup(BackupPath);
                MessageBox.Show("Database backup completed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error creating backup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion
    }
}