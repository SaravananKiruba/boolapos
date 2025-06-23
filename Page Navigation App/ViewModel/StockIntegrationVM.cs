using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using Page_Navigation_App.Model;
using Page_Navigation_App.Services;
using Page_Navigation_App.Utilities;

namespace Page_Navigation_App.ViewModel
{
    /// <summary>
    /// ViewModel to handle the integration between stock, purchase orders, and finance
    /// </summary>
    public class StockIntegrationVM : ViewModelBase
    {
        private readonly StockIntegrationService _integrationService;
        private readonly StockService _stockService;
        private readonly ProductService _productService;
        private readonly SupplierService _supplierService;
        private readonly PurchaseOrderService _purchaseOrderService;
        
        public ObservableCollection<Product> Products { get; } = new ObservableCollection<Product>();
        public ObservableCollection<Supplier> Suppliers { get; } = new ObservableCollection<Supplier>();
        public ObservableCollection<PurchaseOrder> RecentPurchases { get; } = new ObservableCollection<PurchaseOrder>();
        
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
        
        private decimal _newQuantity = 1;
        public decimal NewQuantity
        {
            get => _newQuantity;
            set
            {
                _newQuantity = value;
                OnPropertyChanged();
                CalculateTotal();
            }
        }
        
        private decimal _newPurchaseRate;
        public decimal NewPurchaseRate
        {
            get => _newPurchaseRate;
            set
            {
                _newPurchaseRate = value;
                OnPropertyChanged();
                CalculateTotal();
            }
        }
        
        private decimal _newTotalAmount;
        public decimal NewTotalAmount
        {
            get => _newTotalAmount;
            set
            {
                _newTotalAmount = value;
                OnPropertyChanged();
            }
        }
        
        public ICommand AddStockCommand { get; }
        public ICommand AddExpenseCommand { get; }
        
        public StockIntegrationVM(
            StockIntegrationService integrationService,
            StockService stockService,
            ProductService productService,
            SupplierService supplierService,
            PurchaseOrderService purchaseOrderService)
        {
            _integrationService = integrationService;
            _stockService = stockService;
            _productService = productService;
            _supplierService = supplierService;
            _purchaseOrderService = purchaseOrderService;
            
            AddStockCommand = new RelayCommand<object>(AddStock, CanAddStock);
            AddExpenseCommand = new RelayCommand<PurchaseOrder>(AddExpense);
            
            LoadDataAsync();
        }
        
        private async void LoadDataAsync()
        {
            await LoadProducts();
            await LoadSuppliers();
            await LoadRecentPurchases();
        }
        
        private async Task LoadProducts()
        {
            try
            {
                var products = await _productService.GetAllProducts();
                Products.Clear();
                foreach (var product in products)
                {
                    Products.Add(product);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading products: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async Task LoadSuppliers()
        {
            try
            {
                var suppliers = await _supplierService.GetAllSuppliers();
                Suppliers.Clear();
                foreach (var supplier in suppliers)
                {
                    Suppliers.Add(supplier);
                }
                
                if (Suppliers.Count > 0)
                {
                    SelectedSupplier = Suppliers[0];
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading suppliers: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async Task LoadRecentPurchases()
        {
            try
            {                var purchases = await _purchaseOrderService.GetPurchaseOrders();
                RecentPurchases.Clear();
                
                foreach (var purchase in purchases.Take(10))
                {
                    RecentPurchases.Add(purchase);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading recent purchases: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void CalculateTotal()
        {
            NewTotalAmount = NewQuantity * NewPurchaseRate;
        }
        
        private bool CanAddStock(object parameter)
        {
            return SelectedProduct != null && 
                   SelectedSupplier != null && 
                   NewQuantity > 0 && 
                   NewPurchaseRate > 0;
        }
        
        private async void AddStock(object parameter)
        {
            try
            {
                if (!CanAddStock(parameter)) return;
                
                var result = await _integrationService.AddStockWithItems(
                    SelectedProduct.ProductID,
                    SelectedSupplier.SupplierID,
                    NewQuantity,
                    NewPurchaseRate,
                    null, // invoice number (auto-generated)
                    "Main" // default location
                );
                
                if (result.stock != null)
                {
                    MessageBox.Show($"Successfully added {NewQuantity} units of {SelectedProduct.ProductName} to stock with {result.stockItems.Count} individual items.",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Reset form
                    ResetForm();
                    
                    // Reload data
                    await LoadRecentPurchases();
                }
                else
                {
                    MessageBox.Show("Failed to add stock. Please try again.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void ResetForm()
        {
            NewQuantity = 1;
            NewPurchaseRate = 0;
            NewTotalAmount = 0;
        }
        
        private async void AddExpense(PurchaseOrder purchaseOrder)
        {
            try
            {
                if (purchaseOrder == null) return;
                
                var result = await _integrationService.RecordPurchaseExpense(purchaseOrder.PurchaseOrderID);
                
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
                MessageBox.Show($"Error: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
