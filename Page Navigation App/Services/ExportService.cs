using ClosedXML.Excel;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Page_Navigation_App.Model;
using System.Text;
using System.Diagnostics;

namespace Page_Navigation_App.Services
{
    /// <summary>
    /// Service for exporting data to various formats
    /// </summary>
    public sealed class ExportService
    {
        private readonly ReportService _reportService;
        private readonly OrderService _orderService;
        private readonly StockService _stockService;
        private readonly CustomerService _customerService;
        private readonly string _exportPath;
        private readonly LogService _logService;
        private readonly object _fileLock = new object();

        public ExportService(
            ReportService reportService,
            OrderService orderService,
            StockService stockService,
            CustomerService customerService,
            LogService logService,
            string exportPath)
        {
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _stockService = stockService ?? throw new ArgumentNullException(nameof(stockService));
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _exportPath = !string.IsNullOrWhiteSpace(exportPath) ? exportPath : 
                throw new ArgumentException("Export path cannot be null or empty", nameof(exportPath));

            // Ensure export directory exists
            if (!Directory.Exists(_exportPath))
            {
                try
                {
                    Directory.CreateDirectory(_exportPath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to create export directory: {ex.Message}");
                    // Create in temp folder as fallback
                    _exportPath = Path.Combine(Path.GetTempPath(), "BoolaPOS_Exports");
                    Directory.CreateDirectory(_exportPath);
                }
            }
        }

        /// <summary>
        /// Exports sales report data to Excel or CSV format
        /// </summary>
        /// <param name="startDate">Start date for report period</param>
        /// <param name="endDate">End date for report period</param>
        /// <param name="format">Export format (xlsx or csv)</param>
        /// <returns>Path to the exported file</returns>
        public async Task<string> ExportSalesReport(
            DateTime startDate,
            DateTime endDate,
            string format = "xlsx")
        {
            try
            {
                if (endDate < startDate)
                {
                    await _logService.LogErrorAsync($"Invalid date range for sales report: {startDate} to {endDate}");
                    throw new ArgumentException("End date cannot be earlier than start date");
                }

                var analytics = _reportService.GetSalesAnalytics(startDate, endDate);
                if (analytics == null)
                {
                    await _logService.LogErrorAsync("Failed to retrieve sales analytics data");
                    throw new InvalidOperationException("Sales data could not be retrieved");
                }

                var fileName = $"SalesReport_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.{format}";
                var filePath = Path.Combine(_exportPath, fileName);

                // Use a lock to prevent multiple threads from writing to the same file
                lock (_fileLock)
                {
                    if (format.ToLower() == "xlsx")
                    {
                        using var workbook = new XLWorkbook();
                        var worksheet = workbook.Worksheets.Add("Sales Report");

                        // Set column headers with formatting
                        var headerRow = worksheet.Row(1);
                        headerRow.Style.Font.Bold = true;
                        headerRow.Style.Fill.BackgroundColor = XLColor.LightBlue;

                        worksheet.Cell(1, 1).Value = "Category";
                        worksheet.Cell(1, 2).Value = "Amount";

                        // Add data
                        var row = 2;
                        foreach (var item in analytics.SalesByCategory)
                        {
                            worksheet.Cell(row, 1).Value = item.CategoryName;
                            worksheet.Cell(row, 2).Value = item.Amount;
                            worksheet.Cell(row, 2).Style.NumberFormat.Format = "₹#,##0.00";
                            row++;
                        }

                        // Add summary section
                        row += 2;
                        worksheet.Cell(row, 1).Value = "Total Sales:";
                        worksheet.Cell(row, 1).Style.Font.Bold = true;
                        worksheet.Cell(row, 2).Value = analytics.TotalSales;
                        worksheet.Cell(row, 2).Style.NumberFormat.Format = "₹#,##0.00";
                        worksheet.Cell(row, 2).Style.Font.Bold = true;

                        // Add metadata
                        row += 2;
                        worksheet.Cell(row, 1).Value = "Report Period:";
                        worksheet.Cell(row, 2).Value = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}";
                        row++;
                        worksheet.Cell(row, 1).Value = "Generated On:";
                        worksheet.Cell(row, 2).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                        // Auto-fit columns
                        worksheet.Columns().AdjustToContents();

                        workbook.SaveAs(filePath);
                    }
                    else if (format.ToLower() == "csv")
                    {
                        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
                        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                        // Write metadata
                        csv.WriteField("Report Period");
                        csv.WriteField($"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
                        csv.NextRecord();
                        csv.WriteField("Generated On");
                        csv.WriteField(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        csv.NextRecord();
                        csv.NextRecord(); // Empty line

                        // Write category sales data
                        csv.WriteField("Category");
                        csv.WriteField("Amount");
                        csv.NextRecord();

                        foreach (var item in analytics.SalesByCategory)
                        {
                            csv.WriteField(item.CategoryName);
                            csv.WriteField(item.Amount.ToString("F2"));
                            csv.NextRecord();
                        }

                        // Write summary
                        csv.NextRecord();
                        csv.WriteField("Total Sales");
                        csv.WriteField(analytics.TotalSales.ToString("F2"));
                        csv.NextRecord();
                    }
                    else
                    {
                        throw new ArgumentException($"Unsupported export format: {format}. Supported formats are 'xlsx' and 'csv'.");
                    }
                }

                await _logService.LogInformationAsync($"Sales report exported successfully: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error exporting sales report: {ex.Message}", exception: ex);
                throw new Exception($"Failed to export sales report: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Exports inventory report data to Excel or CSV format
        /// </summary>
        /// <param name="format">Export format (xlsx or csv)</param>
        /// <returns>Path to the exported file</returns>
        public async Task<string> ExportInventoryReport(string format = "xlsx")
        {
            try
            {
                var inventory = await _stockService.SearchStock(string.Empty);
                if (inventory == null || !inventory.Any())
                {
                    await _logService.LogErrorAsync("No inventory data available for export");
                    throw new InvalidOperationException("No inventory data available to export");
                }

                var fileName = $"InventoryReport_{DateTime.Now:yyyyMMdd_HHmmss}.{format}";
                var filePath = Path.Combine(_exportPath, fileName);

                // Use a lock to prevent multiple threads from writing to the same file
                lock (_fileLock)
                {
                    if (format.ToLower() == "xlsx")
                    {
                        using var workbook = new XLWorkbook();
                        var worksheet = workbook.Worksheets.Add("Inventory");

                        // Headers with formatting
                        var headerRow = worksheet.Row(1);
                        headerRow.Style.Font.Bold = true;
                        headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;
                        
                        worksheet.Cell(1, 1).Value = "Product";
                        worksheet.Cell(1, 2).Value = "Metal Type";
                        worksheet.Cell(1, 3).Value = "Purity";
                        worksheet.Cell(1, 4).Value = "Location";
                        worksheet.Cell(1, 5).Value = "Quantity";
                        worksheet.Cell(1, 6).Value = "Unit Value";
                        worksheet.Cell(1, 7).Value = "Total Value";
                        worksheet.Cell(1, 8).Value = "Status";
                        worksheet.Cell(1, 9).Value = "HUID";

                        // Data
                        var row = 2;
                        decimal totalInventoryValue = 0;
                        foreach (var item in inventory)
                        {
                            var unitValue = item.Product?.BasePrice ?? 0;
                            var totalValue = item.Quantity * unitValue;
                            totalInventoryValue += totalValue;
                            
                            worksheet.Cell(row, 1).Value = item.Product?.ProductName ?? "Unknown";
                            worksheet.Cell(row, 2).Value = item.Product?.MetalType ?? "N/A";
                            worksheet.Cell(row, 3).Value = item.Product?.Purity ?? "N/A";
                            worksheet.Cell(row, 4).Value = item.Location ?? "Main Store";
                            worksheet.Cell(row, 5).Value = item.Quantity;
                            worksheet.Cell(row, 6).Value = unitValue;
                            worksheet.Cell(row, 6).Style.NumberFormat.Format = "₹#,##0.00";
                            worksheet.Cell(row, 7).Value = totalValue;
                            worksheet.Cell(row, 7).Style.NumberFormat.Format = "₹#,##0.00";
                            worksheet.Cell(row, 8).Value = item.StockStatus ?? "Active";
                            worksheet.Cell(row, 9).Value = item.Product?.HUID ?? "N/A";
                            
                            // Highlight low stock items
                            if (item.Quantity <= (item.Product?.ReorderLevel ?? 0))
                            {
                                worksheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightPink;
                            }
                            
                            row++;
                        }

                        // Add summary section
                        row += 2;
                        worksheet.Cell(row, 1).Value = "Total Items:";
                        worksheet.Cell(row, 2).Value = inventory.Count();
                        worksheet.Cell(row, 1).Style.Font.Bold = true;
                        worksheet.Cell(row, 2).Style.Font.Bold = true;
                        
                        row++;
                        worksheet.Cell(row, 1).Value = "Total Inventory Value:";
                        worksheet.Cell(row, 2).Value = totalInventoryValue;
                        worksheet.Cell(row, 2).Style.NumberFormat.Format = "₹#,##0.00";
                        worksheet.Cell(row, 1).Style.Font.Bold = true;
                        worksheet.Cell(row, 2).Style.Font.Bold = true;

                        // Add metadata
                        row += 2;
                        worksheet.Cell(row, 1).Value = "Generated On:";
                        worksheet.Cell(row, 2).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                        // Auto-fit columns for better readability
                        worksheet.Columns().AdjustToContents();
                        
                        // Save the workbook
                        workbook.SaveAs(filePath);
                    }
                    else if (format.ToLower() == "csv")
                    {
                        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
                        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                        // Write metadata
                        csv.WriteField("Inventory Report");
                        csv.WriteField(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        csv.NextRecord();
                        csv.NextRecord(); // Empty line

                        // Write headers
                        csv.WriteField("Product");
                        csv.WriteField("Metal Type");
                        csv.WriteField("Purity");
                        csv.WriteField("Location");
                        csv.WriteField("Quantity");
                        csv.WriteField("Unit Value");
                        csv.WriteField("Total Value");
                        csv.WriteField("Status");
                        csv.WriteField("HUID");
                        csv.NextRecord();

                        // Write data
                        decimal totalInventoryValue = 0;
                        foreach (var item in inventory)
                        {
                            var unitValue = item.Product?.BasePrice ?? 0;
                            var totalValue = item.Quantity * unitValue;
                            totalInventoryValue += totalValue;
                            
                            csv.WriteField(item.Product?.ProductName ?? "Unknown");
                            csv.WriteField(item.Product?.MetalType ?? "N/A");
                            csv.WriteField(item.Product?.Purity ?? "N/A");
                            csv.WriteField(item.Location ?? "Main Store");
                            csv.WriteField(item.Quantity);
                            csv.WriteField(unitValue.ToString("F2"));
                            csv.WriteField(totalValue.ToString("F2"));
                            csv.WriteField(item.StockStatus ?? "Active");
                            csv.WriteField(item.Product?.HUID ?? "N/A");
                            csv.NextRecord();
                        }

                        // Write summary
                        csv.NextRecord();
                        csv.WriteField("Total Items");
                        csv.WriteField(inventory.Count());
                        csv.NextRecord();
                        csv.WriteField("Total Inventory Value");
                        csv.WriteField(totalInventoryValue.ToString("F2"));
                    }
                    else
                    {
                        throw new ArgumentException($"Unsupported export format: {format}. Supported formats are 'xlsx' and 'csv'.");
                    }
                }

                await _logService.LogInformationAsync($"Inventory report exported successfully: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error exporting inventory report: {ex.Message}", exception: ex);
                throw new Exception($"Failed to export inventory report: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Exports GST report data to Excel or CSV format
        /// </summary>
        /// <param name="startDate">Start date for report period</param>
        /// <param name="endDate">End date for report period</param>
        /// <param name="format">Export format (xlsx or csv)</param>
        /// <returns>Path to the exported file</returns>
        public async Task<string> ExportGSTReport(
            DateTime startDate,
            DateTime endDate,
            string format = "xlsx")
        {
            try
            {
                if (endDate < startDate)
                {
                    await _logService.LogErrorAsync($"Invalid date range for GST report: {startDate} to {endDate}");
                    throw new ArgumentException("End date cannot be earlier than start date");
                }

                var gstData = _reportService.GetFinancialReports(startDate, endDate);
                if (gstData == null)
                {
                    await _logService.LogErrorAsync("Failed to retrieve GST report data");
                    throw new InvalidOperationException("GST data could not be retrieved");
                }

                var fileName = $"GSTReport_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.{format}";
                var filePath = Path.Combine(_exportPath, fileName);

                // Use a lock to prevent multiple threads from writing to the same file
                lock (_fileLock)
                {
                    if (format.ToLower() == "xlsx")
                    {
                        using var workbook = new XLWorkbook();
                        var worksheet = workbook.Worksheets.Add("GST Report");

                        // Add headers with formatting
                        var headerRow = worksheet.Row(1);
                        headerRow.Style.Font.Bold = true;
                        headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                        worksheet.Cell(1, 1).Value = "Category";
                        worksheet.Cell(1, 2).Value = "Sales";
                        worksheet.Cell(1, 3).Value = "Expenses"; 
                        worksheet.Cell(1, 4).Value = "Profit/Loss";

                        // Format headers
                        worksheet.Range("A1:D1").Style.Font.Bold = true;

                        // Add total data with formatting
                        worksheet.Cell(2, 1).Value = "Total";
                        worksheet.Cell(2, 1).Style.Font.Bold = true;
                        worksheet.Cell(2, 2).Value = gstData.TotalSales;
                        worksheet.Cell(2, 2).Style.NumberFormat.Format = "₹#,##0.00";
                        worksheet.Cell(2, 3).Value = gstData.TotalExpenses;
                        worksheet.Cell(2, 3).Style.NumberFormat.Format = "₹#,##0.00";
                        worksheet.Cell(2, 4).Value = gstData.GrossProfit;
                        worksheet.Cell(2, 4).Style.NumberFormat.Format = "₹#,##0.00";
                        
                        // Format total row
                        worksheet.Range("A2:D2").Style.Fill.BackgroundColor = XLColor.LightCyan;

                        // Category sales breakdown header
                        var row = 4;
                        worksheet.Cell(3, 1).Value = "By Category";
                        worksheet.Cell(3, 1).Style.Font.Bold = true;
                        
                        // Add category sales data
                        foreach (var category in gstData.SalesByCategory)
                        {
                            worksheet.Cell(row, 1).Value = category.CategoryName;
                            worksheet.Cell(row, 2).Value = category.SalesAmount;
                            worksheet.Cell(row, 2).Style.NumberFormat.Format = "₹#,##0.00";
                            row++;
                        }

                        // Expenses by category header
                        row += 2;
                        worksheet.Cell(row, 1).Value = "Expenses by Category";
                        worksheet.Cell(row, 1).Style.Font.Bold = true;
                        row++;
                        
                        // Add expense category data
                        foreach (var expense in gstData.ExpensesByCategory)
                        {
                            worksheet.Cell(row, 1).Value = expense.CategoryName;
                            worksheet.Cell(row, 3).Value = expense.ExpenseAmount;
                            worksheet.Cell(row, 3).Style.NumberFormat.Format = "₹#,##0.00";
                            row++;
                        }

                        // Add report metadata
                        row += 2;
                        worksheet.Cell(row, 1).Value = "Report Period:";
                        worksheet.Cell(row, 2).Value = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}";
                        row++;
                        worksheet.Cell(row, 1).Value = "Generated On:";
                        worksheet.Cell(row, 2).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                        // Auto-fit columns for better readability
                        worksheet.Columns().AdjustToContents();

                        workbook.SaveAs(filePath);
                    }
                    else if (format.ToLower() == "csv")
                    {
                        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
                        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                        // Write metadata
                        csv.WriteField("Report Period");
                        csv.WriteField($"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
                        csv.NextRecord();
                        csv.WriteField("Generated On");
                        csv.WriteField(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        csv.NextRecord();
                        csv.NextRecord(); // Empty line

                        // Write totals
                        csv.WriteField("Category");
                        csv.WriteField("Sales");
                        csv.WriteField("Expenses");
                        csv.WriteField("Profit/Loss");
                        csv.NextRecord();

                        csv.WriteField("Total");
                        csv.WriteField(gstData.TotalSales.ToString("F2"));
                        csv.WriteField(gstData.TotalExpenses.ToString("F2"));
                        csv.WriteField(gstData.GrossProfit.ToString("F2"));
                        csv.NextRecord();
                        csv.NextRecord();
                        
                        // Write sales by category
                        csv.WriteField("By Category");
                        csv.NextRecord();

                        foreach (var category in gstData.SalesByCategory)
                        {
                            csv.WriteField(category.CategoryName);
                            csv.WriteField(category.SalesAmount.ToString("F2"));
                            csv.NextRecord();
                        }
                        csv.NextRecord();
                        
                        // Write expenses by category
                        csv.WriteField("Expenses by Category");
                        csv.NextRecord();
                        
                        foreach (var expense in gstData.ExpensesByCategory)
                        {
                            csv.WriteField(expense.CategoryName);
                            csv.WriteField("");
                            csv.WriteField(expense.ExpenseAmount.ToString("F2"));
                            csv.NextRecord();
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"Unsupported export format: {format}. Supported formats are 'xlsx' and 'csv'.");
                    }
                }

                await _logService.LogInformationAsync($"GST report exported successfully: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error exporting GST report: {ex.Message}", exception: ex);
                throw new Exception($"Failed to export GST report: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Exports customer list to Excel or CSV format
        /// </summary>
        /// <param name="format">Export format (xlsx or csv)</param>
        /// <returns>Path to the exported file</returns>
        public async Task<string> ExportCustomerList(string format = "xlsx")
        {
            try
            {
                // Get all customers from customer service
                var customers = await _customerService.GetAllCustomers();
                if (customers == null || !customers.Any())
                {
                    await _logService.LogErrorAsync("No customer data available for export");
                    throw new InvalidOperationException("No customer data available to export");
                }

                var fileName = $"CustomerList_{DateTime.Now:yyyyMMdd_HHmmss}.{format}";
                var filePath = Path.Combine(_exportPath, fileName);

                // Use a lock to prevent multiple threads from writing to the same file
                lock (_fileLock)
                {
                    if (format.ToLower() == "xlsx")
                    {
                        using var workbook = new XLWorkbook();
                        var worksheet = workbook.Worksheets.Add("Customers");

                        // Headers with formatting
                        var headerRow = worksheet.Row(1);
                        headerRow.Style.Font.Bold = true;
                        headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                        worksheet.Cell(1, 1).Value = "ID";
                        worksheet.Cell(1, 2).Value = "Name";
                        worksheet.Cell(1, 3).Value = "Phone";
                        worksheet.Cell(1, 4).Value = "Email";
                        worksheet.Cell(1, 5).Value = "Address";
                        worksheet.Cell(1, 6).Value = "Customer Type";
                        worksheet.Cell(1, 7).Value = "Loyalty Points";
                        worksheet.Cell(1, 8).Value = "Outstanding Amount";
                        worksheet.Cell(1, 9).Value = "Registration Date";

                        // Data
                        var row = 2;
                        foreach (var customer in customers)
                        {
                            worksheet.Cell(row, 1).Value = customer.CustomerID;
                            worksheet.Cell(row, 2).Value = customer.CustomerName;
                            worksheet.Cell(row, 3).Value = customer.PhoneNumber;
                            worksheet.Cell(row, 4).Value = customer.Email;
                            worksheet.Cell(row, 5).Value = customer.Address;
                            worksheet.Cell(row, 6).Value = customer.CustomerType;
                            worksheet.Cell(row, 7).Value = customer.LoyaltyPoints;
                            worksheet.Cell(row, 8).Value = customer.OutstandingAmount;
                            worksheet.Cell(row, 8).Style.NumberFormat.Format = "₹#,##0.00";
                            worksheet.Cell(row, 9).Value = customer.RegistrationDate;
                            
                            // Highlight customers with outstanding amounts
                            if (customer.OutstandingAmount > 0)
                            {
                                worksheet.Cell(row, 8).Style.Font.FontColor = XLColor.Red;
                                worksheet.Cell(row, 8).Style.Font.Bold = true;
                            }
                            
                            row++;
                        }

                        // Summary section
                        row += 2;
                        worksheet.Cell(row, 1).Value = "Total Customers:";
                        worksheet.Cell(row, 2).Value = customers.Count();
                        worksheet.Cell(row, 1).Style.Font.Bold = true;
                        row++;
                        
                        worksheet.Cell(row, 1).Value = "Total Outstanding:";
                        worksheet.Cell(row, 2).Value = customers.Sum(c => c.OutstandingAmount);
                        worksheet.Cell(row, 2).Style.NumberFormat.Format = "₹#,##0.00";
                        worksheet.Cell(row, 1).Style.Font.Bold = true;
                        row++;
                        
                        worksheet.Cell(row, 1).Value = "Generated On:";
                        worksheet.Cell(row, 2).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        
                        // Auto-fit columns
                        worksheet.Columns().AdjustToContents();

                        workbook.SaveAs(filePath);
                    }
                    else if (format.ToLower() == "csv")
                    {
                        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
                        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                        // Write metadata
                        csv.WriteField("Customer Report");
                        csv.WriteField(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        csv.NextRecord();
                        csv.NextRecord(); // Empty line

                        // Write headers
                        csv.WriteField("ID");
                        csv.WriteField("Name");
                        csv.WriteField("Phone");
                        csv.WriteField("Email");
                        csv.WriteField("Address");
                        csv.WriteField("Customer Type");
                        csv.WriteField("Loyalty Points");
                        csv.WriteField("Outstanding Amount");
                        csv.WriteField("Registration Date");
                        csv.NextRecord();

                        // Write data
                        foreach (var customer in customers)
                        {
                            csv.WriteField(customer.CustomerID);
                            csv.WriteField(customer.CustomerName);
                            csv.WriteField(customer.PhoneNumber);
                            csv.WriteField(customer.Email);
                            csv.WriteField(customer.Address);
                            csv.WriteField(customer.CustomerType);
                            csv.WriteField(customer.LoyaltyPoints);
                            csv.WriteField(customer.OutstandingAmount.ToString("F2"));
                            csv.WriteField(customer.RegistrationDate.ToString("yyyy-MM-dd"));
                            csv.NextRecord();
                        }

                        // Write summary
                        csv.NextRecord();
                        csv.WriteField("Total Customers");
                        csv.WriteField(customers.Count());
                        csv.NextRecord();
                        csv.WriteField("Total Outstanding");
                        csv.WriteField(customers.Sum(c => c.OutstandingAmount).ToString("F2"));
                        csv.NextRecord();
                    }
                    else
                    {
                        throw new ArgumentException($"Unsupported export format: {format}. Supported formats are 'xlsx' and 'csv'.");
                    }
                }

                await _logService.LogInformationAsync($"Customer list exported successfully: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error exporting customer list: {ex.Message}", exception: ex);
                throw new Exception($"Failed to export customer list: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Exports repair report data to Excel or CSV format
        /// </summary>
        /// <param name="startDate">Start date for report period</param>
        /// <param name="endDate">End date for report period</param>
        /// <param name="format">Export format (xlsx or csv)</param>
        /// <returns>Path to the exported file</returns>
        public async Task<string> ExportRepairReport(
            DateTime startDate,
            DateTime endDate,
            string format = "xlsx")
        {
            try
            {
                if (endDate < startDate)
                {
                    await _logService.LogErrorAsync($"Invalid date range for repair report: {startDate} to {endDate}");
                    throw new ArgumentException("End date cannot be earlier than start date");
                }

                var repairs = _reportService.GetRepairAnalytics(startDate, endDate);
                if (repairs == null || repairs.JobDetails == null || !repairs.JobDetails.Any())
                {
                    await _logService.LogErrorAsync("No repair data available for export");
                    throw new InvalidOperationException("No repair data available to export");
                }

                var fileName = $"RepairReport_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.{format}";
                var filePath = Path.Combine(_exportPath, fileName);

                // Use a lock to prevent multiple threads from writing to the same file
                lock (_fileLock)
                {
                    if (format.ToLower() == "xlsx")
                    {
                        using var workbook = new XLWorkbook();
                        var worksheet = workbook.Worksheets.Add("Repair Report");

                        // Headers with formatting
                        var headerRow = worksheet.Row(1);
                        headerRow.Style.Font.Bold = true;
                        headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                        worksheet.Cell(1, 1).Value = "Job ID";
                        worksheet.Cell(1, 2).Value = "Customer";
                        worksheet.Cell(1, 3).Value = "Item Description";
                        worksheet.Cell(1, 4).Value = "Work Type";
                        worksheet.Cell(1, 5).Value = "Status";
                        worksheet.Cell(1, 6).Value = "Receipt Date";
                        worksheet.Cell(1, 7).Value = "Delivery Date";
                        worksheet.Cell(1, 8).Value = "Estimated Cost";
                        worksheet.Cell(1, 9).Value = "Final Amount";

                        // Data
                        var row = 2;
                        foreach (var job in repairs.JobDetails)
                        {
                            worksheet.Cell(row, 1).Value = job.RepairID;
                            worksheet.Cell(row, 2).Value = job.CustomerName;
                            worksheet.Cell(row, 3).Value = job.ItemDescription;
                            worksheet.Cell(row, 4).Value = job.WorkType;
                            worksheet.Cell(row, 5).Value = job.Status;
                            worksheet.Cell(row, 6).Value = job.ReceiptDate;
                            worksheet.Cell(row, 7).Value = job.DeliveryDate;
                            worksheet.Cell(row, 8).Value = job.EstimatedCost;
                            worksheet.Cell(row, 8).Style.NumberFormat.Format = "₹#,##0.00";
                            worksheet.Cell(row, 9).Value = job.FinalAmount;
                            worksheet.Cell(row, 9).Style.NumberFormat.Format = "₹#,##0.00";
                            
                            // Highlight based on status
                            if (job.Status == "Pending")
                            {
                                worksheet.Cell(row, 5).Style.Fill.BackgroundColor = XLColor.LightYellow;
                            }
                            else if (job.Status == "Completed")
                            {
                                worksheet.Cell(row, 5).Style.Fill.BackgroundColor = XLColor.LightGreen;
                            }
                            else if (job.Status == "Delayed")
                            {
                                worksheet.Cell(row, 5).Style.Fill.BackgroundColor = XLColor.LightPink;
                            }
                            
                            row++;
                        }

                        // Add summary section with formatting
                        row += 2;
                        worksheet.Cell(row, 1).Value = "Summary";
                        worksheet.Cell(row, 1).Style.Font.Bold = true;
                        worksheet.Cell(row, 1).Style.Font.FontSize = 14;
                        row++;
                        
                        worksheet.Cell(row, 1).Value = "Total Jobs:";
                        worksheet.Cell(row, 2).Value = repairs.TotalJobs;
                        worksheet.Cell(row, 1).Style.Font.Bold = true;
                        row++;
                        
                        worksheet.Cell(row, 1).Value = "Completed Jobs:";
                        worksheet.Cell(row, 2).Value = repairs.CompletedJobs;
                        worksheet.Cell(row, 1).Style.Font.Bold = true;
                        row++;
                        
                        worksheet.Cell(row, 1).Value = "Pending Jobs:";
                        worksheet.Cell(row, 2).Value = repairs.PendingJobs;
                        worksheet.Cell(row, 1).Style.Font.Bold = true;
                        row++;
                        
                        worksheet.Cell(row, 1).Value = "Total Revenue:";
                        worksheet.Cell(row, 2).Value = repairs.TotalRevenue;
                        worksheet.Cell(row, 2).Style.NumberFormat.Format = "₹#,##0.00";
                        worksheet.Cell(row, 1).Style.Font.Bold = true;
                        worksheet.Cell(row, 2).Style.Font.Bold = true;
                        
                        // Add report metadata
                        row += 2;
                        worksheet.Cell(row, 1).Value = "Report Period:";
                        worksheet.Cell(row, 2).Value = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}";
                        row++;
                        worksheet.Cell(row, 1).Value = "Generated On:";
                        worksheet.Cell(row, 2).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                        // Auto-fit columns for better readability
                        worksheet.Columns().AdjustToContents();

                        workbook.SaveAs(filePath);
                    }
                    else if (format.ToLower() == "csv")
                    {
                        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
                        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                        // Write metadata
                        csv.WriteField("Repair Report");
                        csv.WriteField($"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
                        csv.NextRecord();
                        csv.WriteField("Generated On");
                        csv.WriteField(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        csv.NextRecord();
                        csv.NextRecord(); // Empty line

                        // Write headers
                        csv.WriteField("Job ID");
                        csv.WriteField("Customer");
                        csv.WriteField("Item Description");
                        csv.WriteField("Work Type");
                        csv.WriteField("Status");
                        csv.WriteField("Receipt Date");
                        csv.WriteField("Delivery Date");
                        csv.WriteField("Estimated Cost");
                        csv.WriteField("Final Amount");
                        csv.NextRecord();

                        // Write data
                        foreach (var job in repairs.JobDetails)
                        {
                            csv.WriteField(job.RepairID);
                            csv.WriteField(job.CustomerName);
                            csv.WriteField(job.ItemDescription);
                            csv.WriteField(job.WorkType);
                            csv.WriteField(job.Status);
                            csv.WriteField(job.ReceiptDate.ToString("yyyy-MM-dd"));
                            csv.WriteField(job.DeliveryDate.HasValue ? job.DeliveryDate.Value.ToString("yyyy-MM-dd") : "N/A");
                            csv.WriteField(job.EstimatedCost.ToString("F2"));
                            csv.WriteField(job.FinalAmount.ToString("F2"));
                            csv.NextRecord();
                        }

                        // Write summary
                        csv.NextRecord();
                        csv.WriteField("Summary");
                        csv.NextRecord();
                        csv.WriteField("Total Jobs");
                        csv.WriteField(repairs.TotalJobs);
                        csv.NextRecord();
                        csv.WriteField("Completed Jobs");
                        csv.WriteField(repairs.CompletedJobs);
                        csv.NextRecord();
                        csv.WriteField("Pending Jobs");
                        csv.WriteField(repairs.PendingJobs);
                        csv.NextRecord();
                        csv.WriteField("Total Revenue");
                        csv.WriteField(repairs.TotalRevenue.ToString("F2"));
                        csv.NextRecord();
                    }
                    else
                    {
                        throw new ArgumentException($"Unsupported export format: {format}. Supported formats are 'xlsx' and 'csv'.");
                    }
                }

                await _logService.LogInformationAsync($"Repair report exported successfully: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error exporting repair report: {ex.Message}", exception: ex);
                throw new Exception($"Failed to export repair report: {ex.Message}", ex);
            }
        }
    }
}