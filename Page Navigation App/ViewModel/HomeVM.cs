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
        private NavigationVM _navigationVM;

        // Property to set the NavigationVM reference after initialization
        public NavigationVM NavigationService
        {
            set { _navigationVM = value; }
        }

        public HomeVM(ProductService productService, OrderService orderService, CustomerService customerService)
        {
            _productService = productService;
            _orderService = orderService;
            _customerService = customerService;

            LoadDashboardData();
            ExportReportCommand = new RelayCommand<object>(_ => ExportReport(), _ => true);
            
            // Initialize view details commands
            ViewCustomersCommand = new RelayCommand<object>(_ => NavigateToCustomers(), _ => _navigationVM != null);
            ViewProductsCommand = new RelayCommand<object>(_ => NavigateToProducts(), _ => _navigationVM != null);
            ViewOrdersCommand = new RelayCommand<object>(_ => NavigateToOrders(), _ => _navigationVM != null);
            RefreshCommand = new RelayCommand<object>(_ => LoadDashboardData(), _ => true);
        }

        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public decimal TotalRevenue { get; set; }

        public DateTime StartDate { get; set; } = DateTime.Now.AddMonths(-1);
        public DateTime EndDate { get; set; } = DateTime.Now;

        public int ProductInCount { get; set; }
        public int ProductOutCount { get; set; }

        public ICommand ExportReportCommand { get; }
        public ICommand ViewCustomersCommand { get; }
        public ICommand ViewProductsCommand { get; }
        public ICommand ViewOrdersCommand { get; }
        public ICommand RefreshCommand { get; }

        private async void LoadDashboardData()
        {
            var products = await _productService.GetAllProducts();
            var orders = await _orderService.GetAllOrders();
            var customers = await _customerService.GetAllCustomers();

            TotalProducts = products.Count();
            TotalOrders = orders.Count();
            TotalCustomers = customers.Count();
            TotalRevenue = orders.Sum(o => o.TotalAmount);

            // Calculate product movement in the last month
            var lastMonth = DateTime.Now.AddMonths(-1);
            ProductInCount = products.Count(p => p.DateAdded >= lastMonth);
            ProductOutCount = products.Count(p => p.DateRemoved >= lastMonth);

            OnPropertyChanged(nameof(TotalProducts));
            OnPropertyChanged(nameof(TotalOrders));
            OnPropertyChanged(nameof(TotalCustomers));
            OnPropertyChanged(nameof(TotalRevenue));
            OnPropertyChanged(nameof(ProductInCount));
            OnPropertyChanged(nameof(ProductOutCount));
        }

        private void ExportReport()
        {
            // Placeholder for exporting report logic
            System.Windows.MessageBox.Show("Report exported successfully!", "Export", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void NavigateToCustomers()
        {
            _navigationVM.NavigateToCustomers();
        }

        private void NavigateToProducts()
        {
            _navigationVM.NavigateToProducts();
        }

        private void NavigateToOrders()
        {
            _navigationVM.NavigateToOrders();
        }
    }
}
