using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Page_Navigation_App.Model;
using Page_Navigation_App.Services;
using Page_Navigation_App.Utilities;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Page_Navigation_App.ViewModel
{
    public class RateMasterVM : ViewModelBase
    {
        private readonly RateMasterService _rateService;
        private readonly ProductService _productService;

        public ICommand AddOrUpdateCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand CalculateRateCommand { get; }

        public ObservableCollection<RateMaster> Rates { get; set; } = new ObservableCollection<RateMaster>();
        public ObservableCollection<RateMaster> RateHistory { get; set; } = new ObservableCollection<RateMaster>();
        public ObservableCollection<RateAnalytics> RateAnalytics { get; set; } = new ObservableCollection<RateAnalytics>();

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

        private string[] _purities = new[] 
        { 
            "24k", "22k", "18k", "14k",  // Gold
            "999", "995", "925", "916",   // Silver and Platinum
            "875", "835" 
        };
        public string[] Purities => _purities;

        private string[] _sources = new[] { "Market", "Association", "MCX", "Bank", "Custom" };
        public string[] Sources => _sources;

        private decimal _volatilityThreshold = 2.0m;
        public decimal VolatilityThreshold
        {
            get => _volatilityThreshold;
            set
            {
                _volatilityThreshold = value;
                OnPropertyChanged();
            }
        }

        private bool _autoUpdatePrices = true;
        public bool AutoUpdatePrices
        {
            get => _autoUpdatePrices;
            set
            {
                _autoUpdatePrices = value;
                OnPropertyChanged();
            }
        }

        public RateMasterVM(
            RateMasterService rateService,
            ProductService productService)
        {
            _rateService = rateService;
            _productService = productService;
            
            LoadCurrentRates();
            LoadRateAnalytics();            AddOrUpdateCommand = new RelayCommand<object>(_ => AddOrUpdateRate(), _ => CanAddOrUpdateRate());
            ClearCommand = new RelayCommand<object>(_ => ClearForm(), _ => true);
            SearchCommand = new RelayCommand<object>(_ => LoadRateHistory(), _ => true);
            CalculateRateCommand = new RelayCommand<object>(_ => CalculateRate(), _ => CanCalculateRate());
        }

        private void LoadCurrentRates()
        {
            Rates.Clear();
            var currentRates = _rateService.GetCurrentRates();
            foreach (var rate in currentRates)
            {
                Rates.Add(rate);
            }
        }

        private void LoadRateHistory()
        {
            if (SelectedRate == null) return;

            RateHistory.Clear();
            var history = _rateService.GetRateHistory(
                SelectedRate.MetalType,
                SelectedRate.Purity,
                DateTime.Now.AddMonths(-1));

            foreach (var rate in history)
            {
                RateHistory.Add(rate);
            }
        }

        private void LoadRateAnalytics()
        {
            RateAnalytics.Clear();
            var currentRates = _rateService.GetCurrentRates();
            
            foreach (var current in currentRates)
            {
                var dayHistory = _rateService.GetRateHistory(
                    current.MetalType,
                    current.Purity,
                    DateTime.Now.AddDays(-7));

                var oldestRate = dayHistory.OrderBy(r => r.EffectiveDate).FirstOrDefault();
                var dayOldRate = dayHistory.Where(r => r.EffectiveDate <= DateTime.Now.AddDays(-1))
                                         .OrderByDescending(r => r.EffectiveDate)
                                         .FirstOrDefault();

                decimal change24h = 0;
                decimal change7d = 0;

                if (dayOldRate != null)
                {
                    change24h = ((current.Rate - dayOldRate.Rate) / dayOldRate.Rate) * 100;
                }

                if (oldestRate != null)
                {
                    change7d = ((current.Rate - oldestRate.Rate) / oldestRate.Rate) * 100;
                }

                RateAnalytics.Add(new RateAnalytics
                {
                    MetalType = current.MetalType,
                    Purity = current.Purity,
                    Change24h = change24h,
                    Change7d = change7d
                });
            }
        }

        private void AddOrUpdateRate()
        {
            try
            {
                SelectedRate.EffectiveDate = DateTime.Now;
                SelectedRate.IsActive = true;
                SelectedRate.EnteredBy = Environment.UserName;
                SelectedRate.UpdatedBy = Environment.UserName;
                
                if (SelectedRate.ValidUntil == null)
                {
                    SelectedRate.ValidUntil = DateTime.Now.AddDays(1);
                }

                if (string.IsNullOrEmpty(SelectedRate.Source))
                {
                    SelectedRate.Source = "Market";
                }

                bool result;
                if (SelectedRate.RateID > 0)
                {
                    result = _rateService.UpdateRate(SelectedRate);
                }
                else
                {
                    result = _rateService.AddRate(SelectedRate);
                }

                // Update product prices if enabled
                if (AutoUpdatePrices && result)
                {
                    UpdateProductPrices(SelectedRate).Wait();
                }

                LoadCurrentRates();
                LoadRateAnalytics();
                ClearForm();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to save rate: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async Task UpdateProductPrices(RateMaster newRate)
        {
            try
            {
                var products = await _productService.GetProductsByMetal(
                    newRate.MetalType,
                    newRate.Purity);

                foreach (var product in products)
                {
                    product.BasePrice = product.NetWeight * newRate.Rate;
                    
                    // Recalculate final price with all components
                    decimal makingCharges = product.MakingCharges;
                   

                    decimal wastagePercentage = product.WastagePercentage;
                    

                    decimal wastageAmount = (product.BasePrice * wastagePercentage) / 100;
                    decimal makingAmount = (product.BasePrice * makingCharges) / 100;

                    product.FinalPrice = Math.Round(
                        product.BasePrice + 
                        makingAmount + 
                        product.StoneValue +
                        wastageAmount +
                        (product.ValueAdditionPercentage > 0 
                            ? (product.BasePrice * product.ValueAdditionPercentage) / 100 
                            : 0), 
                        2);

                    await _productService.UpdateProduct(product);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to update product prices: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ClearForm()
        {
            SelectedRate = new RateMaster
            {
                EffectiveDate = DateTime.Now,
                ValidUntil = DateTime.Now.AddDays(1),
                Source = "Market",
                IsActive = true
            };
        }

        private bool CanAddOrUpdateRate()
        {
            return !string.IsNullOrEmpty(SelectedRate.MetalType) &&
                   !string.IsNullOrEmpty(SelectedRate.Purity) &&
                   !string.IsNullOrEmpty(SelectedRate.Source) &&
                   SelectedRate.Rate > 0 &&
                   SelectedRate.ValidUntil > DateTime.Now;
        }

        private void CalculateRate()
        {
            if (SelectedRate != null)
            {
                SelectedRate.FinalRate = SelectedRate.CalculateFinalRate();
            }
        }

        private bool CanCalculateRate()
        {
            return SelectedRate != null && SelectedRate.Rate > 0;
        }
    }

    public class RateAnalytics
    {
        public string MetalType { get; set; }
        public string Purity { get; set; }
        public decimal Change24h { get; set; }
        public decimal Change7d { get; set; }
    }
}