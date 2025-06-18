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

        public ObservableCollection<RepairJob> RepairJobs { get; set; } = new ObservableCollection<RepairJob>();
        public ObservableCollection<Customer> Customers { get; set; } = new ObservableCollection<Customer>();

        private RepairJob _selectedJob = new RepairJob { ReceiptDate = DateOnly.FromDateTime(DateTime.Now) };
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

        private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(-1));

        public DateOnly StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                OnPropertyChanged();
            }
        }


        private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);
        public DateOnly EndDate
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
            UploadImageCommand = new RelayCommand<object>(_ => UploadImage(), _ => SelectedJob?.RepairID > 0);
            UpdateStatusCommand = new RelayCommand<object>(param => UpdateStatus(param?.ToString()), _ => SelectedJob?.RepairID > 0);
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
            if (SelectedJob.RepairID == 0)
            {
                await _repairService.CreateRepairJob(SelectedJob);
            }
            else
            {
                await _repairService.UpdateStatus(SelectedJob.RepairID, SelectedJob.Status);
                if (!string.IsNullOrEmpty(SelectedJob.ImagePath))
                {
                    await _repairService.UpdateImage(SelectedJob.RepairID, SelectedJob.ImagePath);
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
            if (SelectedJob.RepairID == 0) return;

            await _repairService.UpdateFinalAmount(SelectedJob.RepairID, SelectedJob.FinalAmount);
            LoadJobs();
        }

        private bool ValidateInputs()
        {
            if (SelectedJob == null) return false;
            if (string.IsNullOrWhiteSpace(SelectedJob.ItemDescription)) return false;

            return SelectedJob.EstimatedCost > 0;
        }

        private bool CanAddOrUpdateRepairJob()
        {
            return !string.IsNullOrEmpty(SelectedJob?.ItemDescription) &&
                   !string.IsNullOrEmpty(SelectedJob?.MetalType) &&
                   !string.IsNullOrEmpty(SelectedJob?.WorkType) &&
                   SelectedJob?.Customer != null &&
                   SelectedJob?.ItemWeight > 0 &&
                   SelectedJob?.EstimatedCost > 0;
        }

        private void ClearForm()
        {
            SelectedJob = new RepairJob { ReceiptDate = DateOnly.FromDateTime(DateTime.Now) };
            SearchTerm = string.Empty;
            StartDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(-1));
            EndDate = DateOnly.FromDateTime(DateTime.Now);
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
                await _repairService.UpdateImage(SelectedJob.RepairID, imagePath);
                SelectedJob.ImagePath = imagePath;
            }
        }

        private async void UpdateStatus(string newStatus)
        {
            if (string.IsNullOrEmpty(newStatus) || SelectedJob == null) return;

            var updatedJob = await _repairService.UpdateStatus(SelectedJob.RepairID, newStatus);
            if (updatedJob != null)
            {
                SelectedJob.Status = updatedJob.Status;
                SelectedJob.CompletionDate = updatedJob.CompletionDate;
            }

            await LoadPendingJobs();
        }
    }
}