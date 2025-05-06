using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Page_Navigation_App.Model;
using Page_Navigation_App.Services;
using Page_Navigation_App.Utilities;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Page_Navigation_App.ViewModel
{
    public class ReportVM : ViewModelBase
    {
        private readonly CustomerService _customerService;
        private readonly OrderService _orderService;
        private readonly ProductService _productService;
        private readonly FinanceService _financeService;
        private readonly CategoryService _categoryService;
        private readonly ExportService _exportService;
        private readonly PrintService _printService;
        private readonly LogService _logService;
        
        public ICommand GenerateReportCommand { get; }
        public ICommand ExportReportCommand { get; }

        public ReportVM(
            CustomerService customerService,
            OrderService orderService,
            ProductService productService,
            FinanceService financeService,
            CategoryService categoryService,
            ExportService exportService,
            PrintService printService,
            LogService logService)
        {
            _customerService = customerService;
            _orderService = orderService;
            _productService = productService;
            _financeService = financeService;
            _categoryService = categoryService;
            _exportService = exportService;
            _printService = printService;
            _logService = logService;
            
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
                { "Low Stock Products", lowStockProducts.Count() }
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
        
        private Task GenerateFinancialReport()
        {
            ReportTitle = $"Financial Report ({StartDate:d} - {EndDate:d})";
            
            // Get all financial transactions in date range 
            var transactions = _financeService.GetTransactionsByDateRange(StartDate, EndDate);
            // Store the transactions directly without awaiting
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
            
            return Task.CompletedTask;
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
        
        private async void ExportReport()
        {
            try
            {
                // Create a SaveFileDialog to let user choose where to save the file
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Export Report",
                    Filter = "Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                    DefaultExt = ".xlsx",
                    FileName = $"{SelectedReportType.Replace(" ", "")}_{DateTime.Now:yyyyMMdd}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    string exportFormat = Path.GetExtension(filePath).TrimStart('.').ToLower();
                    
                    // Validate format
                    if (string.IsNullOrEmpty(exportFormat) || (exportFormat != "xlsx" && exportFormat != "csv"))
                    {
                        MessageBox.Show("Unsupported export format. Please choose either Excel (.xlsx) or CSV (.csv) format.",
                            "Invalid Format", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    // Show export progress using a non-blocking message
                    var progressMessage = new Window
                    {
                        Title = "Export In Progress",
                        Width = 300,
                        Height = 120,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        ResizeMode = ResizeMode.NoResize,
                        WindowStyle = WindowStyle.ToolWindow,
                        Content = new TextBlock
                        {
                            Text = $"Exporting {SelectedReportType}...\nPlease wait.",
                            TextAlignment = TextAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontSize = 14
                        }
                    };
                    
                    try
                    {
                        progressMessage.Show();
                        string result = string.Empty;
                        int retryCount = 0;
                        const int maxRetries = 3;
                        bool success = false;

                        // Retry logic for export operations
                        while (!success && retryCount < maxRetries)
                        {
                            try
                            {
                                switch (SelectedReportType)
                                {
                                    case "Sales Report":
                                        result = await _exportService.ExportSalesReport(StartDate, EndDate, exportFormat);
                                        break;
                                    case "Inventory Report":
                                        result = await _exportService.ExportInventoryReport(exportFormat);
                                        break;
                                    case "Customer Report":
                                        try {
                                            // Get all customers and prepare report data
                                            var customers = await _customerService.GetAllCustomers();
                                            if(customers == null || !customers.Any()) {
                                                throw new InvalidOperationException("No customer data available to export");
                                            }
                                            
                                            var customerReport = new Services.CustomerReport
                                            {
                                                FromDate = StartDate,
                                                ToDate = EndDate,
                                                TotalCustomers = customers.Count(),
                                                CustomerDetails = customers.Select(c => new Services.CustomerPurchaseDetail
                                                {
                                                    CustomerID = c.CustomerID,
                                                    CustomerName = c.CustomerName,
                                                    PhoneNumber = c.PhoneNumber,
                                                    TotalPurchases = c.TotalPurchases,
                                                    OrderCount = c.Orders?.Count() ?? 0,
                                                    LastPurchaseDate = c.LastPurchaseDate,
                                                    PendingAmount = c.OutstandingAmount,
                                                    LoyaltyPoints = c.LoyaltyPoints
                                                }).ToList()
                                            };
                                            
                                            // First try export service
                                            try {
                                                result = await _exportService.ExportCustomerList(exportFormat);
                                            }
                                            catch (Exception exportEx) {
                                                await _logService.LogWarningAsync($"Export service failed, falling back to print service: {exportEx.Message}");
                                                // Fall back to print service
                                                result = await _printService.GenerateReportAsync("customer", customerReport);
                                            }
                                        }
                                        catch (Exception ex) {
                                            await _logService.LogErrorAsync($"Error generating customer report: {ex.Message}");
                                            throw;
                                        }
                                        break;
                                    case "Financial Report":
                                        try {
                                            var financialReport = await _financeService.GetProfitLossReportAsync(StartDate, EndDate);
                                            result = await _printService.GenerateReportAsync("financial", financialReport);
                                        }
                                        catch (Exception ex) {
                                            await _logService.LogErrorAsync($"Error generating financial report: {ex.Message}");
                                            throw;
                                        }
                                        break;
                                    case "Category Report":
                                        var categories = await _categoryService.GetAllCategories();
                                        result = await _printService.GenerateReportAsync("category", categories);
                                        break;
                                    default:
                                        progressMessage.Close();
                                        throw new NotImplementedException($"Export for {SelectedReportType} is not implemented");
                                }

                                success = true;
                            }
                            catch (Exception ex) when (retryCount < maxRetries - 1 && 
                                                       !(ex is NotImplementedException || 
                                                         ex is ArgumentException))
                            {
                                retryCount++;
                                await _logService.LogWarningAsync($"Export attempt {retryCount} failed: {ex.Message}. Retrying...");
                                await Task.Delay(500); // Brief delay before retry
                            }
                        }

                        progressMessage.Close();

                        // Copy the result file to the user-selected location if needed
                        if (!string.IsNullOrEmpty(result) && File.Exists(result) && result != filePath)
                        {
                            try
                            {
                                // Ensure the target directory exists
                                string targetDir = Path.GetDirectoryName(filePath);
                                if (!Directory.Exists(targetDir))
                                {
                                    Directory.CreateDirectory(targetDir);
                                }
                                
                                // Copy with overwrite if file exists
                                File.Copy(result, filePath, true);
                            }
                            catch (Exception copyEx)
                            {
                                await _logService.LogErrorAsync($"Error copying export file: {copyEx.Message}");
                                MessageBox.Show($"The report was generated but could not be saved to the selected location: {copyEx.Message}",
                                    "File Access Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                                
                                // Use the source file as result
                                filePath = result;
                            }
                        }

                        // Check if export was successful and file exists
                        if (File.Exists(filePath))
                        {
                            var successMessage = $"{SelectedReportType} has been exported successfully!\n\nSaved to: {filePath}";
                            
                            // Ask if user wants to open the file
                            if (MessageBox.Show(successMessage + "\n\nWould you like to open this file now?", 
                                "Export Success", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                            {
                                try
                                {
                                    // Open the file with default application
                                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                    {
                                        FileName = filePath,
                                        UseShellExecute = true
                                    });
                                }
                                catch (Exception openEx)
                                {
                                    MessageBox.Show($"Could not open the file: {openEx.Message}", 
                                        "File Open Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                                }
                            }
                            
                            // Log the successful export
                            await _logService.LogInformationAsync($"Report exported: {SelectedReportType} to {filePath}");
                        }
                        else
                        {
                            throw new Exception("Export failed. The output file was not created.");
                        }
                    }
                    catch (Exception ex)
                    {
                        progressMessage.Close();
                        throw; // Re-throw to be caught by outer try/catch
                    }
                }
            }
            catch (NotImplementedException ex)
            {
                MessageBox.Show($"{ex.Message}. Please contact support to request this feature.", 
                    "Feature Not Available", MessageBoxButton.OK, MessageBoxImage.Information);
                
                await _logService.LogWarningAsync($"Attempted to use unimplemented export: {ex.Message}");
            }
            catch (Exception ex)
            {
                string errorDetails = ex.InnerException != null 
                    ? $"{ex.Message}\n\nAdditional details: {ex.InnerException.Message}" 
                    : ex.Message;
                    
                MessageBox.Show($"Error exporting report: {errorDetails}", 
                    "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Log the detailed error
                await _logService.LogErrorAsync($"Report export error: {ex.Message}", exception: ex);
            }
        }
    }
}