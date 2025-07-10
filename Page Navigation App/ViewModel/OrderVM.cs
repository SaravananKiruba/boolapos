using Page_Navigation_App.Model;
using System;
using Page_Navigation_App.Utilities;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Page_Navigation_App.Services;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using System.Diagnostics;

namespace Page_Navigation_App.ViewModel
{
    public class OrderVM : ViewModelBase
    {
        private readonly EnhancedWorkflowService _enhancedWorkflowService;
        private readonly OrderService _orderService;
        private readonly CustomerService _customerService;
        private readonly ProductService _productService;
        private readonly FinanceService _financeService;
        private readonly BarcodeService _barcodeService;

        public ICommand AddOrUpdateCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand SearchByDateCommand { get; }
        public ICommand SearchByCustomerCommand { get; }
        public ICommand AddOrderItemCommand { get; }
        public ICommand RemoveOrderItemCommand { get; }
        public ICommand GenerateInvoiceCommand { get; }
        public ICommand CreateFinanceEntryCommand { get; }
        public ICommand CreateExpenseEntryCommand { get; }
        public ICommand ScanBarcodeCommand { get; }
        public ICommand ProcessBarcodeOrderCommand { get; }

        // Add a parameterless constructor to allow XAML instantiation
        public OrderVM() 
        {
            // Initialize collections to prevent null reference exceptions
            Orders = new ObservableCollection<Order>();
            Customers = new ObservableCollection<Customer>();
            Products = new ObservableCollection<Product>();
            OrderItems = new ObservableCollection<OrderDetail>();
            
            // Initialize SelectedOrder but NOT SelectedOrderItem to prevent empty records
            SelectedOrder = new Order
            {
                OrderDate = DateOnly.FromDateTime(DateTime.Now),
                Status = "Pending",
                PaymentMethod = "Cash",
                DiscountAmount = 0
            };
        }        public OrderVM(EnhancedWorkflowService enhancedWorkflowService, OrderService orderService, 
                      CustomerService customerService, ProductService productService, 
                      FinanceService financeService, BarcodeService barcodeService)
        {
            _enhancedWorkflowService = enhancedWorkflowService;
            _orderService = orderService;
            _customerService = customerService;
            _productService = productService;
            _financeService = financeService;
            _barcodeService = barcodeService;
            
            // Initialize SelectedOrder to avoid null reference exceptions
            SelectedOrder = new Order
            {
                OrderDate = DateOnly.FromDateTime(DateTime.Now),
                Status = "Pending",
                PaymentMethod = "Cash"
            };
            
            // Initialize collections
            Orders = new ObservableCollection<Order>();
            Customers = new ObservableCollection<Customer>();
            Products = new ObservableCollection<Product>();
            OrderItems = new ObservableCollection<OrderDetail>();
            
            // Load data in proper order
            Task.Run(async () => {
                await Task.Delay(100); // Small delay to ensure UI is loaded
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    LoadOrders();
                    LoadCustomers();
                    LoadProducts();
                });
            });

