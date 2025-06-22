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
        private readonly OrderService _orderService;
        private readonly CustomerService _customerService;
        private readonly ProductService _productService;
        private readonly FinanceService _financeService;

        public ICommand AddOrUpdateCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand SearchByDateCommand { get; }
        public ICommand SearchByCustomerCommand { get; }
        public ICommand AddOrderItemCommand { get; }
        public ICommand RemoveOrderItemCommand { get; }
        public ICommand GenerateInvoiceCommand { get; }
        public ICommand CreateFinanceEntryCommand { get; }

        // Add a parameterless constructor to allow XAML instantiation
        public OrderVM() { }        public OrderVM(OrderService orderService, CustomerService customerService, 
                      ProductService productService, FinanceService financeService)
        {
            _orderService = orderService;
            _customerService = customerService;
            _productService = productService;
            _financeService = financeService;
            
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
        }

        public ObservableCollection<Order> Orders { get; set; } = new ObservableCollection<Order>();
        public ObservableCollection<Customer> Customers { get; set; } = new ObservableCollection<Customer>();
        public ObservableCollection<Product> Products { get; set; } = new ObservableCollection<Product>();
        public ObservableCollection<OrderDetail> OrderItems { get; set; } = new ObservableCollection<OrderDetail>();
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

        private OrderDetail _selectedOrderItem = new OrderDetail();
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
                if (value != null)
                {
                    if (SelectedOrderItem == null)
                        SelectedOrderItem = new OrderDetail();
                    SelectedOrderItem.ProductID = value.ProductID;
                    SelectedOrderItem.Product = value;
                    SelectedOrderItem.UnitPrice = value.ProductPrice;
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
            if (SelectedOrderItem == null)
                SelectedOrderItem = new OrderDetail();
            if (SelectedOrderItem != null && SelectedProduct != null)
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
                SelectedOrder.PriceBeforeTax = totalAmount + SelectedOrder.DiscountAmount;
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
        }private void AddOrderItem()
        {
            try
            {
                if (SelectedProduct == null)
                    return;                var newItem = new OrderDetail
                {
                    ProductID = SelectedProduct.ProductID,
                    Product = SelectedProduct,
                    Quantity = 1,
                    UnitPrice = SelectedProduct.ProductPrice,
                    TotalAmount = SelectedProduct.ProductPrice
                };

            OrderItems.Add(newItem);
            UpdateOrderTotal();
            SelectedProduct = null;
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

            if (!OrderItems.Any())
            {
                System.Windows.MessageBox.Show("Please add at least one item to the order");
                return;
            }            // Set order details
            SelectedOrder.OrderDate = DateOnly.FromDateTime(DateTime.Now);
            SelectedOrder.CustomerID = SelectedCustomer.CustomerID;

            // Prepare order details list
            List<OrderDetail> orderDetails = OrderItems.ToList();

            try
            {
                if (SelectedOrder.OrderID > 0)
                {
                    bool updateResult = await _orderService.UpdateOrder(SelectedOrder);
                    bool detailsResult = await _orderService.UpdateOrderDetails(SelectedOrder.OrderID, orderDetails);
                    
                    if (!updateResult || !detailsResult)
                    {
                        System.Windows.MessageBox.Show("Failed to update order");
                        return;
                    }
                }                else
                {
                    // Create new order with details and update stock
                    Finance payment = null;
                    
                    // If payment is "Paid", create a finance entry
                    if (SelectedOrder.Status == "Completed" || CreateFinanceEntryOnSave)
                    {
                        payment = new Finance
                        {
                            TransactionDate = DateTime.Now,
                            TransactionType = "Income",
                            Amount = SelectedOrder.GrandTotal,
                            PaymentMode = SelectedOrder.PaymentMethod,
                            Category = "Sales",
                            Description = $"Payment for order from {SelectedCustomer?.CustomerName}",
                            IsPaymentReceived = true,
                            CustomerID = SelectedOrder.CustomerID,
                            Status = "Completed",
                            CreatedBy = "System",
                            Currency = "INR"
                        };
                    }
                    
                    // Create new order with stock integration 
                    var newOrder = await _orderService.AddOrderWithStockUpdate(SelectedOrder, orderDetails, payment);
                    if (newOrder == null)
                    {
                        System.Windows.MessageBox.Show("Failed to create order or insufficient stock");
                        return;
                    }
                    
                    SelectedOrder = newOrder;
                }

                System.Windows.MessageBox.Show($"Order {(SelectedOrder.OrderID > 0 ? "updated" : "created")} successfully!");
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
                System.Windows.MessageBox.Show("Please select an order first");
                return;
            }

            try
            {
                // Create finance entry for the order (in INR)
                var finance = new Finance
                {
                    TransactionDate = DateTime.Now,
                    Amount = SelectedOrder.GrandTotal,
                    TransactionType = "Income",
                    PaymentMethod = SelectedOrder.PaymentType,
                    Description = $"Payment for Order #{SelectedOrder.OrderID} (INR)",
                    CustomerID = SelectedOrder.CustomerID,
                    OrderID = SelectedOrder.OrderID,
                    ReferenceNumber = SelectedOrder.OrderID.ToString(),
                    CreatedBy = Environment.UserName,
                    Currency = "INR"  // Explicitly specify currency as INR
                };

                // Call AddFinanceRecord and handle result properly
                bool result = _financeService.AddFinanceRecord(finance);
                if (result)
                {
                    System.Windows.MessageBox.Show("Finance entry created successfully!");
                }
                else
                {
                    System.Windows.MessageBox.Show("Failed to create finance entry");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error creating finance entry: {ex.Message}");
            }
        }        private void GenerateInvoice()
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
        {            SelectedOrder = new Order
            {
                OrderDate = DateOnly.FromDateTime(DateTime.Now),
                PaymentType = PaymentTypes.First()
            };
            SelectedCustomer = null;
            SelectedProduct = null;
            OrderItems.Clear();            StartDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(-1));
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
            return SelectedProduct != null;
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
    }
}
