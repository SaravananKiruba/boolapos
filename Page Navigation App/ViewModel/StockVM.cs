using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Page_Navigation_App.Model;
using Page_Navigation_App.Services;
using Page_Navigation_App.Utilities;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Page_Navigation_App.ViewModel
{
    public class StockVM : ViewModelBase
    {        private readonly StockService _stockService;
        private readonly ProductService _productService;
        private readonly SupplierService _supplierService; // Added
        private readonly PurchaseOrderService _purchaseOrderService;
        private readonly FinanceService _financeService;

        // Adding missing properties
        private int _selectedProductId;
        public int SelectedProductId
        {
            get => _selectedProductId;
            set
            {
                _selectedProductId = value;
                OnPropertyChanged();
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
        
        private decimal _reorderThreshold = 5;
        public decimal ReorderThreshold
        {
            get => _reorderThreshold;
            set
            {
                _reorderThreshold = value;
                OnPropertyChanged();
            }
        }

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
        
        private Product _selectedProduct;
        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                OnPropertyChanged();
            }
        }
        
        private Supplier _selectedSupplier;
        public Supplier SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                _selectedSupplier = value;
                OnPropertyChanged();
            }
        }
        
        private decimal _quantityToAdd;
        public decimal QuantityToAdd
        {
            get => _quantityToAdd;
            set
            {
                _quantityToAdd = value;
                OnPropertyChanged();
                CalculateTotalAmount();
            }
        }
        
        private decimal _purchaseRate;
        public decimal PurchaseRate
        {
            get => _purchaseRate;
            set
            {
                _purchaseRate = value;
                OnPropertyChanged();
                CalculateTotalAmount();
            }
        }
        
        private decimal _totalAmount;
        public decimal TotalAmount
        {
            get => _totalAmount;
            set
            {
                _totalAmount = value;
                OnPropertyChanged();
            }
        }
        
        private string _invoiceNumber;
        public string InvoiceNumber
        {
            get => _invoiceNumber;
            set
            {
                _invoiceNumber = value;
                OnPropertyChanged();
            }
        }
        
        // Purchase List for display
        public ObservableCollection<PurchaseOrder> RecentPurchases { get; } = new ObservableCollection<PurchaseOrder>();
        
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
        public ICommand CreateExpenseCommand { get; }        
        public ICommand AddQuantityCommand { get; private set; }
        public ICommand OpenAddQuantityModalCommand { get; private set; }
        public ICommand CloseModalCommand { get; private set; }
        public ICommand AddFinanceExpenseCommand { get; private set; }
        
        public StockVM(
            StockService stockService, 
            ProductService productService,
            SupplierService supplierService,
            PurchaseOrderService purchaseOrderService,
            FinanceService financeService) // Add finance service
        {
            _stockService = stockService;
            _productService = productService;
            _supplierService = supplierService;
            _purchaseOrderService = purchaseOrderService;
            _financeService = financeService;
            
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
              AddQuantityCommand = new RelayCommand<object>(AddQuantityExecute, CanAddQuantity);
            OpenAddQuantityModalCommand = new RelayCommand<object>(OpenAddQuantityModal);
            CloseModalCommand = new RelayCommand<object>(CloseModal);
            AddFinanceExpenseCommand = new RelayCommand<PurchaseOrder>(AddFinanceExpense);
            
            CloseStockItemsCommand = new RelayCommand<object>(_ => CloseStockItems());
              // Load initial data asynchronously
            InitializeAsync();
        }

        private void CalculateTotalAmount()
        {
            TotalAmount = QuantityToAdd * PurchaseRate;
        }
        
        private async void InitializeAsync()
        {
            await LoadProducts();
            await LoadSuppliers();
            await LoadLocations();
            await LoadRecentPurchases();
        }
        
        private async Task LoadProducts()
        {
            Products.Clear();
            var products = await _productService.GetAllProducts();
            foreach (var product in products)
            {
                Products.Add(product);
            }
        }        private async Task LoadSuppliers()
        {
            Suppliers.Clear();
            var suppliers = await _supplierService.GetAllSuppliers();
            foreach (var supplier in suppliers)
            {
                Suppliers.Add(supplier);
            }
        }        private async Task LoadRecentPurchases()
        {
            var purchases = await _purchaseOrderService.GetPurchaseOrders(); // Use default parameters
            RecentPurchases.Clear();
            foreach (var purchase in purchases.Take(10))
            {
                RecentPurchases.Add(purchase);
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
        }        private async Task LoadLocations()
        {
            Locations.Clear();
            var locations = await _stockService.GetAllLocations();
            foreach (var location in locations)
            {
                Locations.Add(location);
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
                NewPurchase.Location = "Main"; // Default location
                
                var addedStock = await _stockService.AddStockWithPurchaseRecord(NewPurchase);
                
                if (addedStock != null)
                {
                    // Create expense record for this purchase
                    var expense = await _stockService.CreateExpenseForPurchase(
                        addedStock, 
                        "System");
                    
                    LastCreatedExpense = expense;
                    ShowExpenseConfirmation = expense != null;
                    
                    // Create a simple purchase order for this stock entry
                    await CreateSimplePurchaseOrder(addedStock);
                    
                    LoadStocks();
                    ClosePurchaseModal();
                    
                    System.Windows.MessageBox.Show(
                        "Purchase recorded successfully with stock items generated and expense entry created.", 
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

        private async Task<bool> CreateSimplePurchaseOrder(Stock stock)
        {
            try
            {
                if (stock == null) return false;

                // Get product and supplier details
                var product = await _productService.GetProductById(stock.ProductID);
                var supplier = await _supplierService.GetSupplierById(stock.SupplierID);
                if (product == null || supplier == null) return false;

                // Create purchase order
                var purchaseOrder = new PurchaseOrder
                {
                    SupplierID = stock.SupplierID,
                    PurchaseDate = stock.PurchaseDate,
                    Status = "Completed",
                    TotalAmount = stock.TotalAmount,
                    PaidAmount = stock.PaymentStatus == "Paid" ? stock.TotalAmount : 0,
                    BalanceAmount = stock.PaymentStatus == "Paid" ? 0 : stock.TotalAmount,
                    PaymentStatus = stock.PaymentStatus,
                    PaymentMethod = stock.PaymentStatus == "Paid" ? "Cash" : "Credit",
                    ActualDeliveryDate = DateTime.Now,
                    Notes = $"Auto-generated purchase order for stock ID {stock.StockID}"
                };

                // Create purchase order item
                var item = new PurchaseOrderItem
                {
                    ProductID = stock.ProductID,
                    Quantity = stock.QuantityPurchased,
                    UnitPrice = stock.PurchaseRate,
                    TotalPrice = stock.TotalAmount,
                    Notes = $"Auto-generated for stock entry"
                };

                // Add purchase order
                var result = await _purchaseOrderService.CreatePurchaseOrder(purchaseOrder, new List<PurchaseOrderItem> { item });
                
                return result != null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating purchase order: {ex.Message}");
                return false;
            }
        }

        private void OpenAddQuantityModal(object parameter)
        {
            // If parameter is a product, set selected product
            if (parameter is Product product)
            {
                SelectedProduct = product;
            }
            
            // Default values
            QuantityToAdd = 1;
            PurchaseRate = 0;
            TotalAmount = 0;
            InvoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
            
            // If there's at least one supplier, select the first one
            if (Suppliers.Count > 0)
            {
                SelectedSupplier = Suppliers[0];
            }
            
            IsPurchaseModalOpen = true;
        }
        
        private void CloseModal(object parameter)
        {
            IsPurchaseModalOpen = false;
        }
        
        private bool CanAddQuantity(object parameter)
        {
            return SelectedProduct != null && SelectedSupplier != null && 
                   QuantityToAdd > 0 && PurchaseRate > 0;
        }
        
        private async void AddQuantityExecute(object parameter)
        {
            try
            {
                // Create a stock object
                var stock = new Stock
                {
                    ProductID = SelectedProduct.ProductID,
                    SupplierID = SelectedSupplier.SupplierID,
                    QuantityPurchased = QuantityToAdd,
                    PurchaseRate = PurchaseRate,
                    TotalAmount = TotalAmount,
                    InvoiceNumber = InvoiceNumber,
                    PaymentStatus = "Pending",
                    Location = SelectedLocation ?? "Main"
                };
                
                // Add stock with purchase order
                var (newStock, purchaseOrder) = await _stockService.AddStockWithPurchaseOrder(stock, SelectedSupplier.SupplierID);
                
                if (newStock != null && purchaseOrder != null)
                {
                    // Show success message
                    MessageBox.Show($"Successfully added {QuantityToAdd} units of {SelectedProduct.ProductName} to stock", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Reload data
                    await LoadRecentPurchases();
                    
                    // Close modal
                    IsPurchaseModalOpen = false;
                }
                else
                {
                    MessageBox.Show("Failed to add stock. Please try again.", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async void AddFinanceExpense(PurchaseOrder purchaseOrder)
        {
            try
            {
                if (purchaseOrder == null) return;
                
                var result = await _financeService.RecordPurchaseExpenseAsync(purchaseOrder);
                
                if (result.success)
                {
                    MessageBox.Show($"Successfully recorded expense for purchase order {purchaseOrder.PurchaseOrderNumber}", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to record expense. Please try again.", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }    }
}