using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Page_Navigation_App.Model;
using Page_Navigation_App.Services;
using Page_Navigation_App.Utilities;
using System.Linq;

namespace Page_Navigation_App.ViewModel
{
    public class RateMasterVM : ViewModelBase
    {
        private readonly RateMasterService _rateService;

        public ICommand AddOrUpdateCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand SearchCommand { get; }

        public ObservableCollection<RateMaster> Rates { get; set; } = new ObservableCollection<RateMaster>();
        public ObservableCollection<RateMaster> RateHistory { get; set; } = new ObservableCollection<RateMaster>();

        private RateMaster _selectedRate = new RateMaster();
        public RateMaster SelectedRate
        {
            get => _selectedRate;
            set
            {
                _selectedRate = value;
                OnPropertyChanged();
                LoadRateHistory();
            }
        }

        private string[] _metalTypes = new[] { "Gold", "Silver", "Platinum" };
        public string[] MetalTypes => _metalTypes;

        private string[] _purities = new[] { "18k", "22k", "24k" };
        public string[] Purities => _purities;

        public RateMasterVM(RateMasterService rateService)
        {
            _rateService = rateService;
            LoadCurrentRates();

            AddOrUpdateCommand = new RelayCommand<object>(_ => AddOrUpdateRate(), _ => CanAddOrUpdateRate());
            ClearCommand = new RelayCommand<object>(_ => ClearForm(), _ => true);
            SearchCommand = new RelayCommand<object>(_ => LoadRateHistory(), _ => true);
        }

        private async void LoadCurrentRates()
        {
            Rates.Clear();
            var currentRates = await _rateService.GetCurrentRates();
            foreach (var rate in currentRates)
            {
                Rates.Add(rate);
            }
        }

        private async void LoadRateHistory()
        {
            if (SelectedRate == null) return;

            RateHistory.Clear();
            var history = await _rateService.GetRateHistory(
                SelectedRate.MetalType,
                SelectedRate.Purity,
                DateTime.Now.AddMonths(-1));

            foreach (var rate in history)
            {
                RateHistory.Add(rate);
            }
        }

        private async void AddOrUpdateRate()
        {
            SelectedRate.EffectiveDate = DateTime.Now;
            SelectedRate.IsActive = true;
            SelectedRate.UpdatedBy = Environment.UserName;

            if (SelectedRate.RateID > 0)
            {
                await _rateService.UpdateRate(SelectedRate);
            }
            else
            {
                await _rateService.AddRate(SelectedRate);
            }

            LoadCurrentRates();
            ClearForm();
        }

        private void ClearForm()
        {
            SelectedRate = new RateMaster
            {
                EffectiveDate = DateTime.Now,
                IsActive = true
            };
        }

        private bool CanAddOrUpdateRate()
        {
            return !string.IsNullOrEmpty(SelectedRate.MetalType) &&
                   !string.IsNullOrEmpty(SelectedRate.Purity) &&
                   SelectedRate.Rate > 0;
        }
    }
}