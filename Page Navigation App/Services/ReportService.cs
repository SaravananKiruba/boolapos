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
        public async Task<DashboardData> GetDashboardDataAsync(DateOnly fromDate, DateOnly toDate)
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

              

                return new DashboardData
                {
                    TotalSales = orders.Sum(o => o.GrandTotal),
                    OrderCount = orders.Count,
                    NewCustomers = customers.Count,
                    LowStockCount = products.Count,
                    TopSellingProducts = await GetTopSellingProductsAsync(fromDate, toDate, 5),
                    MonthlySalesData = await GetMonthlySalesAsync(fromDate, toDate),
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
        private async Task<decimal> GetMetalSalesAsync(DateOnly fromDate, DateOnly toDate, string metalType)
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
            DateOnly fromDate, 
            DateOnly toDate, 
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

        private async Task<List<MonthlySales>> GetMonthlySalesAsync(DateOnly fromDate, DateOnly toDate)
        {
            // Normalize fromDate and toDate to DateTime
            var fromDateTime = new DateTime(fromDate.Year, fromDate.Month, 1);
            var toDateTime = new DateTime(toDate.Year, toDate.Month, 1).AddMonths(1).AddDays(-1); // End of 'toDate' month

            // Ensure the range includes at least one full month
            if (toDateTime < fromDateTime.AddMonths(1).AddDays(-1))
            {
                toDateTime = fromDateTime.AddMonths(1).AddDays(-1);
            }

            var monthlySales = new List<MonthlySales>();
            var currentDate = fromDateTime;

            while (currentDate <= toDateTime)
            {
                var monthStart = new DateTime(currentDate.Year, currentDate.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                var totalSales = await _context.Orders
                    .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate)
                    .SumAsync(o => (decimal?)o.GrandTotal) ?? 0;

                monthlySales.Add(new MonthlySales
                {
                    Month = monthStart.ToString("MMM yyyy"),
                    Amount = totalSales
                });

                currentDate = currentDate.AddMonths(1);
            }

            return monthlySales;
        }


      
        public async Task<InventoryReport> GenerateInventoryReportAsync()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Supplier)
                    .ToListAsync();
                
                var goldProducts = products.Where(p => p.MetalType.Contains("Gold")).ToList();
                var silverProducts = products.Where(p => p.MetalType.Contains("Silver")).ToList();
                
                return new InventoryReport
                {
                    TotalProducts = products.Count,
                    TotalValue = products.Sum(p => p.ProductPrice * p.StockQuantity),
                    GoldItems = goldProducts.Count,
                    GoldWeight = goldProducts.Sum(p => p.GrossWeight * p.StockQuantity),
                    GoldValue = goldProducts.Sum(p => p.ProductPrice * p.StockQuantity),
                    SilverItems = silverProducts.Count,
                    SilverWeight = silverProducts.Sum(p => p.GrossWeight * p.StockQuantity),
                    SilverValue = silverProducts.Sum(p => p.ProductPrice * p.StockQuantity),
                    LowStockItems = products.Count(p => p.StockQuantity <= p.ReorderLevel),
                    ProductDetails = products.Select(p => new ProductReportItem
                    {
                        ProductID = p.ProductID,
                        ProductName = p.ProductName,
                        MetalType = p.MetalType,
                        Purity = p.Purity,
                        GrossWeight = p.GrossWeight,
                        NetWeight = p.NetWeight,
                        StockQuantity = p.StockQuantity,
                        Value = p.ProductPrice * p.StockQuantity,
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
        public async Task<GSTReport> GenerateGSTReportAsync(DateOnly fromDate, DateOnly toDate)
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
                    TotalCGST = 0, // No longer tracking CGST separately
                    TotalSGST = 0, // No longer tracking SGST separately
                    TotalIGST = 0, // No longer tracking IGST separately                    TotalTax = orders.Sum(o => o.GrandTotal - o.PriceBeforeTax), // Total tax is the difference
                    B2BSales = 0, // Not tracking GST numbers anymore
                    B2CSales = orders.Sum(o => o.TotalAmount), // All sales are B2C now
                    InvoiceDetails = orders.Select(o => new GSTInvoiceDetail
                    {
                        InvoiceNumber = o.InvoiceNumber,
                        InvoiceDate = o.OrderDate,
                        CustomerName = o.Customer.CustomerName,
                        CustomerGST = "", // No longer tracking GST numbers
                        HSNCode = "7113", // Default HSN code for jewelry
                        TaxableValue = o.PriceBeforeTax,
                        CGST = 0, // No longer tracking separate components
                        SGST = 0, // No longer tracking separate components
                        IGST = o.GrandTotal - o.PriceBeforeTax, // Total tax amount
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
        /// Generate customer purchase report
        /// </summary>
        public async Task<CustomerReport> GenerateCustomerReportAsync(
            DateOnly fromDate, 
            DateOnly toDate, 
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
                   
                      // Calculate pending amount from orders and payments
                    var totalOrders = orders.Sum(o => o.GrandTotal);
                    var totalPayments = _context.Finances
                        .Where(f => f.CustomerID == customer.CustomerID && f.IsPaymentReceived)
                        .Sum(f => f.Amount);
                    var pendingAmount = totalOrders - totalPayments;
                    
                    customerReports.Add(new CustomerPurchaseDetail
                    {
                        CustomerID = customer.CustomerID,
                        CustomerName = customer.CustomerName,
                        PhoneNumber = customer.PhoneNumber,
                        TotalPurchases = orders.Sum(o => o.GrandTotal),
                        OrderCount = orders.Count,
                        LastPurchaseDate = orders.Any() ? orders.Max(o => o.OrderDate) : (DateOnly?)null,
                        PendingAmount = pendingAmount,
                        LoyaltyPoints = 0 // LoyaltyPoints property removed from Customer model
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
        public async Task<DashboardData> GetSalesAnalyticsAsync(DateOnly fromDate, DateOnly toDate)
        {
            return await GetDashboardDataAsync(fromDate, toDate);
        }

        /// <summary>
        /// Get sales analytics data (synchronous version)
        /// </summary>
        public DashboardData GetSalesAnalytics(DateOnly fromDate, DateOnly toDate)
        {
            return GetSalesAnalyticsAsync(fromDate, toDate).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Get financial reports
        /// </summary>
        public ProfitLossReport GetFinancialReports(DateOnly fromDate, DateOnly toDate)
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

       
    }

    // Report data classes
    public class DashboardData
    {
        public decimal TotalSales { get; set; }
        public int OrderCount { get; set; }
        public int NewCustomers { get; set; }
        public int LowStockCount { get; set; }       
        public List<TopSellingProduct> TopSellingProducts { get; set; } = new List<TopSellingProduct>();
        public List<MonthlySales> MonthlySalesData { get; set; } = new List<MonthlySales>();
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
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
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
        public DateOnly InvoiceDate { get; set; }
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
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
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
        public DateOnly ReceiptDate { get; set; }
        public DateOnly DeliveryDate { get; set; }
        public string Status { get; set; }
        public decimal EstimatedCost { get; set; }
        public decimal FinalAmount { get; set; }
        public string WorkType { get; set; }
    }

    public class CustomerReport
    {
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public int TotalCustomers { get; set; }
        public decimal TotalPurchases { get; set; }
        public List<CustomerPurchaseDetail> CustomerDetails { get; set; } = new List<CustomerPurchaseDetail>();
    }    public class CustomerPurchaseDetail
    {
        public int CustomerID { get; set; }
        public string CustomerName { get; set; }
        public string PhoneNumber { get; set; }
        public decimal TotalPurchases { get; set; }
        public int OrderCount { get; set; }
        public DateOnly? LastPurchaseDate { get; set; }
        public decimal PendingAmount { get; set; }
        // LoyaltyPoints system has been removed
        public int LoyaltyPoints { get; set; } = 0; // Default to 0
    }
}