            AddOrUpdateCommand = new RelayCommand<object>(_ => AddOrUpdateOrder(), _ => CanAddOrUpdateOrder());
            ClearCommand = new RelayCommand<object>(_ => ClearForm(), _ => true);
            SearchByDateCommand = new RelayCommand<object>(_ => SearchOrdersByDate(), _ => true);
            SearchByCustomerCommand = new RelayCommand<object>(_ => SearchOrdersByCustomer(), _ => true);
            AddOrderItemCommand = new RelayCommand<object>(_ => AddOrderItem(), _ => CanAddOrderItem());
            RemoveOrderItemCommand = new RelayCommand<OrderDetail>(item => RemoveOrderItem(item), _ => true);
            GenerateInvoiceCommand = new RelayCommand<object>(_ => GenerateInvoice(), _ => SelectedOrder?.OrderID > 0);
            CreateFinanceEntryCommand = new RelayCommand<object>(_ => CreateFinanceEntry(), _ => SelectedOrder?.OrderID > 0);
            CreateExpenseEntryCommand = new RelayCommand<object>(_ => CreateExpenseEntry(), _ => SelectedOrder?.OrderID > 0);
            ScanBarcodeCommand = new RelayCommand<object>(_ => ScanBarcode(), _ => true);
            ProcessBarcodeOrderCommand = new RelayCommand<object>(_ => ProcessBarcodeOrder(), _ => CanProcessBarcodeOrder());
        }

        public ObservableCollection<Order> Orders { get; set; } = new ObservableCollection<Order>();
        public ObservableCollection<Customer> Customers { get; set; } = new ObservableCollection<Customer>();
        public ObservableCollection<Product> Products { get; set; } = new ObservableCollection<Product>();
        public ObservableCollection<OrderDetail> OrderItems { get; set; } = new ObservableCollection<OrderDetail>();
        public ObservableCollection<string> ScannedBarcodes { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> PaymentTypes { get; set; } = new ObservableCollection<string>
        {
            "Cash",
            "Card",
            "UPI",
            "Bank Transfer",
            "Credit",
            "EMI",
            "Mixed"
        };

        // Barcode scanning properties
        private string _currentBarcode;
        public string CurrentBarcode
        {
            get => _currentBarcode;
            set
            {
                _currentBarcode = value;
                OnPropertyChanged();
            }
        }

        private bool _isBarcodeMode = false;
        public bool IsBarcodeMode
        {
            get => _isBarcodeMode;
            set
            {
                _isBarcodeMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsManualMode));
            }
        }

        public bool IsManualMode => !IsBarcodeMode;

        private Order _selectedOrder = new Order();
        public Order SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                _selectedOrder = value;
                OnPropertyChanged();
                if (value != null && value.OrderID > 0)
                {
                    LoadOrderDetails(value.OrderID);
                }
            }
        }

        private OrderDetail _selectedOrderItem;
        public OrderDetail SelectedOrderItem
        {
            get => _selectedOrderItem;
            set
            {
                _selectedOrderItem = value;
                OnPropertyChanged();
            }
        }

        private Customer _selectedCustomer;
        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                _selectedCustomer = value;
                if (value != null)
                {
                    SelectedOrder.CustomerID = value.CustomerID;
                    SelectedOrder.Customer = value;
                }
                OnPropertyChanged();
            }
        }        private Product _selectedProduct;
        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                OnPropertyChanged();
                if (value != null && value.ProductID > 0)
                {
                    // Only create new OrderDetail when we have a valid product
                    // This prevents empty records from appearing in the UI
                    SelectedOrderItem = new OrderDetail
                    {
                        ProductID = value.ProductID,
                        Product = value,
                        UnitPrice = value.ProductPrice,
                        Quantity = 1,
                        TotalAmount = value.ProductPrice
                    };
                    CalculateOrderItemTotal();
                }
            }
        }private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(-1));
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
        
        // UI helper properties for DateTime pickers
        [NotMapped]
        public DateTime StartDateUI
        {
            get => StartDate.ToDateTime(TimeOnly.MinValue);
            set => StartDate = DateOnly.FromDateTime(value);
        }
        
        [NotMapped]
        public DateTime EndDateUI
        {
            get => EndDate.ToDateTime(TimeOnly.MinValue);
            set => EndDate = DateOnly.FromDateTime(value);
        }

        private int _customerId;
        public int CustomerId
        {
            get => _customerId;
            set
            {
                _customerId = value;
                OnPropertyChanged();
            }
        }

        private async void LoadOrders()
        {
            Orders.Clear();
            var orders = await _orderService.GetAllOrders();
            foreach (var order in orders)
            {
                Orders.Add(order);
            }
        }        private async void LoadCustomers()
        {
            try
            {
                Customers.Clear();
                var customers = await _customerService.GetAllCustomers();
                if (customers != null)
                {
                    foreach (var customer in customers)
                    {
                        Customers.Add(customer);
                    }
                    System.Diagnostics.Debug.WriteLine($"Loaded {Customers.Count} customers");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Customer loading returned null");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading customers: {ex.Message}");
                System.Windows.MessageBox.Show($"Failed to load customers: {ex.Message}");
            }
        }

        private async void LoadProducts()
        {
            try
            {
                Products.Clear();
                var products = await _productService.GetAllProducts();
                if (products != null)
                {
                    foreach (var product in products)
                    {
                        Products.Add(product);
                    }
                    System.Diagnostics.Debug.WriteLine($"Loaded {Products.Count} products");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Product loading returned null");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading products: {ex.Message}");
                System.Windows.MessageBox.Show($"Failed to load products: {ex.Message}");
            }
        }

        private async void LoadOrderDetails(int orderId)
        {
            OrderItems.Clear();
            var orderDetails = await _orderService.GetOrderDetails(orderId);
            foreach (var item in orderDetails)
            {
                OrderItems.Add(item);
            }
        }        private void CalculateOrderItemTotal()
        {
            if (SelectedProduct != null && SelectedProduct.ProductID > 0 && SelectedOrderItem != null)
            {
                SelectedOrderItem.Quantity = 1;  // Default quantity
                SelectedOrderItem.UnitPrice = SelectedProduct.ProductPrice;
                SelectedOrderItem.TotalAmount = Math.Round(SelectedOrderItem.UnitPrice * SelectedOrderItem.Quantity, 2);
                OnPropertyChanged(nameof(SelectedOrderItem));
            }
        }private void UpdateOrderTotal()
        {
            try
            {
                // Ensure SelectedOrder is not null
                if (SelectedOrder == null)
                {
                    SelectedOrder = new Order
                    {
                        OrderDate = DateOnly.FromDateTime(DateTime.Now),
                        Status = "Pending",
                        PaymentType = "Cash"
                    };
                }
                
                // 1. Calculate base amount (sum of all product prices)
                decimal totalAmount = OrderItems?.Sum(item => item.TotalAmount) ?? 0;
                SelectedOrder.TotalAmount = totalAmount;
                
                // Update total items count
                SelectedOrder.TotalItems = OrderItems?.Sum(item => (int)item.Quantity) ?? 0;
                
                // 2. Calculate price before tax (after discount)
                SelectedOrder.PriceBeforeTax = totalAmount - SelectedOrder.DiscountAmount;
                if (SelectedOrder.PriceBeforeTax < 0) SelectedOrder.PriceBeforeTax = 0; // Ensure it doesn't go negative
                
                // 3. Calculate final price with 3% tax
                SelectedOrder.GrandTotal = Math.Round(SelectedOrder.PriceBeforeTax * 1.03m, 2);
                
                OnPropertyChanged(nameof(SelectedOrder));
                OnPropertyChanged(nameof(TaxValue));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateOrderTotal: {ex.Message}");
            }
        }        private void AddOrderItem()
        {
            try
            {
                if (SelectedProduct == null)
                {
                    System.Windows.MessageBox.Show("Please select a product first.", "No Product Selected");
                    return;
                }

                // Validate product data
                if (SelectedProduct.ProductID <= 0)
                {
                    System.Windows.MessageBox.Show("Invalid product selected. Please select a valid product.", "Invalid Product");
                    return;
                }

                if (string.IsNullOrEmpty(SelectedProduct.ProductName))
                {
                    System.Windows.MessageBox.Show("Product name is required. Please select a valid product.", "Invalid Product");
                    return;
                }

                if (SelectedProduct.ProductPrice <= 0)
                {
                    System.Windows.MessageBox.Show("Product price must be greater than zero.", "Invalid Price");
                    return;
                }

                // Check if product is already in the order
                var existingItem = OrderItems.FirstOrDefault(item => item.ProductID == SelectedProduct.ProductID);
                if (existingItem != null)
                {
                    // If product already exists, increase quantity
                    existingItem.Quantity += 1;
                    existingItem.TotalAmount = existingItem.UnitPrice * existingItem.Quantity;
                }
                else
                {
                    // Create new order item
                    var newItem = new OrderDetail
                    {
                        ProductID = SelectedProduct.ProductID,
                        Product = SelectedProduct,
                        Quantity = 1,
                        UnitPrice = SelectedProduct.ProductPrice,
                        TotalAmount = SelectedProduct.ProductPrice
                    };
                    OrderItems.Add(newItem);
                }

                UpdateOrderTotal();
                // Clear selected product and order item after adding to prevent duplicates on UI interaction
                SelectedProduct = null;
                SelectedOrderItem = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AddOrderItem: {ex.Message}");
                System.Windows.MessageBox.Show($"Failed to add product to order: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void RemoveOrderItem(OrderDetail item)
        {
            if (item != null)
            {
                OrderItems.Remove(item);
                UpdateOrderTotal();
            }
        }

        private async void AddOrUpdateOrder()
        {
            if (SelectedCustomer == null)
            {
                System.Windows.MessageBox.Show("Please select a customer");
                return;
            }

            // If in barcode mode, use barcode workflow
            if (IsBarcodeMode)
            {
                ProcessBarcodeOrder();
                return;
            }

            // Manual mode - existing logic
            if (!OrderItems.Any())
            {
                System.Windows.MessageBox.Show("Please add at least one item to the order");
                return;
            }

            // Set order details
            SelectedOrder.OrderDate = DateOnly.FromDateTime(DateTime.Now);
            SelectedOrder.CustomerID = SelectedCustomer.CustomerID;

            // Prepare order details list
            List<OrderDetail> orderDetails = OrderItems.ToList();

            try
            {
                if (SelectedOrder.OrderID > 0)
                {
                    // Update existing order (keep existing logic for now)
                    bool updateResult = await _orderService.UpdateOrder(SelectedOrder);
                    bool detailsResult = await _orderService.UpdateOrderDetails(SelectedOrder.OrderID, orderDetails);
                    
                    if (!updateResult || !detailsResult)
                    {
                        System.Windows.MessageBox.Show("Failed to update order");
                        return;
                    }
                }
                else
                {
                    // ENHANCED: Use the fixed OrderService.CreateOrder for new orders
                    var newOrder = await _orderService.CreateOrder(SelectedOrder, orderDetails);
                    if (newOrder != null)
                    {
                        SelectedOrder = newOrder;
                        System.Windows.MessageBox.Show("Order created successfully with proper stock tracking!", "Success");
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Failed to create order or insufficient stock", "Error");
                        return;
                    }
                }

                LoadOrders();
                ClearForm();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving order: {ex.Message}");
            }
        }        private void CreateFinanceEntry()
        {
            if (SelectedOrder == null || SelectedOrder.OrderID <= 0)
            {
                System.Windows.MessageBox.Show("Please select an order first", "Selection Required", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Check if finance entry already exists for this order
                var existingFinanceRecords = _financeService.GetAllFinanceRecords()
                    .Where(f => f.OrderID == SelectedOrder.OrderID && f.TransactionType == "Income")
                    .ToList();

                if (existingFinanceRecords.Any())
                {
                    var result = System.Windows.MessageBox.Show(
                        $"Finance entry already exists for Order #{SelectedOrder.OrderID}. Do you want to create another one?",
                        "Duplicate Entry", 
                        System.Windows.MessageBoxButton.YesNo, 
                        System.Windows.MessageBoxImage.Question);
                    
                    if (result == System.Windows.MessageBoxResult.No)
                    {
                        return;
                    }
                }

                // Create finance entry for the order with proper fields
                var finance = new Finance
                {
                    FinanceID = Guid.NewGuid().ToString(),
                    TransactionDate = SelectedOrder.OrderDate.ToDateTime(TimeOnly.MinValue),
                    Amount = SelectedOrder.GrandTotal,
                    TransactionType = "Income",
                    PaymentMethod = SelectedOrder.PaymentType ?? "Cash",
                    PaymentMode = SelectedOrder.PaymentType ?? "Cash",
                    Category = "Sales",
                    Description = $"Payment for Order #{SelectedOrder.OrderID} - ₹{SelectedOrder.GrandTotal:N2}",
                    Notes = $"Finance entry for order placed on {SelectedOrder.OrderDate}",
                    CustomerID = SelectedOrder.CustomerID,
                    OrderID = SelectedOrder.OrderID,
                    ReferenceNumber = SelectedOrder.OrderID.ToString(),
                    CreatedBy = Environment.UserName,
                    Currency = "INR",
                    IsPaymentReceived = true,
                    Status = "Completed"
                };

                // Call AddFinanceRecord and handle result properly
                bool success = _financeService.AddFinanceRecord(finance);
                if (success)
                {
                    System.Windows.MessageBox.Show($"Finance entry created successfully!\nAmount: ₹{SelectedOrder.GrandTotal:N2}\nPayment Method: {SelectedOrder.PaymentType}", 
                        "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show("Failed to create finance entry. Please check the logs for details.", 
                        "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error creating finance entry: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }        private void CreateExpenseEntry()
        {
            if (SelectedOrder == null || SelectedOrder.OrderID <= 0)
            {
                System.Windows.MessageBox.Show("Please select an order first", "Selection Required", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Create expense entry for the order (for purchase orders or operational expenses)
                var expense = new Finance
                {
                    FinanceID = Guid.NewGuid().ToString(),
                    TransactionDate = SelectedOrder.OrderDate.ToDateTime(TimeOnly.MinValue),
                    Amount = SelectedOrder.GrandTotal,
                    TransactionType = "Expense",
                    PaymentMethod = SelectedOrder.PaymentType ?? "Cash",
                    PaymentMode = SelectedOrder.PaymentType ?? "Cash",
                    Category = "Purchase",
                    Description = $"Expense for Order #{SelectedOrder.OrderID} - ₹{SelectedOrder.GrandTotal:N2}",
                    Notes = $"Expense entry for purchase order placed on {SelectedOrder.OrderDate}",
                    CustomerID = SelectedOrder.CustomerID,
                    OrderID = SelectedOrder.OrderID,
                    ReferenceNumber = SelectedOrder.OrderID.ToString(),
                    CreatedBy = Environment.UserName,
                    Currency = "INR",
                    IsPaymentReceived = false, // This is an expense (money going out)
                    Status = "Completed"
                };

                // Call AddFinanceRecord and handle result properly
                bool success = _financeService.AddFinanceRecord(expense);
                if (success)
                {
                    System.Windows.MessageBox.Show($"Expense entry created successfully!\nAmount: ₹{SelectedOrder.GrandTotal:N2}\nPayment Method: {SelectedOrder.PaymentType}", 
                        "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show("Failed to create expense entry. Please check the logs for details.", 
                        "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error creating expense entry: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void GenerateInvoice()
        {
            try
            {
                if (SelectedOrder == null || SelectedOrder.OrderID <= 0)
                {
                    System.Windows.MessageBox.Show("Please select an order first");
                    return;
                }
            
                // Get the order details
                List<OrderDetail> orderDetails = new List<OrderDetail>();
                if (OrderItems.Count > 0)
                {
                    // Use current items if we're viewing the selected order details
                    orderDetails = OrderItems.ToList();
                }
                else
                {                    // Load the order details from the database
                    orderDetails = _orderService.GetOrderDetails(SelectedOrder.OrderID).Result.ToList();
                }
                
                // Business Information - you might want to load this from settings or database
                string businessName = "BOOLA Jewelry";
                string businessAddress = "123 Jewelry Street, Luxury Lane, Golden City";
                
                try
                {
                    // Generate the invoice PDF
                    string pdfPath = Utilities.InvoiceGenerator.GenerateInvoice(
                        SelectedOrder,
                        orderDetails,
                        businessName,
                        businessAddress
                    );
                      // Open the PDF file with the default PDF viewer
                    var psi = new ProcessStartInfo
                    {
                        FileName = pdfPath,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                    
                    System.Windows.MessageBox.Show($"Invoice PDF has been generated and saved to:\n{pdfPath}", "Success", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }                catch (QuestPDF.Drawing.Exceptions.DocumentDrawingException fontEx)
                {
                    // Special handling for font issues
                    System.Diagnostics.Debug.WriteLine($"Font error in invoice generation: {fontEx.Message}");
                    System.Windows.MessageBox.Show(
                        "There was an issue with fonts while generating the invoice. " +
                        "Using INR currency notation instead of the Rupee symbol. " +
                        "Please check the output PDF.",
                        "Font Warning",
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to generate invoice: {ex.Message}", "Error", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error generating invoice: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ClearForm()
        {
            SelectedOrder = new Order
            {
                OrderDate = DateOnly.FromDateTime(DateTime.Now),
                PaymentType = PaymentTypes.First(),
                Status = "Pending",
                DiscountAmount = 0
            };
            SelectedCustomer = null;
            SelectedProduct = null;
            SelectedOrderItem = null;
            OrderItems.Clear();
            
            // Clear barcode data as well
            ClearBarcodeData();
            
            StartDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(-1));
            EndDate = DateOnly.FromDateTime(DateTime.Now);
            CustomerId = 0;
        }

        private bool CanAddOrUpdateOrder()
        {
            return SelectedCustomer != null && 
                   OrderItems.Count > 0 &&
                   !string.IsNullOrEmpty(SelectedOrder.PaymentType);
        }

        private bool CanAddOrderItem()
        {
            return SelectedProduct != null && 
                   SelectedProduct.ProductID > 0 &&
                   !string.IsNullOrEmpty(SelectedProduct.ProductName) &&
                   SelectedProduct.ProductPrice > 0;
        }

        private async void SearchOrdersByDate()
        {
            Orders.Clear();
            var orders = await _orderService.FilterOrdersByDate(StartDate, EndDate);
            foreach (var order in orders)
            {
                Orders.Add(order);
            }
        }

        private async void SearchOrdersByCustomer()
        {
            Orders.Clear();
            var orders = await _orderService.FilterOrdersByCustomer(CustomerId);
            foreach (var order in orders)
            {
                Orders.Add(order);
            }
        }
        
        // Method to manually reload customers and products data
        public void ReloadData()
        {
            LoadOrders();
            LoadCustomers();
            LoadProducts();
            
            // Reset the selected order to a new one
            SelectedOrder = new Order
            {
                OrderDate = DateOnly.FromDateTime(DateTime.Now),
                Status = "Pending",
                PaymentMethod = "Cash"
            };
            
            OrderItems.Clear();
            System.Diagnostics.Debug.WriteLine("Data reloaded");
        }

        // Add a property to handle discount amount changes
        public decimal DiscountAmount
        {
            get => SelectedOrder?.DiscountAmount ?? 0;
            set
            {
                if (SelectedOrder != null)
                {
                    SelectedOrder.DiscountAmount = value;
                    UpdateOrderTotal(); // Recalculate with new discount
                    OnPropertyChanged();
                }
            }
        }

        // Property for displaying tax amount in UI
        public decimal TaxValue
        {
            get
            {
                if (SelectedOrder == null) return 0;
                return Math.Round(SelectedOrder.GrandTotal - SelectedOrder.PriceBeforeTax, 2);
            }
        }

        private bool _createFinanceEntryOnSave = true;
        public bool CreateFinanceEntryOnSave
        {
            get => _createFinanceEntryOnSave;
            set
            {
                _createFinanceEntryOnSave = value;
                OnPropertyChanged();
            }
        }

        #region Barcode Scanning Methods

        /// <summary>
        /// Simulates or handles actual barcode scanning
        /// </summary>
        private async void ScanBarcode()
        {
            try
            {
                // For now, simulate barcode scanning - in production, this would interface with hardware
                if (!string.IsNullOrWhiteSpace(CurrentBarcode))
                {
                    var isValid = await _barcodeService.ValidateBarcodeAsync(CurrentBarcode);
                    if (isValid)
                    {
                        // Check if barcode is for a stock item
                        if (CurrentBarcode.StartsWith("ITM-"))
                        {
                            var stockItem = await _barcodeService.FindStockItemByBarcodeAsync(CurrentBarcode);
                            if (stockItem != null && stockItem.Status == "Available")
                            {
                                ScannedBarcodes.Add(CurrentBarcode);
                                System.Windows.MessageBox.Show($"Scanned: {stockItem.Product?.ProductName}\nBarcode: {CurrentBarcode}", "Barcode Scanned");
                                CurrentBarcode = ""; // Clear for next scan
                            }
                            else
                            {
                                System.Windows.MessageBox.Show("Item not available or already sold", "Invalid Item");
                            }
                        }
                        else
                        {
                            System.Windows.MessageBox.Show("Invalid barcode format", "Invalid Barcode");
                        }
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Barcode not found in system", "Invalid Barcode");
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("Please enter a barcode to scan", "No Barcode");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error scanning barcode: {ex.Message}", "Scan Error");
            }
        }

        /// <summary>
        /// Process an order using scanned barcodes via Enhanced Workflow Service
        /// </summary>
        private async void ProcessBarcodeOrder()
        {
            if (SelectedCustomer == null)
            {
                System.Windows.MessageBox.Show("Please select a customer first", "Customer Required");
                return;
            }

            if (!ScannedBarcodes.Any())
            {
                System.Windows.MessageBox.Show("Please scan at least one item", "No Items Scanned");
                return;
            }

            try
            {
                // Use Enhanced Workflow Service for proper sales processing
                var result = await _enhancedWorkflowService.ProcessSaleWithBarcodeAsync(
                    SelectedCustomer.CustomerID,
                    ScannedBarcodes.ToList(),
                    SelectedOrder.PaymentMethod ?? "Cash",
                    SelectedOrder.DiscountAmount,
                    SelectedOrder.Notes ?? ""
                );

                if (result.Success)
                {
                    System.Windows.MessageBox.Show($"Order created successfully!\nOrder ID: {result.Order.OrderID}", "Success");
                    
                    // Update the UI
                    SelectedOrder = result.Order;
                    LoadOrders();
                    ClearBarcodeData();
                }
                else
                {
                    System.Windows.MessageBox.Show($"Failed to create order: {result.Message}", "Error");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error processing barcode order: {ex.Message}", "Processing Error");
            }
        }

        /// <summary>
        /// Check if barcode order can be processed
        /// </summary>
        private bool CanProcessBarcodeOrder()
        {
            return SelectedCustomer != null && ScannedBarcodes.Any();
        }

        /// <summary>
        /// Clear barcode scanning data
        /// </summary>
        private void ClearBarcodeData()
        {
            ScannedBarcodes.Clear();
            CurrentBarcode = "";
        }

        #endregion
    }
}
