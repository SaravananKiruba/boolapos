using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;

namespace Page_Navigation_App.Services
{
    /// <summary>
    /// Service to generate business reports and analytics for the jewelry shop
    /// </summary>
    public class ReportService
    {
        private readonly AppDbContext _context;
        private readonly LogService _logService;

        public ReportService(AppDbContext context, LogService logService)
        {
            _context = context;
            _logService = logService;
        }

        /// <summary>
        /// Get sales dashboard data for specified period
        /// </summary>
        public async Task<DashboardData> GetDashboardDataAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var orders = await _context.Orders
                    .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate)
                    .ToListAsync();

                var customers = await _context.Customers
                    .Where(c => c.RegistrationDate >= fromDate && c.RegistrationDate <= toDate)
                    .ToListAsync();

                var products = await _context.Products
                    .Where(p => p.StockQuantity <= p.ReorderLevel)
                    .ToListAsync();

                var repairJobs = await _context.RepairJobs
                    .Where(r => r.ReceiptDate >= fromDate && r.ReceiptDate <= toDate)
                    .ToListAsync();

                return new DashboardData
                {
                    TotalSales = orders.Sum(o => o.GrandTotal),
                    OrderCount = orders.Count,
                    NewCustomers = customers.Count,
                    PendingRepairs = repairJobs.Count(r => r.Status == "Received" || r.Status == "In Progress"),
                    LowStockCount = products.Count,
                    GoldSalesAmount = await GetMetalSalesAsync(fromDate, toDate, "Gold"),
                    SilverSalesAmount = await GetMetalSalesAsync(fromDate, toDate, "Silver"),
                    TopSellingProducts = await GetTopSellingProductsAsync(fromDate, toDate, 5),
                    MonthlySalesData = await GetMonthlySalesAsync(fromDate, toDate),
                    SalesByCategory = await GetSalesByCategoryAsync(fromDate, toDate)
                };
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error generating dashboard data: {ex.Message}");
                return new DashboardData();
            }
        }

        /// <summary>
        /// Get sales for a specific metal type
        /// </summary>
        private async Task<decimal> GetMetalSalesAsync(DateTime fromDate, DateTime toDate, string metalType)
        {
            var sales = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Product)
                .Where(od => od.Order.OrderDate >= fromDate && 
                            od.Order.OrderDate <= toDate &&
                            od.Product.MetalType.Contains(metalType))
                .SumAsync(od => od.TotalPrice);
                
            return sales;
        }

        /// <summary>
        /// Get top selling products for the period
        /// </summary>
        private async Task<List<TopSellingProduct>> GetTopSellingProductsAsync(
            DateTime fromDate, 
            DateTime toDate, 
            int count)
        {
            var topProducts = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Product)
                .Where(od => od.Order.OrderDate >= fromDate && od.Order.OrderDate <= toDate)
                .GroupBy(od => new { od.ProductID, od.Product.ProductName })
                .Select(g => new TopSellingProduct
                {
                    ProductName = g.Key.ProductName,
                    // Add explicit cast from decimal to int
                    Quantity = (int)g.Sum(od => od.Quantity),
                    Amount = g.Sum(od => od.TotalPrice)
                })
                .OrderByDescending(p => p.Amount)
                .Take(count)
                .ToListAsync();
                
            return topProducts;
        }

        /// <summary>
        /// Get monthly sales data for trend analysis
        /// </summary>
        private async Task<List<MonthlySales>> GetMonthlySalesAsync(DateTime fromDate, DateTime toDate)
        {
            // Ensure the date range spans at least one full month
            if ((toDate - fromDate).TotalDays < 28)
            {
                toDate = fromDate.AddMonths(1);
            }

            var monthlySales = new List<MonthlySales>();
            var currentDate = new DateTime(fromDate.Year, fromDate.Month, 1);
            
            while (currentDate <= toDate)
            {
                var monthStart = currentDate;
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                
                var totalSales = await _context.Orders
                    .Where(o => o.OrderDate >= monthStart && o.OrderDate <= monthEnd)
                    .SumAsync(o => o.GrandTotal);
                
                monthlySales.Add(new MonthlySales
                {
                    Month = currentDate.ToString("MMM yyyy"),
                    Amount = totalSales
                });
                
                currentDate = currentDate.AddMonths(1);
            }
            
            return monthlySales;
        }

        /// <summary>
        /// Get sales data by product category
        /// </summary>
        private async Task<List<CategorySales>> GetSalesByCategoryAsync(DateTime fromDate, DateTime toDate)
        {
            var categorySales = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Product)
                .ThenInclude(p => p.Category)
                .Where(od => od.Order.OrderDate >= fromDate && od.Order.OrderDate <= toDate)
                .GroupBy(od => new { od.Product.CategoryID, od.Product.Category.CategoryName })
                .Select(g => new CategorySales
                {
                    CategoryName = g.Key.CategoryName,
                    Amount = g.Sum(od => od.TotalPrice)
                })
                .OrderByDescending(c => c.Amount)
                .ToListAsync();
                
            return categorySales;
        }

        /// <summary>
        /// Generate inventory report with valuation
        /// </summary>
        public async Task<InventoryReport> GenerateInventoryReportAsync()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .ToListAsync();
                
                var goldProducts = products.Where(p => p.MetalType.Contains("Gold")).ToList();
                var silverProducts = products.Where(p => p.MetalType.Contains("Silver")).ToList();
                
                return new InventoryReport
                {
                    TotalProducts = products.Count,
                    TotalValue = products.Sum(p => p.FinalPrice * p.StockQuantity),
                    GoldItems = goldProducts.Count,
                    GoldWeight = goldProducts.Sum(p => p.GrossWeight * p.StockQuantity),
                    GoldValue = goldProducts.Sum(p => p.FinalPrice * p.StockQuantity),
                    SilverItems = silverProducts.Count,
                    SilverWeight = silverProducts.Sum(p => p.GrossWeight * p.StockQuantity),
                    SilverValue = silverProducts.Sum(p => p.FinalPrice * p.StockQuantity),
                    LowStockItems = products.Count(p => p.StockQuantity <= p.ReorderLevel),
                    ProductDetails = products.Select(p => new ProductReportItem
                    {
                        ProductID = p.ProductID,
                        ProductName = p.ProductName,
                        Category = p.Category.CategoryName,
                        MetalType = p.MetalType,
                        Purity = p.Purity,
                        GrossWeight = p.GrossWeight,
                        NetWeight = p.NetWeight,
                        StockQuantity = p.StockQuantity,
                        Value = p.FinalPrice * p.StockQuantity,
                        HUID = p.HUID,
                        TagNumber = p.TagNumber
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error generating inventory report: {ex.Message}");
                return new InventoryReport();
            }
        }

        /// <summary>
        /// Generate GST compliance report for tax filing
        /// </summary>
        public async Task<GSTReport> GenerateGSTReportAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.Customer)
                    .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate)
                    .ToListAsync();
                
                return new GSTReport
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    TotalInvoices = orders.Count,
                    TotalSales = orders.Sum(o => o.TotalAmount),
                    TotalCGST = orders.Sum(o => o.CGST),
                    TotalSGST = orders.Sum(o => o.SGST),
                    TotalIGST = orders.Sum(o => o.IGST),
                    TotalTax = orders.Sum(o => o.CGST + o.SGST + o.IGST),
                    B2BSales = orders.Where(o => !string.IsNullOrEmpty(o.GSTNumber)).Sum(o => o.TotalAmount),
                    B2CSales = orders.Where(o => string.IsNullOrEmpty(o.GSTNumber)).Sum(o => o.TotalAmount),
                    InvoiceDetails = orders.Select(o => new GSTInvoiceDetail
                    {
                        InvoiceNumber = o.InvoiceNumber,
                        InvoiceDate = o.OrderDate,
                        CustomerName = o.Customer.CustomerName,
                        CustomerGST = o.GSTNumber,
                        HSNCode = o.HSNCode,
                        TaxableValue = o.TotalAmount,
                        CGST = o.CGST,
                        SGST = o.SGST,
                        IGST = o.IGST,
                        Total = o.GrandTotal
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error generating GST report: {ex.Message}");
                return new GSTReport();
            }
        }

        /// <summary>
        /// Generate repair jobs report
        /// </summary>
        public async Task<RepairJobsReport> GenerateRepairJobsReportAsync(
            DateTime fromDate, 
            DateTime toDate, 
            string status = null)
        {
            try
            {
                var query = _context.RepairJobs
                    .Include(rj => rj.Customer)
                    .Where(rj => rj.ReceiptDate >= fromDate && rj.ReceiptDate <= toDate);
                
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(rj => rj.Status == status);
                }
                
                var repairJobs = await query.ToListAsync();
                
                return new RepairJobsReport
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    Status = status,
                    TotalJobs = repairJobs.Count,
                    CompletedJobs = repairJobs.Count(rj => rj.Status == "Delivered"),
                    PendingJobs = repairJobs.Count(rj => rj.Status == "Received" || rj.Status == "In Progress"),
                    TotalRevenue = repairJobs.Where(rj => rj.Status == "Delivered").Sum(rj => rj.FinalAmount),
                    JobDetails = repairJobs.Select(rj => new RepairJobDetail
                    {
                        RepairID = rj.RepairID,
                        CustomerName = rj.Customer.CustomerName,
                        ItemDescription = rj.ItemDescription,
                        ReceiptDate = rj.ReceiptDate,
                        DeliveryDate = rj.DeliveryDate,
                        Status = rj.Status,
                        EstimatedCost = rj.EstimatedCost,
                        FinalAmount = rj.FinalAmount,
                        WorkType = rj.WorkType
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error generating repair jobs report: {ex.Message}");
                return new RepairJobsReport();
            }
        }

        /// <summary>
        /// Generate customer purchase report
        /// </summary>
        public async Task<CustomerReport> GenerateCustomerReportAsync(
            DateTime fromDate, 
            DateTime toDate, 
            int? customerId = null)
        {
            try
            {
                var query = _context.Customers.AsQueryable();
                
                if (customerId.HasValue)
                {
                    query = query.Where(c => c.CustomerID == customerId.Value);
                }
                
                var customers = await query.ToListAsync();
                
                var customerReports = new List<CustomerPurchaseDetail>();
                
                foreach (var customer in customers)
                {
                    var orders = await _context.Orders
                        .Where(o => o.CustomerID == customer.CustomerID && 
                                    o.OrderDate >= fromDate && 
                                    o.OrderDate <= toDate)
                        .ToListAsync();
                    
                    var repairJobs = await _context.RepairJobs
                        .Where(rj => rj.CustomerId == customer.CustomerID && 
                                     rj.ReceiptDate >= fromDate && 
                                     rj.ReceiptDate <= toDate)
                        .ToListAsync();
                    
                    customerReports.Add(new CustomerPurchaseDetail
                    {
                        CustomerID = customer.CustomerID,
                        CustomerName = customer.CustomerName,
                        PhoneNumber = customer.PhoneNumber,
                        TotalPurchases = orders.Sum(o => o.GrandTotal),
                        OrderCount = orders.Count,
                        LastPurchaseDate = orders.Any() ? orders.Max(o => o.OrderDate) : (DateTime?)null,
                        RepairJobCount = repairJobs.Count,
                        PendingAmount = customer.OutstandingAmount,
                        LoyaltyPoints = customer.LoyaltyPoints
                    });
                }
                
                return new CustomerReport
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    TotalCustomers = customers.Count,
                    TotalPurchases = customerReports.Sum(c => c.TotalPurchases),
                    CustomerDetails = customerReports.OrderByDescending(c => c.TotalPurchases).ToList()
                };
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error generating customer report: {ex.Message}");
                return new CustomerReport();
            }
        }

        /// <summary>
        /// Export report data to ReportData table for printing/exporting
        /// </summary>
        public async Task<bool> ExportReportToDataTableAsync(string reportType, object reportData)
        {
            try
            {
                var jsonData = System.Text.Json.JsonSerializer.Serialize(reportData);
                
                var reportEntity = new ReportData
                {
                    ReportType = reportType,
                    Date = DateTime.Now,
                    DetailedReportData = jsonData,
                    GeneratedOn = DateTime.Now,
                    GeneratedBy = "System"
                };
                
                await _context.ReportData.AddAsync(reportEntity);
                await _context.SaveChangesAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error exporting report: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get sales analytics data
        /// </summary>
        public async Task<DashboardData> GetSalesAnalyticsAsync(DateTime fromDate, DateTime toDate)
        {
            return await GetDashboardDataAsync(fromDate, toDate);
        }

        /// <summary>
        /// Get sales analytics data (synchronous version)
        /// </summary>
        public DashboardData GetSalesAnalytics(DateTime fromDate, DateTime toDate)
        {
            return GetSalesAnalyticsAsync(fromDate, toDate).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Get financial reports
        /// </summary>
        public ProfitLossReport GetFinancialReports(DateTime fromDate, DateTime toDate)
        {
            try
            {
                // For financial reports, we'll use the ProfitLossReport which contains comprehensive financial data
                var financeService = new FinanceService(_context, _logService);
                return financeService.GetProfitLossReportAsync(fromDate, toDate).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error generating financial reports: {ex.Message}");
                return new ProfitLossReport
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    TotalSales = 0,
                    TotalExpenses = 0,
                    GrossProfit = 0
                };
            }
        }

        /// <summary>
        /// Get repair analytics data
        /// </summary>
        public RepairJobsReport GetRepairAnalytics(DateTime fromDate, DateTime toDate, string status = null)
        {
            return GenerateRepairJobsReportAsync(fromDate, toDate, status).GetAwaiter().GetResult();
        }
    }

    // Report data classes
    public class DashboardData
    {
        public decimal TotalSales { get; set; }
        public int OrderCount { get; set; }
        public int NewCustomers { get; set; }
        public int PendingRepairs { get; set; }
        public int LowStockCount { get; set; }
        public decimal GoldSalesAmount { get; set; }
        public decimal SilverSalesAmount { get; set; }
        public List<TopSellingProduct> TopSellingProducts { get; set; } = new List<TopSellingProduct>();
        public List<MonthlySales> MonthlySalesData { get; set; } = new List<MonthlySales>();
        public List<CategorySales> SalesByCategory { get; set; } = new List<CategorySales>();
    }

    public class TopSellingProduct
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Amount { get; set; }
    }

    public class MonthlySales
    {
        public string Month { get; set; }
        public decimal Amount { get; set; }
    }

    public class CategorySales
    {
        public string CategoryName { get; set; }
        public decimal Amount { get; set; }
    }

    public class InventoryReport
    {
        public int TotalProducts { get; set; }
        public decimal TotalValue { get; set; }
        public int GoldItems { get; set; }
        public decimal GoldWeight { get; set; }
        public decimal GoldValue { get; set; }
        public int SilverItems { get; set; }
        public decimal SilverWeight { get; set; }
        public decimal SilverValue { get; set; }
        public int LowStockItems { get; set; }
        public List<ProductReportItem> ProductDetails { get; set; } = new List<ProductReportItem>();
    }

    public class ProductReportItem
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string Category { get; set; }
        public string MetalType { get; set; }
        public string Purity { get; set; }
        public decimal GrossWeight { get; set; }
        public decimal NetWeight { get; set; }
        public int StockQuantity { get; set; }
        public decimal Value { get; set; }
        public string HUID { get; set; }
        public string TagNumber { get; set; }
    }

    public class GSTReport
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalInvoices { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalCGST { get; set; }
        public decimal TotalSGST { get; set; }
        public decimal TotalIGST { get; set; }
        public decimal TotalTax { get; set; }
        public decimal B2BSales { get; set; }
        public decimal B2CSales { get; set; }
        public List<GSTInvoiceDetail> InvoiceDetails { get; set; } = new List<GSTInvoiceDetail>();
    }

    public class GSTInvoiceDetail
    {
        public string InvoiceNumber { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string CustomerName { get; set; }
        public string CustomerGST { get; set; }
        public string HSNCode { get; set; }
        public decimal TaxableValue { get; set; }
        public decimal CGST { get; set; }
        public decimal SGST { get; set; }
        public decimal IGST { get; set; }
        public decimal Total { get; set; }
    }

    public class RepairJobsReport
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Status { get; set; }
        public int TotalJobs { get; set; }
        public int CompletedJobs { get; set; }
        public int PendingJobs { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<RepairJobDetail> JobDetails { get; set; } = new List<RepairJobDetail>();
    }

    public class RepairJobDetail
    {
        public int RepairID { get; set; }
        public string CustomerName { get; set; }
        public string ItemDescription { get; set; }
        public DateTime ReceiptDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string Status { get; set; }
        public decimal EstimatedCost { get; set; }
        public decimal FinalAmount { get; set; }
        public string WorkType { get; set; }
    }

    public class CustomerReport
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalCustomers { get; set; }
        public decimal TotalPurchases { get; set; }
        public List<CustomerPurchaseDetail> CustomerDetails { get; set; } = new List<CustomerPurchaseDetail>();
    }

    public class CustomerPurchaseDetail
    {
        public int CustomerID { get; set; }
        public string CustomerName { get; set; }
        public string PhoneNumber { get; set; }
        public decimal TotalPurchases { get; set; }
        public int OrderCount { get; set; }
        public DateTime? LastPurchaseDate { get; set; }
        public int RepairJobCount { get; set; }
        public decimal PendingAmount { get; set; }
        public int LoyaltyPoints { get; set; }
    }
}