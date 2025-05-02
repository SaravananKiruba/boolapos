using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Page_Navigation_App.Model;
using Page_Navigation_App.Services;
using Page_Navigation_App.Utilities;
using System.Threading.Tasks;

namespace Page_Navigation_App.ViewModel
{
    public class ReportVM : ViewModelBase
    {
        private readonly CustomerService _customerService;
        private readonly OrderService _orderService;
        private readonly ProductService _productService;
        private readonly FinanceService _financeService;
        private readonly CategoryService _categoryService;
        
        public ICommand GenerateReportCommand { get; }
        public ICommand ExportReportCommand { get; }

        public ReportVM(
            CustomerService customerService,
            OrderService orderService,
            ProductService productService,
            FinanceService financeService,
            CategoryService categoryService)
        {
            _customerService = customerService;
            _orderService = orderService;
            _productService = productService;
            _financeService = financeService;
            _categoryService = categoryService;
            
            // Initialize collections
            ReportTypes = new ObservableCollection<string>
            {
                "Sales Report",
                "Inventory Report",
                "Customer Report",
                "Financial Report",
                "Category Report"
            };
            
            GenerateReportCommand = new RelayCommand<object>(_ => GenerateReport(), _ => CanGenerateReport());
            ExportReportCommand = new RelayCommand<object>(_ => ExportReport(), _ => ReportData != null && ReportData.Any());
            
            // Initialize defaults
            SelectedReportType = ReportTypes.FirstOrDefault();
            StartDate = DateTime.Now.AddMonths(-1);
            EndDate = DateTime.Now;
        }
        
        public ObservableCollection<string> ReportTypes { get; set; }
        
        private string _selectedReportType;
        public string SelectedReportType
        {
            get => _selectedReportType;
            set
            {
                _selectedReportType = value;
                OnPropertyChanged();
                ClearReportData();
            }
        }
        
        private DateTime _startDate;
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
        
        private bool _isReportGenerated;
        public bool IsReportGenerated
        {
            get => _isReportGenerated;
            set
            {
                _isReportGenerated = value;
                OnPropertyChanged();
            }
        }
        
        private string _reportTitle;
        public string ReportTitle
        {
            get => _reportTitle;
            set
            {
                _reportTitle = value;
                OnPropertyChanged();
            }
        }
        
        private Dictionary<string, object> _reportSummary;
        public Dictionary<string, object> ReportSummary
        {
            get => _reportSummary;
            set
            {
                _reportSummary = value;
                OnPropertyChanged();
            }
        }
        
        private IEnumerable<object> _reportData;
        public IEnumerable<object> ReportData
        {
            get => _reportData;
            set
            {
                _reportData = value;
                OnPropertyChanged();
            }
        }
        
        private void ClearReportData()
        {
            ReportData = null;
            ReportSummary = null;
            IsReportGenerated = false;
        }
        
        private bool CanGenerateReport()
        {
            return !string.IsNullOrEmpty(SelectedReportType) && 
                   StartDate <= EndDate;
        }
        
