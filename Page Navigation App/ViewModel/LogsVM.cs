using Microsoft.Extensions.DependencyInjection;
using Page_Navigation_App.Model;
using Page_Navigation_App.Services;
using Page_Navigation_App.Utilities;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System;
using System.Threading.Tasks;

namespace Page_Navigation_App.ViewModel
{
    public class LogsVM : ViewModelBase
    {
        private readonly LogService _logService;
        private readonly SecurityService _securityService;
        private DateTime _startDate = DateTime.Today.AddDays(-30);
        private DateTime _endDate = DateTime.Today;
        private string _selectedLogType = "All";

        public LogsVM()
        {
            _logService = App.ServiceProvider.GetRequiredService<LogService>();
            _securityService = App.ServiceProvider.GetRequiredService<SecurityService>();

            LoadLogsCommand = new RelayCommand<object>(async _ => await LoadLogs());

            // Initial load
            _ = LoadLogs();
        }

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                OnPropertyChanged();
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                OnPropertyChanged();
            }
        }

        public string SelectedLogType
        {
            get => _selectedLogType;
            set
            {
                _selectedLogType = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> LogTypes { get; } = new ObservableCollection<string>
        {
            "All",
            "System",
            "Security",
            "Audit"
        };

        public ObservableCollection<LogEntry> SystemLogs { get; } = new ObservableCollection<LogEntry>();
        public ObservableCollection<SecurityLog> SecurityLogs { get; } = new ObservableCollection<SecurityLog>();
        public ObservableCollection<AuditLog> AuditLogs { get; } = new ObservableCollection<AuditLog>();

        public ICommand LoadLogsCommand { get; }

        private async Task LoadLogs()
        {
            SystemLogs.Clear();
            SecurityLogs.Clear();
            AuditLogs.Clear();

            switch (_selectedLogType)
            {
                case "All":
                    await LoadAllLogs();
                    break;
                case "System":
                    var systemLogs = await _logService.GetSystemLogs(_startDate, _endDate);
                    foreach (var log in systemLogs)
                        SystemLogs.Add(log);
                    break;
                case "Security":
                    var securityLogs = await _securityService.GetSecurityLogs(_startDate, _endDate);
                    foreach (var log in securityLogs)
                        SecurityLogs.Add(log);
                    break;
                case "Audit":
                    var auditLogs = await _logService.GetAuditLogs(_startDate, _endDate);
                    foreach (var log in auditLogs)
                        AuditLogs.Add(log);
                    break;
            }
        }

        private async Task LoadAllLogs()
        {
            var systemLogsTask = _logService.GetSystemLogs(_startDate, _endDate);
            var securityLogsTask = _securityService.GetSecurityLogs(_startDate, _endDate);
            var auditLogsTask = _logService.GetAuditLogs(_startDate, _endDate);

            await Task.WhenAll(systemLogsTask, securityLogsTask, auditLogsTask);

            foreach (var log in await systemLogsTask)
                SystemLogs.Add(log);
            foreach (var log in await securityLogsTask)
                SecurityLogs.Add(log);
            foreach (var log in await auditLogsTask)
                AuditLogs.Add(log);
        }
    }
}