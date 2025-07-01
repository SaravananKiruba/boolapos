using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Page_Navigation_App.Model;
using Page_Navigation_App.Services;
using Page_Navigation_App.Utilities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Page_Navigation_App.ViewModel
{
    public class StockVM : ViewModelBase
    {
        private readonly StockService _stockService;
        private readonly ProductService _productService;
        private readonly SupplierService _supplierService;

        public ICommand SearchCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SearchStockItemCommand { get; }
        public ICommand ViewStockItemsCommand { get; }

        public StockVM(
            StockService stockService,
            ProductService productService,
            SupplierService supplierService)
        {
            _stockService = stockService;
            _productService = productService;
            _supplierService = supplierService;

            // Initialize collections
            StockSummary = new ObservableCollection<StockSummaryItem>();
            StockItems = new ObservableCollection<StockItem>();
            Products = new ObservableCollection<Product>();
            LowStockProducts = new ObservableCollection<Product>();

            // Load data
            LoadStockSummary();
            LoadProducts();
            LoadLowStockProducts();

            // Initialize commands
            SearchCommand = new RelayCommand<object>(_ => SearchStock(), _ => true);
            RefreshCommand = new RelayCommand<object>(_ => RefreshAll(), _ => true);
            SearchStockItemCommand = new RelayCommand<object>(_ => SearchStockItem(), _ => !string.IsNullOrEmpty(StockItemSearchTerm));
            ViewStockItemsCommand = new RelayCommand<Product>(ViewStockItemsForProduct, _ => true);
        }

        public ObservableCollection<StockSummaryItem> StockSummary { get; set; }
        public ObservableCollection<StockItem> StockItems { get; set; }
        public ObservableCollection<Product> Products { get; set; }
        public ObservableCollection<Product> LowStockProducts { get; set; }

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

        private string _stockItemSearchTerm;
        public string StockItemSearchTerm
        {
            get => _stockItemSearchTerm;
            set
            {
                _stockItemSearchTerm = value;
                OnPropertyChanged();
            }
        }

        private StockItem _selectedStockItem;
        public StockItem SelectedStockItem
        {
            get => _selectedStockItem;
            set
            {
                _selectedStockItem = value;
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
                if (value != null)
                {
                    LoadStockItemsForProduct(value.ProductID);
                }
            }
        }

        // Summary statistics
        private decimal _totalStockValue;
        public decimal TotalStockValue
        {
            get => _totalStockValue;
            set
            {
                _totalStockValue = value;
                OnPropertyChanged();
            }
        }

        private int _totalProducts;
        public int TotalProducts
        {
            get => _totalProducts;
            set
            {
                _totalProducts = value;
                OnPropertyChanged();
            }
        }

        private int _lowStockCount;
        public int LowStockCount
        {
            get => _lowStockCount;
            set
            {
                _lowStockCount = value;
                OnPropertyChanged();
            }
        }

        private async void LoadStockSummary()
        {
            try
            {
                StockSummary.Clear();
                var allStock = await _stockService.GetAllStock();
                var products = await _productService.GetAllProducts();

                var stockSummary = from stock in allStock
                                 group stock by stock.ProductID into g
                                 join product in products on g.Key equals product.ProductID
                                 select new StockSummaryItem
                                 {
                                     ProductID = g.Key,
                                     ProductName = product.ProductName,
                                     Barcode = product.Barcode,
                                     MetalType = product.MetalType,
                                     Purity = product.Purity,
                                     AvailableQuantity = g.Where(s => s.Status == "Available").Sum(s => s.Quantity),
                                     TotalQuantity = g.Sum(s => s.Quantity),
                                     AverageCost = g.Where(s => s.Status == "Available").Any() ? 
                                                  g.Where(s => s.Status == "Available").Average(s => s.UnitCost) : 0,
                                     TotalValue = g.Where(s => s.Status == "Available").Sum(s => s.Quantity * s.UnitCost),
                                     ReorderLevel = product.ReorderLevel,
                                     IsLowStock = g.Where(s => s.Status == "Available").Sum(s => s.Quantity) <= product.ReorderLevel,
                                     LastUpdated = g.Max(s => s.LastUpdated)
                                 };

                foreach (var item in stockSummary.OrderBy(s => s.ProductName))
                {
                    StockSummary.Add(item);
                }

                // Update summary statistics
                TotalStockValue = StockSummary.Sum(s => s.TotalValue);
                TotalProducts = StockSummary.Count;
                LowStockCount = StockSummary.Count(s => s.IsLowStock);
            }
            catch (Exception ex)
            {
                ShowMessageBox($"Error loading stock summary: {ex.Message}");
            }
        }

        private async void LoadProducts()
        {
            try
            {
                Products.Clear();
                var products = await _productService.GetAllProducts();
                foreach (var product in products)
                {
                    Products.Add(product);
                }
            }
            catch (Exception ex)
            {
                ShowMessageBox($"Error loading products: {ex.Message}");
            }
        }

        private async void LoadLowStockProducts()
        {
            try
            {
                LowStockProducts.Clear();
                var lowStockProducts = await _stockService.GetLowStockProducts();
                foreach (var product in lowStockProducts)
                {
                    LowStockProducts.Add(product);
                }
            }
            catch (Exception ex)
            {
                ShowMessageBox($"Error loading low stock products: {ex.Message}");
            }
        }

        private async void LoadStockItemsForProduct(int productId)
        {
            try
            {
                StockItems.Clear();
                var stockItems = await _stockService.GetAvailableStockItems(productId);
                foreach (var item in stockItems)
                {
                    StockItems.Add(item);
                }
            }
            catch (Exception ex)
            {
                ShowMessageBox($"Error loading stock items: {ex.Message}");
            }
        }

        private async void ViewStockItemsForProduct(Product product)
        {
            if (product != null)
            {
                SelectedProduct = product;
                await Task.Delay(100); // Small delay to ensure UI updates
                LoadStockItemsForProduct(product.ProductID);
            }
        }

        private async void SearchStock()
        {
            try
            {
                if (string.IsNullOrEmpty(SearchTerm))
                {
                    LoadStockSummary();
                    return;
                }

                StockSummary.Clear();
                var allStock = await _stockService.GetAllStock();
                var products = await _productService.GetAllProducts();

                var filteredProducts = products.Where(p =>
                    p.ProductName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.Barcode.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.MetalType.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.Purity.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

                var stockSummary = from stock in allStock
                                 group stock by stock.ProductID into g
                                 join product in filteredProducts on g.Key equals product.ProductID
                                 select new StockSummaryItem
                                 {
                                     ProductID = g.Key,
                                     ProductName = product.ProductName,
                                     Barcode = product.Barcode,
                                     MetalType = product.MetalType,
                                     Purity = product.Purity,
                                     AvailableQuantity = g.Where(s => s.Status == "Available").Sum(s => s.Quantity),
                                     TotalQuantity = g.Sum(s => s.Quantity),
                                     AverageCost = g.Where(s => s.Status == "Available").Any() ? 
                                                  g.Where(s => s.Status == "Available").Average(s => s.UnitCost) : 0,
                                     TotalValue = g.Where(s => s.Status == "Available").Sum(s => s.Quantity * s.UnitCost),
                                     ReorderLevel = product.ReorderLevel,
                                     IsLowStock = g.Where(s => s.Status == "Available").Sum(s => s.Quantity) <= product.ReorderLevel,
                                     LastUpdated = g.Max(s => s.LastUpdated)
                                 };

                foreach (var item in stockSummary.OrderBy(s => s.ProductName))
                {
                    StockSummary.Add(item);
                }
            }
            catch (Exception ex)
            {
                ShowMessageBox($"Error searching stock: {ex.Message}");
            }
        }

        private async void SearchStockItem()
        {
            try
            {
                if (string.IsNullOrEmpty(StockItemSearchTerm))
                {
                    return;
                }

                var stockItem = await _stockService.GetStockItemByIdentifier(StockItemSearchTerm);
                if (stockItem != null)
                {
                    SelectedStockItem = stockItem;
                    SelectedProduct = stockItem.Product;
                    
                    // Show details in a message box
                    var details = $"Stock Item Details:\n\n" +
                                 $"Tag ID: {stockItem.UniqueTagID}\n" +
                                 $"Barcode: {stockItem.Barcode}\n" +
                                 $"Product: {stockItem.Product?.ProductName}\n" +
                                 $"Status: {stockItem.Status}\n" +
                                 $"Purchase Cost: ₹{stockItem.PurchaseCost:N2}\n" +
                                 $"Selling Price: ₹{stockItem.SellingPrice:N2}\n" +
                                 $"Location: {stockItem.Location}\n" +
                                 $"Created: {stockItem.CreatedDate:dd-MMM-yyyy}";

                    if (stockItem.Status == "Sold")
                    {
                        details += $"\nSale Date: {stockItem.SaleDate:dd-MMM-yyyy}\n" +
                                  $"Customer ID: {stockItem.CustomerID}\n" +
                                  $"Profit: ₹{stockItem.Profit:N2}";
                    }

                    if (!string.IsNullOrEmpty(stockItem.HUID))
                    {
                        details += $"\nHUID: {stockItem.HUID}";
                    }

                    ShowMessageBox(details, "Stock Item Found", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    ShowMessageBox($"No stock item found with identifier: {StockItemSearchTerm}", "Not Found", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                ShowMessageBox($"Error searching stock item: {ex.Message}");
            }
        }

        private void RefreshAll()
        {
            LoadStockSummary();
            LoadProducts();
            LoadLowStockProducts();
            StockItems.Clear();
            SelectedProduct = null;
            SelectedStockItem = null;
            SearchTerm = string.Empty;
            StockItemSearchTerm = string.Empty;
        }

        // Helper method to safely show message boxes from async methods
        private void ShowMessageBox(string message, string title = "Error", System.Windows.MessageBoxButton button = System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage icon = System.Windows.MessageBoxImage.Error)
        {
            if (System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    System.Windows.MessageBox.Show(message, title, button, icon);
                }));
            }
        }
    }

    // Helper class for stock summary display
    public class StockSummaryItem
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string Barcode { get; set; }
        public string MetalType { get; set; }
        public string Purity { get; set; }
        public decimal AvailableQuantity { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal AverageCost { get; set; }
        public decimal TotalValue { get; set; }
        public int ReorderLevel { get; set; }
        public bool IsLowStock { get; set; }
        public DateTime LastUpdated { get; set; }

        public string StockStatus => IsLowStock ? "Low Stock" : "Normal";
        public string QuantityDisplay => $"{AvailableQuantity:N0} / {TotalQuantity:N0}";
        public string ValueDisplay => $"₹{TotalValue:N2}";
        public string CostDisplay => $"₹{AverageCost:N2}";
    }
}
