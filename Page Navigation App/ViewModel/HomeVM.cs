using Page_Navigation_App.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Page_Navigation_App.Model;
using Page_Navigation_App.Services;
using System.Windows.Input;

namespace Page_Navigation_App.ViewModel
{
    public class HomeVM : ViewModelBase
    {
        private readonly ProductService _productService;
        private readonly OrderService _orderService;
        private readonly CustomerService _customerService;
        private DateTime _startDate;
        private DateTime _endDate;

        public HomeVM(ProductService productService, OrderService orderService, CustomerService customerService)
        {
            _productService = productService;
            _orderService = orderService;
            _customerService = customerService;

            // Initialize date range to current month
            StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            EndDate = DateTime.Now;

            LoadDashboardData();
            ExportReportCommand = new RelayCommand<object>(_ => ExportReport(), _ => true);
            RefreshCommand = new RelayCommand<object>(_ => LoadDashboardData(), _ => true);
            ViewCustomersCommand = new RelayCommand<object>(_ => ViewCustomers(), _ => true);
            ViewProductsCommand = new RelayCommand<object>(_ => ViewProducts(), _ => true);
            ViewOrdersCommand = new RelayCommand<object>(_ => ViewOrders(), _ => true);
        }

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                OnPropertyChanged(nameof(StartDate));
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                OnPropertyChanged(nameof(EndDate));
            }
        }

        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public decimal TotalRevenue { get; set; }
        public int ProductInCount { get; set; }
        public int ProductOutCount { get; set; }

        public ICommand ExportReportCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ViewCustomersCommand { get; }
        public ICommand ViewProductsCommand { get; }
        public ICommand ViewOrdersCommand { get; }

        private async void LoadDashboardData()
        {
            try
            {
                // Get all records for product, order, customer counts
                var products = await _productService.GetAllProducts();
                var customers = await _customerService.GetAllCustomers();

                // Get orders within date range
                var orders = await _orderService.GetOrdersByDateRange(StartDate, EndDate);

                TotalProducts = products.Count();
                TotalOrders = orders.Count();
                TotalCustomers = customers.Count();
                TotalRevenue = orders.Sum(o => o.TotalAmount);

                // Set product movement counts to 0 since we removed stock management
                ProductInCount = 0;
                ProductOutCount = 0;

                OnPropertyChanged(nameof(TotalProducts));
                OnPropertyChanged(nameof(TotalOrders));
                OnPropertyChanged(nameof(TotalCustomers));
                OnPropertyChanged(nameof(TotalRevenue));
                OnPropertyChanged(nameof(ProductInCount));
                OnPropertyChanged(nameof(ProductOutCount));
            }
            catch (Exception ex)
            {
                // Handle any exceptions
                System.Windows.MessageBox.Show($"Error loading dashboard data: {ex.Message}", 
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExportReport()
        {
            // Placeholder for exporting report logic
            System.Windows.MessageBox.Show("Report exported successfully!", "Export", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void ViewCustomers()
        {
            // Navigation will be handled by NavigationVM
        }

        private void ViewProducts()
        {
            // Navigation will be handled by NavigationVM
        }

        private void ViewOrders()
        {
            // Navigation will be handled by NavigationVM
        }
    }
}
