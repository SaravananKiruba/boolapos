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

        public HomeVM(ProductService productService, OrderService orderService, CustomerService customerService)
        {
            _productService = productService;
            _orderService = orderService;
            _customerService = customerService;

            LoadDashboardData();
            ExportReportCommand = new RelayCommand<object>(_ => ExportReport(), _ => true);
        }

        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public decimal TotalRevenue { get; set; }

        public ICommand ExportReportCommand { get; }

        private void LoadDashboardData()
        {
            TotalProducts = _productService.GetAllProducts().Count;
            TotalOrders = _orderService.GetAllOrders().Count;
            TotalCustomers = _customerService.GetAllCustomers().Count;
            TotalRevenue = _orderService.GetAllOrders().Sum(o => o.TotalAmount);

            OnPropertyChanged(nameof(TotalProducts));
            OnPropertyChanged(nameof(TotalOrders));
            OnPropertyChanged(nameof(TotalCustomers));
            OnPropertyChanged(nameof(TotalRevenue));
        }

        private void ExportReport()
        {
            // Placeholder for exporting report logic
            System.Windows.MessageBox.Show("Report exported successfully!", "Export", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }
}
