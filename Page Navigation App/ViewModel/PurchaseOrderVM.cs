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
    public class PurchaseOrderVM : ViewModelBase
    {
        private readonly PurchaseOrderService _purchaseOrderService;
        private readonly SupplierService _supplierService;
        private readonly ProductService _productService;
        private readonly StockService _stockService;

        public ICommand AddOrUpdateCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand AddItemCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public ICommand ReceivePurchaseOrderCommand { get; }
        public ICommand RecordPaymentCommand { get; }
        public ICommand CancelPurchaseOrderCommand { get; }

        public PurchaseOrderVM(
            PurchaseOrderService purchaseOrderService,
            SupplierService supplierService,
            ProductService productService,
            StockService stockService)
        {
            _purchaseOrderService = purchaseOrderService;
            _supplierService = supplierService;
            _productService = productService;
            _stockService = stockService;

            // Initialize SelectedPurchaseOrder
            SelectedPurchaseOrder = new PurchaseOrder
            {
                OrderDate = DateOnly.FromDateTime(DateTime.Now),
                ExpectedDeliveryDate = DateOnly.FromDateTime(DateTime.Now.AddDays(7)),
                Status = "Pending",
                PaymentMethod = "Cash",
                PaymentStatus = "Pending"
            };

            // Initialize collections
            PurchaseOrders = new ObservableCollection<PurchaseOrder>();
            Suppliers = new ObservableCollection<Supplier>();
            Products = new ObservableCollection<Product>();
            PurchaseOrderItems = new ObservableCollection<PurchaseOrderItem>();
            PaymentMethods = new ObservableCollection<string>
            {
                "Cash",
                "Bank Transfer",
                "Cheque",
                "Credit",
                "UPI"
            };

            // Load data
            LoadPurchaseOrders();
            LoadSuppliers();
            LoadProducts();

            // Initialize commands
            AddOrUpdateCommand = new RelayCommand<object>(_ => AddOrUpdatePurchaseOrder(), _ => CanAddOrUpdatePurchaseOrder());
            ClearCommand = new RelayCommand<object>(_ => ClearForm(), _ => true);
            SearchCommand = new RelayCommand<object>(_ => SearchPurchaseOrders(), _ => true);
            AddItemCommand = new RelayCommand<object>(_ => AddPurchaseOrderItem(), _ => CanAddPurchaseOrderItem());
            RemoveItemCommand = new RelayCommand<PurchaseOrderItem>(RemovePurchaseOrderItem, _ => true);
            ReceivePurchaseOrderCommand = new RelayCommand<object>(_ => ReceivePurchaseOrder(), _ => SelectedPurchaseOrder?.PurchaseOrderID > 0);
            RecordPaymentCommand = new RelayCommand<object>(_ => RecordPayment(), _ => SelectedPurchaseOrder?.PurchaseOrderID > 0);
            CancelPurchaseOrderCommand = new RelayCommand<object>(_ => CancelPurchaseOrder(), _ => SelectedPurchaseOrder?.PurchaseOrderID > 0);
        }

        public ObservableCollection<PurchaseOrder> PurchaseOrders { get; set; }
        public ObservableCollection<Supplier> Suppliers { get; set; }
        public ObservableCollection<Product> Products { get; set; }
        public ObservableCollection<PurchaseOrderItem> PurchaseOrderItems { get; set; }
        public ObservableCollection<string> PaymentMethods { get; set; }

        private PurchaseOrder _selectedPurchaseOrder;
        public PurchaseOrder SelectedPurchaseOrder
        {
            get => _selectedPurchaseOrder;
            set
            {
                _selectedPurchaseOrder = value;
                OnPropertyChanged();
                if (value != null && value.PurchaseOrderID > 0)
                {
                    LoadPurchaseOrderItems(value.PurchaseOrderID);
                }
            }
        }

        private PurchaseOrderItem _selectedPurchaseOrderItem;
        public PurchaseOrderItem SelectedPurchaseOrderItem
        {
            get => _selectedPurchaseOrderItem;
            set
            {
                _selectedPurchaseOrderItem = value;
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
                if (value != null)
                {
                    SelectedPurchaseOrder.SupplierID = value.SupplierID;
                    SelectedPurchaseOrder.Supplier = value;
                }
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
                if (value != null && value.ProductID > 0)
                {
                    SelectedPurchaseOrderItem = new PurchaseOrderItem
                    {
                        ProductID = value.ProductID,
                        Product = value,
                        UnitCost = value.ProductPrice * 0.8m, // Assume 20% margin
                        Quantity = 1
                    };
                    CalculatePurchaseOrderItemTotal();
                }
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

        private decimal _paymentAmount;
        public decimal PaymentAmount
        {
            get => _paymentAmount;
            set
            {
                _paymentAmount = value;
                OnPropertyChanged();
            }
        }

        private string _paymentNotes;
        public string PaymentNotes
        {
            get => _paymentNotes;
            set
            {
                _paymentNotes = value;
                OnPropertyChanged();
            }
        }

        private async void LoadPurchaseOrders()
        {
            try
            {
                PurchaseOrders.Clear();
                var purchaseOrders = await _purchaseOrderService.GetAllPurchaseOrders();
                foreach (var po in purchaseOrders)
                {
                    PurchaseOrders.Add(po);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading purchase orders: {ex.Message}", "Error");
            }
        }

        private async void LoadSuppliers()
        {
            try
            {
                Suppliers.Clear();
                var suppliers = await _supplierService.GetAllSuppliers();
                foreach (var supplier in suppliers)
                {
                    Suppliers.Add(supplier);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading suppliers: {ex.Message}", "Error");
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
                System.Windows.MessageBox.Show($"Error loading products: {ex.Message}", "Error");
            }
        }

        private async void LoadPurchaseOrderItems(int purchaseOrderId)
        {
            try
            {
                PurchaseOrderItems.Clear();
                var purchaseOrder = await _purchaseOrderService.GetPurchaseOrderById(purchaseOrderId);
                if (purchaseOrder?.PurchaseOrderItems != null)
                {
                    foreach (var item in purchaseOrder.PurchaseOrderItems)
                    {
                        PurchaseOrderItems.Add(item);
                    }
                }
                UpdatePurchaseOrderTotal();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading purchase order items: {ex.Message}", "Error");
            }
        }

        private void CalculatePurchaseOrderItemTotal()
        {
            if (SelectedProduct != null && SelectedProduct.ProductID > 0 && SelectedPurchaseOrderItem != null)
            {
                SelectedPurchaseOrderItem.TotalAmount = Math.Round(SelectedPurchaseOrderItem.UnitCost * SelectedPurchaseOrderItem.Quantity, 2);
                OnPropertyChanged(nameof(SelectedPurchaseOrderItem));
            }
        }

        private void UpdatePurchaseOrderTotal()
        {
            if (SelectedPurchaseOrder != null)
            {
                SelectedPurchaseOrder.TotalAmount = PurchaseOrderItems.Sum(item => item.TotalAmount);
                SelectedPurchaseOrder.TotalItems = PurchaseOrderItems.Count;
                SelectedPurchaseOrder.TaxAmount = Math.Round(SelectedPurchaseOrder.TotalAmount * 0.18m, 2); // 18% GST
                SelectedPurchaseOrder.GrandTotal = SelectedPurchaseOrder.TotalAmount + SelectedPurchaseOrder.TaxAmount - SelectedPurchaseOrder.DiscountAmount;
                OnPropertyChanged(nameof(SelectedPurchaseOrder));
            }
        }

        private void AddPurchaseOrderItem()
        {
            try
            {
                if (SelectedProduct == null)
                {
                    System.Windows.MessageBox.Show("Please select a product first.", "No Product Selected");
                    return;
                }

                if (SelectedPurchaseOrderItem.Quantity <= 0)
                {
                    System.Windows.MessageBox.Show("Please enter a valid quantity.", "Invalid Quantity");
                    return;
                }

                if (SelectedPurchaseOrderItem.UnitCost <= 0)
                {
                    System.Windows.MessageBox.Show("Please enter a valid unit cost.", "Invalid Cost");
                    return;
                }

                // Check if product already exists
                var existingItem = PurchaseOrderItems.FirstOrDefault(item => item.ProductID == SelectedProduct.ProductID);
                if (existingItem != null)
                {
                    existingItem.Quantity += SelectedPurchaseOrderItem.Quantity;
                    existingItem.TotalAmount = existingItem.UnitCost * existingItem.Quantity;
                }
                else
                {
                    var newItem = new PurchaseOrderItem
                    {
                        ProductID = SelectedProduct.ProductID,
                        Product = SelectedProduct,
                        Quantity = SelectedPurchaseOrderItem.Quantity,
                        UnitCost = SelectedPurchaseOrderItem.UnitCost,
                        TotalAmount = SelectedPurchaseOrderItem.UnitCost * SelectedPurchaseOrderItem.Quantity
                    };
                    PurchaseOrderItems.Add(newItem);
                }

                UpdatePurchaseOrderTotal();
                SelectedProduct = null;
                SelectedPurchaseOrderItem = null;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error adding item: {ex.Message}", "Error");
            }
        }

        private void RemovePurchaseOrderItem(PurchaseOrderItem item)
        {
            try
            {
                if (item != null)
                {
                    PurchaseOrderItems.Remove(item);
                    UpdatePurchaseOrderTotal();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error removing item: {ex.Message}", "Error");
            }
        }

        private async void AddOrUpdatePurchaseOrder()
        {
            try
            {
                if (SelectedSupplier == null)
                {
                    System.Windows.MessageBox.Show("Please select a supplier.", "No Supplier Selected");
                    return;
                }

                if (PurchaseOrderItems.Count == 0)
                {
                    System.Windows.MessageBox.Show("Please add at least one item to the purchase order.", "No Items");
                    return;
                }

                var items = PurchaseOrderItems.ToList();

                if (SelectedPurchaseOrder.PurchaseOrderID > 0)
                {
                    // Update existing purchase order
                    var success = await _purchaseOrderService.UpdatePurchaseOrder(SelectedPurchaseOrder);
                    if (success)
                    {
                        System.Windows.MessageBox.Show("Purchase order updated successfully!", "Success");
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Failed to update purchase order.", "Error");
                    }
                }
                else
                {
                    // Create new purchase order
                    var newPurchaseOrder = await _purchaseOrderService.CreatePurchaseOrder(SelectedPurchaseOrder, items);
                    if (newPurchaseOrder != null)
                    {
                        SelectedPurchaseOrder = newPurchaseOrder;
                        System.Windows.MessageBox.Show("Purchase order created successfully!", "Success");
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Failed to create purchase order.", "Error");
                    }
                }

                LoadPurchaseOrders();
                ClearForm();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving purchase order: {ex.Message}", "Error");
            }
        }

        private async void ReceivePurchaseOrder()
        {
            try
            {
                if (SelectedPurchaseOrder?.PurchaseOrderID <= 0)
                {
                    System.Windows.MessageBox.Show("Please select a purchase order.", "No Purchase Order Selected");
                    return;
                }

                var result = System.Windows.MessageBox.Show(
                    $"Mark Purchase Order #{SelectedPurchaseOrder.PurchaseOrderNumber} as received and add items to stock?",
                    "Confirm Receipt",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    var purchaseOrderId = SelectedPurchaseOrder.PurchaseOrderID;
                    var success = await _purchaseOrderService.ReceivePurchaseOrder(purchaseOrderId);
                    if (success)
                    {
                        System.Windows.MessageBox.Show("Purchase order received successfully! Items added to stock.", "Success");
                        LoadPurchaseOrders();
                        LoadPurchaseOrderItems(purchaseOrderId);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Failed to receive purchase order.", "Error");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error receiving purchase order: {ex.Message}", "Error");
            }
        }

        private async void RecordPayment()
        {
            try
            {
                if (SelectedPurchaseOrder?.PurchaseOrderID <= 0)
                {
                    System.Windows.MessageBox.Show("Please select a purchase order.", "No Purchase Order Selected");
                    return;
                }

                if (PaymentAmount <= 0)
                {
                    System.Windows.MessageBox.Show("Please enter a valid payment amount.", "Invalid Amount");
                    return;
                }

                var purchaseOrderId = SelectedPurchaseOrder.PurchaseOrderID;
                var success = await _purchaseOrderService.RecordPayment(
                    purchaseOrderId,
                    PaymentAmount,
                    SelectedPurchaseOrder.PaymentMethod,
                    PaymentNotes);

                if (success)
                {
                    System.Windows.MessageBox.Show($"Payment of ₹{PaymentAmount:N2} recorded successfully!", "Success");
                    LoadPurchaseOrders();
                    LoadPurchaseOrderItems(purchaseOrderId);
                    PaymentAmount = 0;
                    PaymentNotes = string.Empty;
                }
                else
                {
                    System.Windows.MessageBox.Show("Failed to record payment.", "Error");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error recording payment: {ex.Message}", "Error");
            }
        }

        private async void CancelPurchaseOrder()
        {
            try
            {
                if (SelectedPurchaseOrder?.PurchaseOrderID <= 0)
                {
                    System.Windows.MessageBox.Show("Please select a purchase order.", "No Purchase Order Selected");
                    return;
                }

                var reason = "Cancelled by user"; // Default reason
                
                // Create a simple input dialog using MessageBox (you can create a proper input dialog later)
                var result = System.Windows.MessageBox.Show(
                    "Are you sure you want to cancel this purchase order?",
                    "Cancel Purchase Order",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    var purchaseOrderId = SelectedPurchaseOrder.PurchaseOrderID;
                    var success = await _purchaseOrderService.CancelPurchaseOrder(purchaseOrderId, reason);
                    if (success)
                    {
                        System.Windows.MessageBox.Show("Purchase order cancelled successfully!", "Success");
                        LoadPurchaseOrders();
                        LoadPurchaseOrderItems(purchaseOrderId);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Failed to cancel purchase order.", "Error");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error cancelling purchase order: {ex.Message}", "Error");
            }
        }

        private void ClearForm()
        {
            SelectedPurchaseOrder = new PurchaseOrder
            {
                OrderDate = DateOnly.FromDateTime(DateTime.Now),
                ExpectedDeliveryDate = DateOnly.FromDateTime(DateTime.Now.AddDays(7)),
                Status = "Pending",
                PaymentMethod = "Cash",
                PaymentStatus = "Pending"
            };

            PurchaseOrderItems.Clear();
            SelectedProduct = null;
            SelectedPurchaseOrderItem = null;
            SelectedSupplier = null;
            SearchTerm = string.Empty;
            PaymentAmount = 0;
            PaymentNotes = string.Empty;
        }

        private bool CanAddOrUpdatePurchaseOrder()
        {
            return SelectedSupplier != null && PurchaseOrderItems.Count > 0;
        }

        private bool CanAddPurchaseOrderItem()
        {
            return SelectedProduct != null && SelectedPurchaseOrderItem != null &&
                   SelectedPurchaseOrderItem.Quantity > 0 && SelectedPurchaseOrderItem.UnitCost > 0;
        }

        private async void SearchPurchaseOrders()
        {
            try
            {
                if (string.IsNullOrEmpty(SearchTerm))
                {
                    LoadPurchaseOrders();
                    return;
                }

                PurchaseOrders.Clear();
                var allPurchaseOrders = await _purchaseOrderService.GetAllPurchaseOrders();
                var filteredPurchaseOrders = allPurchaseOrders.Where(po =>
                    po.PurchaseOrderNumber.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    po.Supplier.SupplierName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    po.Status.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

                foreach (var po in filteredPurchaseOrders)
                {
                    PurchaseOrders.Add(po);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error searching purchase orders: {ex.Message}", "Error");
            }
        }

        // Property for displaying discount amount in the UI
        public decimal DiscountAmount
        {
            get => SelectedPurchaseOrder?.DiscountAmount ?? 0;
            set
            {
                if (SelectedPurchaseOrder != null)
                {
                    SelectedPurchaseOrder.DiscountAmount = value;
                    UpdatePurchaseOrderTotal();
                    OnPropertyChanged();
                }
            }
        }
    }
}
