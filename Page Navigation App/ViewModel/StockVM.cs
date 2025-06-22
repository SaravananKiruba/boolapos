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
    {        private readonly StockService _stockService;
        private readonly ProductService _productService;
        private readonly SupplierService _supplierService; // Added
        
        public ObservableCollection<Stock> Stocks { get; } = new ObservableCollection<Stock>();
        public ObservableCollection<string> Locations { get; } = new ObservableCollection<string>();
        public ObservableCollection<Product> Products { get; } = new ObservableCollection<Product>();
        public ObservableCollection<Supplier> Suppliers { get; } = new ObservableCollection<Supplier>();
        public ObservableCollection<StockItem> StockItems { get; } = new ObservableCollection<StockItem>();
        
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

        private string _transferLocation;
        public string TransferLocation
        {
            get => _transferLocation;
            set
            {
                _transferLocation = value;
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
        
        private Stock _newPurchase = new Stock();
        public Stock NewPurchase
        {
            get => _newPurchase;
            set
            {
                _newPurchase = value;
                OnPropertyChanged();
            }
        }
        
        private bool _isPurchaseModalOpen;
        public bool IsPurchaseModalOpen
        {
            get => _isPurchaseModalOpen;
            set
            {
                _isPurchaseModalOpen = value;
                OnPropertyChanged();
            }
        }
        
        private int _selectedProductId;
        public int SelectedProductId
        {
            get => _selectedProductId;
            set
            {
                _selectedProductId = value;
                OnPropertyChanged();
                UpdateSelectedProductInfo();
            }
        }
        
        private int _selectedSupplierId;
        public int SelectedSupplierId
        {
            get => _selectedSupplierId;
            set
            {
                _selectedSupplierId = value;
                OnPropertyChanged();
            }
        }
        
        private Expense _lastCreatedExpense;
        public Expense LastCreatedExpense
        {
            get => _lastCreatedExpense;
            set
            {
                _lastCreatedExpense = value;
                OnPropertyChanged();
            }
        }
        
        private bool _showExpenseConfirmation;
        public bool ShowExpenseConfirmation
        {
            get => _showExpenseConfirmation;
            set
            {
                _showExpenseConfirmation = value;
                OnPropertyChanged();
            }
        }
        
        // Stock threshold for low stock warning
        private decimal _reorderThreshold = 10;
        public decimal ReorderThreshold
        {
            get => _reorderThreshold;
            set
            {
                _reorderThreshold = value;
                OnPropertyChanged();
            }
        }
        
        public ICommand AddOrUpdateCommand { get; }
        public ICommand TransferCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand LoadDeadStockCommand { get; }
        public ICommand LoadLowStockCommand { get; }
        public ICommand OpenPurchaseModalCommand { get; }
        public ICommand ClosePurchaseModalCommand { get; }
        public ICommand AddPurchaseCommand { get; }
        public ICommand ViewStockDetailsCommand { get; }
        public ICommand CreateExpenseCommand { get; }        public StockVM(
            StockService stockService, 
            ProductService productService,
            SupplierService supplierService)
        {
            _stockService = stockService;
            _productService = productService;
            _supplierService = supplierService;
            
            AddOrUpdateCommand = new RelayCommand<object>(_ => AddOrUpdateStock(), _ => CanAddOrUpdateStock());
            TransferCommand = new RelayCommand<object>(_ => TransferStock(), _ => CanTransferStock());
            SearchCommand = new RelayCommand<object>(_ => SearchStocks());
            ClearCommand = new RelayCommand<object>(_ => ClearForm());
            LoadDeadStockCommand = new RelayCommand<object>(_ => LoadDeadStock());
            LoadLowStockCommand = new RelayCommand<object>(_ => LoadLowStock());
            OpenPurchaseModalCommand = new RelayCommand<object>(_ => OpenPurchaseModal());
            ClosePurchaseModalCommand = new RelayCommand<object>(_ => ClosePurchaseModal());
            AddPurchaseCommand = new RelayCommand<object>(_ => AddPurchase(), _ => CanAddPurchase());
            ViewStockDetailsCommand = new RelayCommand<int>(productId => ViewStockDetails(productId));
            CreateExpenseCommand = new RelayCommand<Stock>(stock => CreateExpenseForPurchase(stock));
            
            CloseStockItemsCommand = new RelayCommand<object>(_ => CloseStockItems());
            
            LoadStocks();
            LoadLocations();
            LoadProducts();
            LoadSuppliers();
        }

        private async void LoadProducts()
        {
            Products.Clear();
            var products = await _productService.GetAllProducts();
            foreach (var product in products)
            {
                Products.Add(product);
            }
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

        private async void LoadSuppliers()
        {
            Suppliers.Clear();
            var suppliers = await _supplierService.GetAllSuppliers();
            foreach (var supplier in suppliers)
            {
                Suppliers.Add(supplier);
            }
        }
        
        private void OpenPurchaseModal()
        {
            NewPurchase = new Stock
            {
                PurchaseDate = DateTime.Now,
                Location = "Main",
                PaymentStatus = "Pending"
            };
            IsPurchaseModalOpen = true;
        }
        
        private void ClosePurchaseModal()
        {
            IsPurchaseModalOpen = false;
        }
        
        private bool CanAddPurchase()
        {
            return NewPurchase != null && 
                   SelectedProductId > 0 && 
                   SelectedSupplierId > 0 && 
                   NewPurchase.QuantityPurchased > 0 && 
                   NewPurchase.PurchaseRate > 0;
        }
        
        private async void AddPurchase()
        {
            try
            {
                NewPurchase.ProductID = SelectedProductId;
                NewPurchase.SupplierID = SelectedSupplierId;
                NewPurchase.TotalAmount = NewPurchase.QuantityPurchased * NewPurchase.PurchaseRate;
                
                var addedStock = await _stockService.AddStockWithPurchaseRecord(NewPurchase);
                
                if (addedStock != null)
                {
                    // Create expense record for this purchase
                    var expense = await _stockService.CreateExpenseForPurchase(
                        addedStock, 
                        "System");
                    
                    LastCreatedExpense = expense;
                    ShowExpenseConfirmation = expense != null;
                    
                    LoadStocks();
                    ClosePurchaseModal();
                    
                    System.Windows.MessageBox.Show(
                        "Purchase recorded successfully with stock items generated.", 
                        "Success", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        "Failed to record purchase. Please check your inputs.", 
                        "Error", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"An error occurred: {ex.Message}", 
                    "Error", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error);
            }
        }
        
        private async void UpdateSelectedProductInfo()
        {
            if (SelectedProductId > 0)
            {
                var product = await _productService.GetProductById(SelectedProductId);
                if (product != null)
                {
                    // Set suggested purchase price based on product price
                    NewPurchase.PurchaseRate = product.ProductPrice * 0.7m;
                }
            }
        }
        
        private bool _isStockItemsVisible;
        public bool IsStockItemsVisible
        {
            get => _isStockItemsVisible;
            set
            {
                _isStockItemsVisible = value;
                OnPropertyChanged();
            }
        }
        
        public ICommand CloseStockItemsCommand { get; }

        private void CloseStockItems()
        {
            IsStockItemsVisible = false;
        }
        
        private async void ViewStockDetails(int productId)
        {
            try
            {
                StockItems.Clear();
                var items = await _stockService.GetStockItemsByProduct(productId);
                
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        StockItems.Add(item);
                    }
                
                    IsStockItemsVisible = true;
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        "No stock items found for this product.", 
                        "Information", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to load stock items: {ex.Message}", 
                    "Error", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error);
            }
        }
        
        private async void CreateExpenseForPurchase(Stock stock)
        {
            if (stock != null)
            {
                try
                {
                    var expense = await _stockService.CreateExpenseForPurchase(stock, "User");
                    
                    if (expense != null)
                    {
                        LastCreatedExpense = expense;
                        System.Windows.MessageBox.Show(
                            "Expense record created successfully.", 
                            "Success", 
                            System.Windows.MessageBoxButton.OK, 
                            System.Windows.MessageBoxImage.Information);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(
                            "Failed to create expense record.", 
                            "Error", 
                            System.Windows.MessageBoxButton.OK, 
                            System.Windows.MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"An error occurred: {ex.Message}", 
                        "Error", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Error);
                }
            }
        }
        
        // 1. Add or Update Stock - Implementation
        public async void AddOrUpdateStock()
        {
            try
            {
                bool success;
                
                if (SelectedStock != null && SelectedStock.StockID > 0)
                {
                    // Update existing stock
                    success = await _stockService.UpdateStock(SelectedStock);
                    if (success)
                    {
                        System.Windows.MessageBox.Show(
                            "Stock updated successfully.", 
                            "Success", 
                            System.Windows.MessageBoxButton.OK, 
                            System.Windows.MessageBoxImage.Information);
                    }
                }
                else
                {
                    // Add new stock
                    var newStock = new Stock
                    {
                        ProductID = SelectedProductId,
                        SupplierID = _selectedSupplierId,
                        QuantityPurchased = Quantity,
                        Location = SelectedLocation ?? "Main",
                        PurchaseDate = DateTime.Now
                    };
                    
                    var addedStock = await _stockService.AddStock(newStock);
                    success = addedStock != null;
                    
                    if (success)
                    {
                        System.Windows.MessageBox.Show(
                            "Stock added successfully.", 
                            "Success", 
                            System.Windows.MessageBoxButton.OK, 
                            System.Windows.MessageBoxImage.Information);
                    }
                }
                
                if (!success)
                {
                    System.Windows.MessageBox.Show(
                        "Failed to save stock. Please check your inputs.", 
                        "Error", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Error);
                }
                else
                {
                    LoadStocks();
                    ClearForm();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"An error occurred: {ex.Message}", 
                    "Error", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error);
            }
        }
        
        // Can execute method for AddOrUpdateStock
        public bool CanAddOrUpdateStock()
        {
            // If updating existing stock
            if (SelectedStock != null && SelectedStock.StockID > 0)
            {
                return true; // For simplicity, we're allowing updates as long as a stock is selected
            }
            
            // If adding new stock
            return SelectedProductId > 0 && 
                   _selectedSupplierId > 0 &&
                   Quantity > 0 &&
                   !string.IsNullOrEmpty(SelectedLocation);
        }
        
        // 2. Transfer Stock - Implementation
        public async void TransferStock()
        {
            try
            {
                if (SelectedStock == null || SelectedStock.ProductID <= 0)
                {
                    System.Windows.MessageBox.Show(
                        "Please select a stock item to transfer.", 
                        "Error", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Error);
                    return;
                }
                
                if (string.IsNullOrEmpty(TransferLocation) || TransferLocation == SelectedStock.Location)
                {
                    System.Windows.MessageBox.Show(
                        "Please select a different location to transfer to.", 
                        "Error", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Error);
                    return;
                }
                
                bool success = await _stockService.TransferStock(
                    SelectedStock.ProductID,
                    SelectedStock.Location,
                    TransferLocation,
                    Quantity);
                
                if (success)
                {
                    System.Windows.MessageBox.Show(
                        $"Successfully transferred {Quantity} units to {TransferLocation}.", 
                        "Success", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Information);
                    
                    LoadStocks();
                    ClearForm();
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        "Transfer failed. Please check that sufficient stock exists at source location.", 
                        "Error", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"An error occurred during transfer: {ex.Message}", 
                    "Error", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error);
            }
        }
        
        // Can execute method for TransferStock
        public bool CanTransferStock()
        {
            return SelectedStock != null && 
                   SelectedStock.ProductID > 0 && 
                   !string.IsNullOrEmpty(TransferLocation) && 
                   TransferLocation != SelectedStock.Location && 
                   Quantity > 0 && 
                   Quantity <= SelectedStock.Quantity;
        }
        
        // 3. Search Stocks - Implementation
        public async void SearchStocks()
        {
            try
            {
                Stocks.Clear();
                var stocks = await _stockService.SearchStock(SearchTerm, SelectedLocation);
                
                foreach (var stock in stocks)
                {
                    Stocks.Add(stock);
                }
                
                if (stocks.Count() == 0)
                {
                    System.Windows.MessageBox.Show(
                        "No stock items found matching your search criteria.", 
                        "Information", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Search failed: {ex.Message}", 
                    "Error", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error);
            }
        }
        
        // 4. Clear Form - Implementation
        public void ClearForm()
        {
            SelectedStock = null;
            SelectedLocation = null;
            TransferLocation = null;
            Quantity = 0;
            SelectedProductId = 0;
            SelectedSupplierId = 0;
            SearchTerm = string.Empty;
        }
        
        // 5. Load Dead Stock - Implementation
        public async void LoadDeadStock()
        {
            try
            {
                Stocks.Clear();
                var deadStocks = await _stockService.GetDeadStock();
                
                foreach (var stock in deadStocks)
                {
                    Stocks.Add(stock);
                }
                
                if (deadStocks.Count() == 0)
                {
                    System.Windows.MessageBox.Show(
                        "No dead stock items found.", 
                        "Information", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        $"Found {deadStocks.Count()} items with no movement in the last 180 days.", 
                        "Dead Stock Report", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to load dead stock: {ex.Message}", 
                    "Error", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error);
            }
        }
        
        // 6. Load Low Stock - Implementation
        public async void LoadLowStock()
        {
            try
            {
                Stocks.Clear();
                var lowStocks = await _stockService.GetLowStock(ReorderThreshold);
                
                foreach (var stock in lowStocks)
                {
                    Stocks.Add(stock);
                }
                
                if (lowStocks.Count() == 0)
                {
                    System.Windows.MessageBox.Show(
                        $"No products below reorder threshold of {ReorderThreshold}.", 
                        "Information", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        $"Found {lowStocks.Count()} items below reorder threshold of {ReorderThreshold}.", 
                        "Low Stock Report", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to load low stock: {ex.Message}", 
                    "Error", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }
}