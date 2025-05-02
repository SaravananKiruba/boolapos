using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Page_Navigation_App.Model;
using Page_Navigation_App.Services;
using Page_Navigation_App.Utilities;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Win32;

namespace Page_Navigation_App.ViewModel
{
    public class RepairJobVM : ViewModelBase
    {
        private readonly RepairJobService _repairService;
        private readonly CustomerService _customerService;

        public ICommand AddOrUpdateCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand UploadImageCommand { get; }
        public ICommand UpdateStatusCommand { get; }
        public ICommand SendNotificationCommand { get; }

        public ObservableCollection<RepairJob> RepairJobs { get; set; } = new ObservableCollection<RepairJob>();
        public ObservableCollection<Customer> Customers { get; set; } = new ObservableCollection<Customer>();

        private RepairJob _selectedJob = new RepairJob { ReceiptDate = DateTime.Now };
        public RepairJob SelectedJob
        {
            get => _selectedJob;
            set
            {
                _selectedJob = value;
                OnPropertyChanged();
            }
        }

        private string _searchTerm;
        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                _searchTerm = value;
                OnPropertyChanged();
            }
        }

        private DateTime _startDate = DateTime.Now.AddMonths(-1);
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                OnPropertyChanged();
            }
        }

        private DateTime _endDate = DateTime.Now;
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                OnPropertyChanged();
            }
        }

        private string _selectedStatus;
        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                _selectedStatus = value;
                OnPropertyChanged();
            }
        }

        public string[] Statuses => new[] { "Pending", "In Process", "Delivered" };
        public string[] MetalTypes => new[] { "Gold", "Silver", "Platinum" };
        public string[] CommonWorkTypes => new[] { "Resizing", "Polish", "Stone Setting", "Chain Repair", "Clasp Repair", "Other" };

        public RepairJobVM(RepairJobService repairService, CustomerService customerService)
        {
            _repairService = repairService;
            _customerService = customerService;

            LoadInitialData();

            AddOrUpdateCommand = new RelayCommand<object>(_ => UpdateJobStatus(), _ => CanAddOrUpdateRepairJob());
            SearchCommand = new RelayCommand<object>(_ => SearchRepairJobs(), _ => true);
            ClearCommand = new RelayCommand<object>(_ => ClearForm(), _ => true);
            UploadImageCommand = new RelayCommand<object>(_ => UploadImage(), _ => SelectedJob?.RepairID > 0); // Changed from RepairJobID to RepairID
            UpdateStatusCommand = new RelayCommand<object>(param => UpdateStatus(param?.ToString()), _ => SelectedJob?.RepairID > 0); // Changed from RepairJobID to RepairID
            SendNotificationCommand = new RelayCommand<object>(_ => SendNotification(), _ => SelectedJob?.RepairID > 0); // Changed from RepairJobID to RepairID
        }

        private async void LoadInitialData()
        {
            var customers = await _customerService.GetAllCustomers();
            Customers.Clear();
            foreach (var customer in customers)
            {
                Customers.Add(customer);
            }

            await LoadPendingJobs();
        }

        private async Task LoadPendingJobs()
        {
            var jobs = await _repairService.GetPendingJobs();
            RepairJobs.Clear();
            foreach (var job in jobs)
            {
                RepairJobs.Add(job);
            }
        }

        private async void SearchRepairJobs()
        {
            var jobs = await _repairService.SearchJobs(SearchTerm, StartDate, EndDate, SelectedStatus);
            RepairJobs.Clear();
            foreach (var job in jobs)
            {
                RepairJobs.Add(job);
            }
        }

        private async void UpdateJobStatus()
        {
            if (SelectedJob.RepairID == 0)  // Changed from RepairJobID to RepairID
            {
                await _repairService.CreateRepairJob(SelectedJob);
            }
            else
            {
                await _repairService.UpdateStatus(SelectedJob.RepairID, SelectedJob.Status);  // Changed from RepairJobID to RepairID
                if (!string.IsNullOrEmpty(SelectedJob.ImagePath))
                {
                    await _repairService.UpdateImage(SelectedJob.RepairID, SelectedJob.ImagePath);  // Changed from RepairJobID to RepairID
                }
            }

            LoadJobs();
            ClearForm();
        }

        private async void LoadJobs()
        {
            RepairJobs.Clear();
            var jobs = await _repairService.GetPendingJobs();
            foreach (var job in jobs)
            {
                RepairJobs.Add(job);
            }
        }

        private async void UpdateFinalAmount()
        {
            if (SelectedJob.RepairID == 0) return; // Changed from RepairJobID to RepairID

            await _repairService.UpdateFinalAmount(SelectedJob.RepairID, SelectedJob.FinalAmount); // Changed from RepairJobID to RepairID
            LoadJobs();
        }

        private async void SendNotification()
        {
            if (SelectedJob.RepairID == 0) return; // Changed from RepairJobID to RepairID

            await _repairService.SendStatusUpdateNotification(SelectedJob.RepairID); // Changed from RepairJobID to RepairID
        }

        private bool ValidateInputs()
        {
            if (SelectedJob == null) return false;
            if (string.IsNullOrWhiteSpace(SelectedJob.ItemDescription)) return false; // Changed from ItemDetails to ItemDescription

            return SelectedJob.EstimatedCost > 0; // Changed from EstimatedAmount to EstimatedCost
        }

        private bool CanAddOrUpdateRepairJob()
        {
            return !string.IsNullOrEmpty(SelectedJob?.ItemDescription) && // Changed from ItemDetails to ItemDescription
                   !string.IsNullOrEmpty(SelectedJob?.MetalType) &&
                   !string.IsNullOrEmpty(SelectedJob?.WorkType) &&
                   SelectedJob?.Customer != null &&
                   SelectedJob?.ItemWeight > 0 && // Changed from Weight to ItemWeight
                   SelectedJob?.EstimatedCost > 0; // Changed from EstimatedAmount to EstimatedCost
        }

        private void ClearForm()
        {
            SelectedJob = new RepairJob { ReceiptDate = DateTime.Now };
            SearchTerm = string.Empty;
            StartDate = DateTime.Now.AddMonths(-1);
            EndDate = DateTime.Now;
            SelectedStatus = null;
        }

        private async void UploadImage()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg, *.jpeg, *.png) | *.jpg; *.jpeg; *.png",
                Title = "Select an image for the repair job"
            };

            if (dialog.ShowDialog() == true)
            {
                string imagePath = dialog.FileName;
                await _repairService.UpdateImage(SelectedJob.RepairID, imagePath); // Changed from RepairJobID to RepairID
                SelectedJob.ImagePath = imagePath;
            }
        }

        private async void UpdateStatus(string newStatus)
        {
            if (string.IsNullOrEmpty(newStatus) || SelectedJob == null) return;

            var updatedJob = await _repairService.UpdateStatus(SelectedJob.RepairID, newStatus); // Changed from RepairJobID to RepairID
            if (updatedJob != null)
            {
                SelectedJob.Status = updatedJob.Status;
                SelectedJob.CompletionDate = updatedJob.CompletionDate;
            }

            await LoadPendingJobs();
        }
    }
}