        private async void GenerateReport()
        {
            try
            {
                IsReportGenerated = false;
                
                switch (SelectedReportType)
                {
                    case "Sales Report":
                        await GenerateSalesReport();
                        break;
                    case "Inventory Report":
                        await GenerateInventoryReport();
                        break;
                    case "Customer Report":
                        await GenerateCustomerReport();
                        break;
                    case "Financial Report":
                        await GenerateFinancialReport();
                        break;
                    case "Category Report":
                        await GenerateCategoryReport();
                        break;
                }
                
                IsReportGenerated = true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error generating report: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        
        private async Task GenerateSalesReport()
        {
            ReportTitle = $"Sales Report ({StartDate:d} - {EndDate:d})";
            
            // Get all orders in date range
            var orders = await _orderService.GetOrdersByDate(StartDate, EndDate);
            ReportData = orders.ToList();
            
            // Calculate summary data
            decimal totalSales = orders.Sum(o => o.GrandTotal);
            int totalOrders = orders.Count();
            decimal averageOrderValue = totalOrders > 0 ? totalSales / totalOrders : 0;
            
            var paymentTypes = orders
                .GroupBy(o => o.PaymentType)
                .Select(g => new
                {
                    PaymentType = g.Key,
                    OrderCount = g.Count(),
                    TotalAmount = g.Sum(o => o.GrandTotal)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();
            
            var topCustomers = orders
                .GroupBy(o => o.CustomerID)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    CustomerName = g.First().Customer?.CustomerName ?? "Unknown",
                    OrderCount = g.Count(),
                    TotalAmount = g.Sum(o => o.GrandTotal)
                })
                .OrderByDescending(x => x.TotalAmount)
                .Take(5)
                .ToList();
            
            ReportSummary = new Dictionary<string, object>
            {
                { "Total Sales", totalSales },
                { "Total Orders", totalOrders },
                { "Average Order Value", averageOrderValue },
                { "Payment Types", paymentTypes },
                { "Top Customers", topCustomers }
            };
        }
        
        private async Task GenerateInventoryReport()
        {
            ReportTitle = "Inventory Report";
            
            // Get all active products
            var products = await _productService.GetAllProducts();
            ReportData = products.ToList();
            
            // Calculate summary data
            int totalProducts = products.Count();
            decimal totalValue = products.Sum(p => p.FinalPrice * p.StockQuantity);
            
            var metalTypes = products
                .GroupBy(p => p.MetalType)
                .Select(g => new
                {
                    MetalType = g.Key,
                    Count = g.Count(),
                    TotalWeight = g.Sum(p => p.NetWeight * p.StockQuantity),
                    TotalValue = g.Sum(p => p.FinalPrice * p.StockQuantity)
                })
                .OrderByDescending(x => x.TotalValue)
                .ToList();
            
            var lowStockProducts = products
                .Where(p => p.StockQuantity <= p.ReorderLevel)
                .ToList();
            
            ReportSummary = new Dictionary<string, object>
            {
                { "Total Products", totalProducts },
                { "Total Inventory Value", totalValue },
                { "Metal Types", metalTypes },
                { "Low Stock Products", lowStockProducts.Count }
            };
        }
        
        private async Task GenerateCustomerReport()
        {
            ReportTitle = "Customer Report";
            
            // Get all customers
            var customers = await _customerService.GetAllCustomers();
            ReportData = customers.ToList();
            
            // Calculate summary data
            int totalCustomers = customers.Count();
            int activeCustomers = customers.Count(c => c.IsActive);
            
            var customerTypes = customers
                .GroupBy(c => c.CustomerType)
                .Select(g => new
                {
                    CustomerType = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToList();
            
            ReportSummary = new Dictionary<string, object>
            {
                { "Total Customers", totalCustomers },
                { "Active Customers", activeCustomers },
                { "Customer Types", customerTypes }
            };
        }
        
        private async Task GenerateFinancialReport()
        {
            ReportTitle = $"Financial Report ({StartDate:d} - {EndDate:d})";
            
            // Get all financial transactions in date range
            var transactions = await _financeService.GetTransactionsByDateRange(StartDate, EndDate);
            ReportData = transactions.ToList();
            
            // Calculate summary data
            var income = transactions
                .Where(t => t.TransactionType == "Income" || t.TransactionType == "Deposit")
                .Sum(t => t.Amount);
                
            var expense = transactions
                .Where(t => t.TransactionType == "Expense" || t.TransactionType == "Withdrawal" || t.TransactionType == "Refund")
                .Sum(t => t.Amount);
                
            var netProfit = income - expense;
            
            var transactionsByType = transactions
                .GroupBy(t => t.TransactionType)
                .Select(g => new
                {
                    Type = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(t => t.Amount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();
            
            var paymentMethods = transactions
                .Where(t => t.TransactionType == "Income")
                .GroupBy(t => t.PaymentMethod)
                .Select(g => new
                {
                    Method = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(t => t.Amount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();
            
            ReportSummary = new Dictionary<string, object>
            {
                { "Total Income", income },
                { "Total Expenses", expense },
                { "Net Profit", netProfit },
                { "Transactions by Type", transactionsByType },
                { "Payment Methods", paymentMethods }
            };
        }
        
        private async Task GenerateCategoryReport()
        {
            ReportTitle = "Category Report";
            
            // Get all categories
            var categories = await _categoryService.GetAllCategories();
            ReportData = categories.ToList();
            
            // Calculate summary data
            int totalCategories = categories.Count();
            int activeCategories = categories.Count(c => c.IsActive);
            
            var categoriesWithProducts = categories
                .Select(c => new
                {
                    CategoryName = c.CategoryName,
                    ProductCount = c.Products?.Count ?? 0,
                    MakingCharges = c.DefaultMakingCharges,
                    Wastage = c.DefaultWastage,
                    SubcategoryCount = c.Subcategories?.Count ?? 0
                })
                .OrderByDescending(x => x.ProductCount)
                .ToList();
            
            ReportSummary = new Dictionary<string, object>
            {
                { "Total Categories", totalCategories },
                { "Active Categories", activeCategories },
                { "Categories with Products", categoriesWithProducts }
            };
        }
        
        private void ExportReport()
        {
            // This would typically export the report to Excel or PDF
            System.Windows.MessageBox.Show(
                $"The {SelectedReportType} would be exported to Excel or PDF.", 
                "Export Report", 
                System.Windows.MessageBoxButton.OK, 
                System.Windows.MessageBoxImage.Information);
        }
    }
}