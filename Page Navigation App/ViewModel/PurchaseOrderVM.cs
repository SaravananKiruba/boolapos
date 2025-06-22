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
{    public class PurchaseOrderVM : ViewModelBase
    {
        private readonly PurchaseOrderService _purchaseOrderService;
        private readonly ProductService _productService;
        private readonly SupplierService _supplierService;
        
        public ObservableCollection<PurchaseOrder> PurchaseOrders { get; } = new ObservableCollection<PurchaseOrder>();
        public ObservableCollection<Product> Products { get; } = new ObservableCollection<Product>();
        public ObservableCollection<Supplier> Suppliers { get; } = new ObservableCollection<Supplier>();
        public ObservableCollection<PurchaseOrderItem> OrderItems { get; } = new ObservableCollection<PurchaseOrderItem>();
        
        private PurchaseOrder _selectedPurchaseOrder;
        public PurchaseOrder SelectedPurchaseOrder
        {
            get => _selectedPurchaseOrder;
            set
            {
                _selectedPurchaseOrder = value;
                OnPropertyChanged();
                LoadOrderItems();
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
        
        private Product _selectedProduct;
        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                OnPropertyChanged();
                
                // Update the unit price when product changes
                if (_selectedProduct != null)
                {
                    UnitPrice = _selectedProduct.ProductPrice;
                }
            }
        }
        
        private PurchaseOrderItem _selectedOrderItem;
        public PurchaseOrderItem SelectedOrderItem
        {
            get => _selectedOrderItem;
            set
            {
                _selectedOrderItem = value;
                OnPropertyChanged();
            }
        }
        
        private decimal _quantity = 1;
        public decimal Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged();
                UpdateTotalPrice();
            }
        }
        
        private decimal _unitPrice;
        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                _unitPrice = value;
                OnPropertyChanged();
                UpdateTotalPrice();
            }
        }
        
        private decimal _totalPrice;
        public decimal TotalPrice
        {
            get => _totalPrice;
            set
            {
                _totalPrice = value;
                OnPropertyChanged();
            }
        }
        
        private decimal _totalOrderAmount;
        public decimal TotalOrderAmount
        {
            get => _totalOrderAmount;
            set
            {
                _totalOrderAmount = value;
                OnPropertyChanged();
                UpdateBalanceAmount();
            }
        }
        
        private decimal _paidAmount;
        public decimal PaidAmount
        {
            get => _paidAmount;
            set
            {
                _paidAmount = value;
                OnPropertyChanged();
                UpdateBalanceAmount();
            }
        }
        
        private decimal _balanceAmount;
        public decimal BalanceAmount
        {
            get => _balanceAmount;
            set
            {
                _balanceAmount = value;
                OnPropertyChanged();
            }
        }
        
        private string _paymentMethod = "Cash";
        public string PaymentMethod
        {
            get => _paymentMethod;
            set
            {
                _paymentMethod = value;
                OnPropertyChanged();
            }
        }
        
        private bool _hasExpenseEntry;
        public bool HasExpenseEntry
        {
            get => _hasExpenseEntry;
            set
            {
                _hasExpenseEntry = value;
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
        
        private string _notes;
        public string Notes
        {
            get => _notes;
            set
            {
                _notes = value;
                OnPropertyChanged();
            }
        }
        
        // Commands
        public ICommand CreatePurchaseOrderCommand { get; }
        public ICommand AddItemCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public ICommand CompletePurchaseOrderCommand { get; }
        public ICommand CreateExpenseCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearFormCommand { get; }
          public PurchaseOrderVM(
            PurchaseOrderService purchaseOrderService = null,
            ProductService productService = null,
            SupplierService supplierService = null)
        {
            _purchaseOrderService = purchaseOrderService;
            _productService = productService;
            _supplierService = supplierService;
            
            CreatePurchaseOrderCommand = new RelayCommand<object>(_ => CreatePurchaseOrder(), _ => CanCreatePurchaseOrder());
            AddItemCommand = new RelayCommand<object>(_ => AddItem(), _ => CanAddItem());
            RemoveItemCommand = new RelayCommand<object>(_ => RemoveItem(), _ => SelectedOrderItem != null);
            CompletePurchaseOrderCommand = new RelayCommand<object>(_ => CompletePurchaseOrder(), _ => SelectedPurchaseOrder != null && SelectedPurchaseOrder?.Status == "Pending");
            CreateExpenseCommand = new RelayCommand<object>(_ => CreateExpenseEntry(), _ => SelectedPurchaseOrder != null && !SelectedPurchaseOrder.HasExpenseEntry);
            SearchCommand = new RelayCommand<object>(_ => SearchPurchaseOrders());
            ClearFormCommand = new RelayCommand<object>(_ => ClearForm());
            
            // Only load data if we have services (runtime only, not design-time)
            if (_purchaseOrderService != null && _productService != null && _supplierService != null)
            {
                LoadPurchaseOrders();
                LoadProducts();
                LoadSuppliers();
            }
        }
          private async void LoadPurchaseOrders()
        {
            if (_purchaseOrderService == null) return;
            
            try
            {
                var purchaseOrders = await _purchaseOrderService.GetPurchaseOrders();
                
                PurchaseOrders.Clear();
                if (purchaseOrders != null)
                {
                    foreach (var order in purchaseOrders)
                    {
                        PurchaseOrders.Add(order);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading purchase orders: {ex.Message}");
            }
        }
        
        private async void LoadProducts()
        {
            if (_productService == null) return;
            
            try
            {
                var products = await _productService.GetAllProducts();
                
                Products.Clear();
                if (products != null)
                {
                    foreach (var product in products)
                    {
                        Products.Add(product);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading products: {ex.Message}");
            }
        }
        
        private async void LoadSuppliers()
        {
            if (_supplierService == null) return;
            
            try
            {
                var suppliers = await _supplierService.GetAllSuppliers();
                
                Suppliers.Clear();
                if (suppliers != null)
                {
                    foreach (var supplier in suppliers)
                    {
                        Suppliers.Add(supplier);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading suppliers: {ex.Message}");
            }
        }
          private async void LoadOrderItems()
        {
            if (_purchaseOrderService == null || SelectedPurchaseOrder == null) return;
            
            try
            {
                var purchaseOrder = await _purchaseOrderService.GetPurchaseOrderById(SelectedPurchaseOrder.PurchaseOrderID);
                if (purchaseOrder == null) return;
                
                OrderItems.Clear();
                if (purchaseOrder.PurchaseOrderItems != null)
                {
                    foreach (var item in purchaseOrder.PurchaseOrderItems)
                    {
                        OrderItems.Add(item);
                    }
                }
                
                // Update the order totals
                TotalOrderAmount = purchaseOrder.TotalAmount;
                PaidAmount = purchaseOrder.PaidAmount;
                BalanceAmount = purchaseOrder.BalanceAmount;
                PaymentMethod = purchaseOrder.PaymentMethod;
                HasExpenseEntry = purchaseOrder.HasExpenseEntry;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading order items: {ex.Message}");
            }
        }
        
        private void UpdateTotalPrice()
        {
            TotalPrice = UnitPrice * Quantity;
        }
        
        private void UpdateBalanceAmount()
        {
            BalanceAmount = TotalOrderAmount - PaidAmount;
        }
          private async void CreatePurchaseOrder()
        {
            if (_purchaseOrderService == null || SelectedSupplier == null || OrderItems.Count == 0) return;
            
            try
            {
                var purchaseOrder = new PurchaseOrder
                {
                    SupplierID = SelectedSupplier.SupplierID,
                    PurchaseDate = DateTime.Now,
                    Status = "Pending",
                    TotalAmount = OrderItems.Sum(i => i.TotalPrice),
                    PaidAmount = PaidAmount,
                    BalanceAmount = OrderItems.Sum(i => i.TotalPrice) - PaidAmount,
                    PaymentStatus = PaidAmount >= OrderItems.Sum(i => i.TotalPrice) ? "Paid" : 
                                    PaidAmount > 0 ? "Partial" : "Pending",
                    PaymentMethod = PaymentMethod,
                    DeliveryAddress = SelectedSupplier.Address,
                    ExpectedDeliveryDate = DateTime.Now.AddDays(7), // Default to 7 days
                    Notes = Notes
                };
                
                var items = OrderItems.ToList();
                
                var createdOrder = await _purchaseOrderService.CreatePurchaseOrder(purchaseOrder, items);
                if (createdOrder != null)
                {
                    PurchaseOrders.Add(createdOrder);
                    SelectedPurchaseOrder = createdOrder;
                    ClearForm();
                }
                
                LoadPurchaseOrders();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating purchase order: {ex.Message}");
            }
        }
        
        private void AddItem()
        {
            if (SelectedProduct == null || Quantity <= 0) return;
            
            var item = new PurchaseOrderItem
            {
                ProductID = SelectedProduct.ProductID,
                Quantity = Quantity,
                UnitPrice = UnitPrice,
                TotalPrice = TotalPrice,
                Product = SelectedProduct
            };
            
            OrderItems.Add(item);
            
            // Update the order total
            TotalOrderAmount = OrderItems.Sum(i => i.TotalPrice);
            UpdateBalanceAmount();
            
            // Clear the form for the next item
            SelectedProduct = null;
            Quantity = 1;
            UnitPrice = 0;
            TotalPrice = 0;
        }
        
        private void RemoveItem()
        {
            if (SelectedOrderItem == null) return;
            
            OrderItems.Remove(SelectedOrderItem);
            
            // Update the order total
            TotalOrderAmount = OrderItems.Sum(i => i.TotalPrice);
            UpdateBalanceAmount();
            
            SelectedOrderItem = null;
        }
          private async void CompletePurchaseOrder()
        {
            if (_purchaseOrderService == null || SelectedPurchaseOrder == null) return;
            
            try
            {
                var result = await _purchaseOrderService.CompletePurchaseOrder(SelectedPurchaseOrder.PurchaseOrderID);
                if (result)
                {
                    // Refresh the purchase order
                    var updatedOrder = await _purchaseOrderService.GetPurchaseOrderById(SelectedPurchaseOrder.PurchaseOrderID);
                    
                    // Update in collection
                    var index = PurchaseOrders.IndexOf(SelectedPurchaseOrder);
                    if (index >= 0 && updatedOrder != null)
                    {
                        PurchaseOrders[index] = updatedOrder;
                    }
                    
                    SelectedPurchaseOrder = updatedOrder;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error completing purchase order: {ex.Message}");
            }
        }
          private async void CreateExpenseEntry()
        {
            if (_purchaseOrderService == null || SelectedPurchaseOrder == null || SelectedPurchaseOrder.HasExpenseEntry) return;
            
            try
            {
                var expense = await _purchaseOrderService.CreateExpenseFromPurchaseOrder(
                    SelectedPurchaseOrder.PurchaseOrderID, 
                    Notes);
                    
                if (expense != null)
                {
                    // Update the purchase order HasExpenseEntry flag
                    SelectedPurchaseOrder.HasExpenseEntry = true;
                    HasExpenseEntry = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating expense entry: {ex.Message}");
            }
        }
        
        private async void SearchPurchaseOrders()
        {
            if (_purchaseOrderService == null) return;
            
            try
            {
                // Use search term to filter supplier name or purchase order number
                var purchaseOrders = await _purchaseOrderService.GetPurchaseOrders();
                
                PurchaseOrders.Clear();
                if (purchaseOrders != null)
                {
                    foreach (var order in purchaseOrders.Where(po => 
                        string.IsNullOrWhiteSpace(SearchTerm) || 
                        po.PurchaseOrderNumber?.Contains(SearchTerm) == true || 
                        po.Supplier?.SupplierName?.Contains(SearchTerm) == true))
                    {
                        PurchaseOrders.Add(order);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error searching purchase orders: {ex.Message}");
            }
        }
        
        private void ClearForm()
        {
            SelectedProduct = null;
            Quantity = 1;
            UnitPrice = 0;
            TotalPrice = 0;
            Notes = string.Empty;
            OrderItems.Clear();
            TotalOrderAmount = 0;
            PaidAmount = 0;
            BalanceAmount = 0;
        }
          private bool CanCreatePurchaseOrder()
        {
            return _purchaseOrderService != null && SelectedSupplier != null && OrderItems.Count > 0;
        }
        
        private bool CanAddItem()
        {
            return SelectedProduct != null && Quantity > 0 && UnitPrice > 0;
        }
    }
}
