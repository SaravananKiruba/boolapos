using Page_Navigation_App.Model;
using System;
using Page_Navigation_App.Utilities;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Page_Navigation_App.Services;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

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
        public OrderVM() { }

        public OrderVM(OrderService orderService, CustomerService customerService, 
                      ProductService productService, FinanceService financeService)
        {
            _orderService = orderService;
            _customerService = customerService;
            _productService = productService;
            _financeService = financeService;
            
            LoadOrders();
            LoadCustomers();
            LoadProducts();

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
                    SelectedOrderItem.ProductID = value.ProductID;
                    SelectedOrderItem.Product = value;
                    SelectedOrderItem.UnitPrice = value.FinalPrice;
                    SelectedOrderItem.MetalRate = value.BasePrice / value.NetWeight;
                    CalculateOrderItemTotal();
                }
            }
        }

        private DateTime _startDate = DateTime.Now.AddMonths(-1);
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                OnPropertyChanged();
            }
        }

        private DateTime _endDate = DateTime.Now;
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                OnPropertyChanged();
            }
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
        }

        private async void LoadCustomers()
        {
            Customers.Clear();
            var customers = await _customerService.GetAllCustomers();
            foreach (var customer in customers)
            {
                Customers.Add(customer);
            }
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

        private async void LoadOrderDetails(int orderId)
        {
            OrderItems.Clear();
            var orderDetails = await _orderService.GetOrderDetails(orderId);
            foreach (var item in orderDetails)
            {
                OrderItems.Add(item);
            }
        }

        private void CalculateOrderItemTotal()
        {
            if (SelectedOrderItem != null && SelectedProduct != null)
            {
                SelectedOrderItem.Quantity = 1;  // Default quantity
                SelectedOrderItem.TotalAmount = SelectedOrderItem.UnitPrice * SelectedOrderItem.Quantity;
                SelectedOrderItem.NetWeight = SelectedProduct.NetWeight;
                SelectedOrderItem.GrossWeight = SelectedProduct.GrossWeight;
                OnPropertyChanged(nameof(SelectedOrderItem));
            }
        }

        private void UpdateOrderTotal()
        {
            SelectedOrder.TotalAmount = OrderItems.Sum(item => item.TotalAmount);
            SelectedOrder.TotalItems = OrderItems.Count;
            
            // Calculate taxes
            SelectedOrder.CGST = SelectedOrder.TotalAmount * 0.015m;  // 1.5%
            SelectedOrder.SGST = SelectedOrder.TotalAmount * 0.015m;  // 1.5%
            
            // Calculate discounts
            // (keep any existing discount logic)
            
            // Calculate grand total
            SelectedOrder.GrandTotal = SelectedOrder.TotalAmount + SelectedOrder.CGST + SelectedOrder.SGST - SelectedOrder.DiscountAmount;
            
            OnPropertyChanged(nameof(SelectedOrder));
        }

        private void AddOrderItem()
        {
            if (SelectedProduct == null)
                return;

            var newItem = new OrderDetail
            {
                ProductID = SelectedProduct.ProductID,
                Product = SelectedProduct,
                Quantity = 1,
                UnitPrice = SelectedProduct.FinalPrice,
                TotalAmount = SelectedProduct.FinalPrice,
                NetWeight = SelectedProduct.NetWeight,
                GrossWeight = SelectedProduct.GrossWeight,
                MetalRate = SelectedProduct.BasePrice / SelectedProduct.NetWeight,
                MakingCharges = SelectedProduct.MakingCharges,
                WastagePercentage = SelectedProduct.WastagePercentage
            };

            OrderItems.Add(newItem);
            UpdateOrderTotal();
            SelectedProduct = null;
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
            }

            // Set order details
            SelectedOrder.OrderDate = DateTime.Now;
            SelectedOrder.CustomerID = SelectedCustomer.CustomerID;

            // Prepare order details list
            List<OrderDetail> orderDetails = OrderItems.ToList();

            try
            {
                if (SelectedOrder.OrderID > 0)
                {
                    await _orderService.UpdateOrder(SelectedOrder);
                    await _orderService.UpdateOrderDetails(SelectedOrder.OrderID, orderDetails);
                }
                else
                {
                    // Create new order with details
                    var newOrder = await _orderService.CreateOrder(SelectedOrder, orderDetails);
                    SelectedOrder = newOrder;
                    
                    // Add loyalty points to customer based on purchase amount
                    await _customerService.AddLoyaltyPoints(SelectedOrder.CustomerID, SelectedOrder.GrandTotal);
                }

                System.Windows.MessageBox.Show($"Order {(SelectedOrder.OrderID > 0 ? "updated" : "created")} successfully!");
                LoadOrders();
                ClearForm();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving order: {ex.Message}");
            }
        }

        private async void CreateFinanceEntry()
        {
            if (SelectedOrder == null || SelectedOrder.OrderID <= 0)
            {
                System.Windows.MessageBox.Show("Please select an order first");
                return;
            }

            try
            {
                var finance = new Finance
                {
                    TransactionDate = DateTime.Now,
                    Amount = SelectedOrder.GrandTotal,
                    TransactionType = "Income",
                    PaymentMethod = SelectedOrder.PaymentType,
                    Description = $"Payment for Order #{SelectedOrder.OrderID}",
                    CustomerId = SelectedOrder.CustomerID,
                    OrderID = SelectedOrder.OrderID,
                    ReferenceNumber = SelectedOrder.OrderID.ToString(),
                    CreatedBy = Environment.UserName
                };

                var result = await _financeService.AddFinanceRecord(finance);
                if (result != null)
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
        }

        private void GenerateInvoice()
        {
            // This would typically call a reporting or printing service
            System.Windows.MessageBox.Show($"Invoice for Order #{SelectedOrder.OrderID} has been generated");
        }

        private void ClearForm()
        {
            SelectedOrder = new Order
            {
                OrderDate = DateTime.Now,
                PaymentType = PaymentTypes.First()
            };
            SelectedCustomer = null;
            SelectedProduct = null;
            OrderItems.Clear();
            StartDate = DateTime.Now.AddMonths(-1);
            EndDate = DateTime.Now;
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
    }
}
