using Microsoft.Extensions.DependencyInjection;
using Page_Navigation_App.Model;
using Page_Navigation_App.Services;
using Page_Navigation_App.Utilities;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace Page_Navigation_App.ViewModel
{
    public class LogsVM : ViewModelBase
    {
        private readonly LogService _logService;
        private DateTime _startDate = DateTime.Today.AddDays(-30);
        private DateTime _endDate = DateTime.Today;
        private string _selectedLogType = "All";

        public LogsVM()
        {
            _logService = App.ServiceProvider.GetRequiredService<LogService>();

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
        public ObservableCollection<AuditLog> AuditLogs { get; } = new ObservableCollection<AuditLog>();

        public ICommand LoadLogsCommand { get; }

        private async Task LoadLogs()
        {
            SystemLogs.Clear();
            AuditLogs.Clear();

            switch (_selectedLogType)
            {
                case "All":
                    await LoadAllLogs();
                    break;
                case "System":
                    var systemLogs = _logService.GetSystemLogs();
                    foreach (var log in systemLogs)
                        SystemLogs.Add(log);
                    break;
                
                case "Audit":
                    var auditLogs = _logService.GetAuditLogs();
                    foreach (var log in auditLogs)
                        AuditLogs.Add(log);
                    break;
            }
        }

        private async Task LoadAllLogs()
        {
            // Get logs synchronously since the methods don't have async versions with date parameters
            var systemLogs = _logService.GetSystemLogs();
            var auditLogs = _logService.GetAuditLogs();

            // Only need to await the security logs

            // Filter logs by date if needed (for system and audit logs)
            var filteredSystemLogs = systemLogs.Where(l => 
                l.Timestamp >= _startDate && l.Timestamp <= _endDate.AddDays(1).AddSeconds(-1));
                
            var filteredAuditLogs = auditLogs.Where(l => 
                l.Timestamp >= _startDate && l.Timestamp <= _endDate.AddDays(1).AddSeconds(-1));

            foreach (var log in filteredSystemLogs)
                SystemLogs.Add(log);
           
            foreach (var log in filteredAuditLogs)
                AuditLogs.Add(log);
        }
    }
}