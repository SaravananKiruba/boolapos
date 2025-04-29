using System.Collections.ObjectModel;
using System.Windows.Input;
using Page_Navigation_App.Model;
using Page_Navigation_App.Services;
using Page_Navigation_App.Utilities;
using System.Linq;
using System;

namespace Page_Navigation_App.ViewModel
{
    public class StockVM : ViewModelBase
    {
        private readonly StockService _stockService;
        private readonly ProductService _productService;

        public ObservableCollection<Stock> Stocks { get; } = new ObservableCollection<Stock>();
        public ObservableCollection<string> Locations { get; } = new ObservableCollection<string>();

        private Stock _selectedStock;
        public Stock SelectedStock
        {
            get => _selectedStock;
            set
            {
                _selectedStock = value;
                OnPropertyChanged();
            }
        }

        private string _selectedLocation;
        public string SelectedLocation
        {
            get => _selectedLocation;
            set
            {
                _selectedLocation = value;
                OnPropertyChanged();
            }
        }

        private decimal _quantity;
        public decimal Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
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

        public ICommand AddOrUpdateCommand { get; }
        public ICommand TransferCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand LoadDeadStockCommand { get; }
        public ICommand LoadLowStockCommand { get; }

        public StockVM(StockService stockService, ProductService productService)
        {
            _stockService = stockService;
            _productService = productService;

            AddOrUpdateCommand = new RelayCommand<object>(_ => AddOrUpdateStock(), _ => CanAddOrUpdateStock());
            TransferCommand = new RelayCommand<object>(_ => TransferStock(), _ => CanTransferStock());
            SearchCommand = new RelayCommand<object>(_ => SearchStocks());
            ClearCommand = new RelayCommand<object>(_ => ClearForm());
            LoadDeadStockCommand = new RelayCommand<object>(_ => LoadDeadStock());
            LoadLowStockCommand = new RelayCommand<object>(_ => LoadLowStock());

            LoadStocks();
            LoadLocations();
        }

        private async void LoadStocks()
        {
            Stocks.Clear();
            var stocks = await _stockService.SearchStock(string.Empty);
            foreach (var stock in stocks)
            {
                Stocks.Add(stock);
            }
        }

        private async void LoadLocations()
        {
            Locations.Clear();
            var locations = await _stockService.GetAllLocations();
            foreach (var location in locations)
            {
                Locations.Add(location);
            }
        }

        private async void AddOrUpdateStock()
        {
            if (SelectedStock.StockID > 0)
            {
                await _stockService.UpdateStockQuantity(
                    SelectedStock.ProductID,
                    SelectedStock.Location,
                    Quantity);
            }
            else
            {
                await _stockService.AddStock(new Stock
                {
                    ProductID = SelectedStock.ProductID,
                    Location = SelectedLocation,
                    Quantity = Quantity,
                    PurchasePrice = SelectedStock.PurchasePrice
                });
            }

            LoadStocks();
            ClearForm();
        }

        private async void TransferStock()
        {
            if (SelectedStock != null && !string.IsNullOrEmpty(SelectedLocation))
            {
                await _stockService.TransferStock(
                    SelectedStock.ProductID,
                    SelectedStock.Location,
                    SelectedLocation,
                    Quantity);

                LoadStocks();
                ClearForm();
            }
        }

        private async void SearchStocks()
        {
            Stocks.Clear();
            var stocks = await _stockService.SearchStock(SearchTerm);
            foreach (var stock in stocks)
            {
                Stocks.Add(stock);
            }
        }

        private async void LoadDeadStock()
        {
            Stocks.Clear();
            var stocks = await _stockService.GetDeadStock();
            foreach (var stock in stocks)
            {
                Stocks.Add(stock);
            }
        }

        private async void LoadLowStock()
        {
            Stocks.Clear();
            var stocks = await _stockService.GetLowStock(5); // Threshold of 5 units
            foreach (var stock in stocks)
            {
                Stocks.Add(stock);
            }
        }

        private void ClearForm()
        {
            SelectedStock = new Stock();
            SelectedLocation = null;
            Quantity = 0;
            SearchTerm = string.Empty;
        }

        private bool CanAddOrUpdateStock()
        {
            return SelectedStock != null && 
                   !string.IsNullOrEmpty(SelectedLocation) && 
                   Quantity > 0;
        }

        private bool CanTransferStock()
        {
            return SelectedStock != null && 
                   !string.IsNullOrEmpty(SelectedLocation) && 
                   Quantity > 0 && 
                   SelectedLocation != SelectedStock?.Location;
        }
    }
}