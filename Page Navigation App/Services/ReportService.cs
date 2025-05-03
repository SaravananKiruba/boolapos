using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Page_Navigation_App.Services
{
    public class ReportService
    {
        private readonly AppDbContext _context;

        public ReportService(AppDbContext context)
        {
            _context = context;
        }

        // Dashboard Overview
        public async Task<Dictionary<string, object>> GetDashboardMetrics()
        {
            // Get today's date for filtering
            var today = DateTime.Now.Date;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            // Sales metrics
            var totalSales = await _context.Orders
                .Where(o => o.OrderDate.Date >= monthStart && o.OrderDate.Date <= monthEnd)
                .SumAsync(o => o.GrandTotal);
                
            var todaySales = await _context.Orders
                .Where(o => o.OrderDate.Date == today)
                .SumAsync(o => o.GrandTotal);
                
            var totalOrders = await _context.Orders
                .Where(o => o.OrderDate.Date >= monthStart && o.OrderDate.Date <= monthEnd)
                .CountAsync();
                
            var todayOrders = await _context.Orders
                .Where(o => o.OrderDate.Date == today)
                .CountAsync();

            // Inventory metrics
            var totalProducts = await _context.Products.CountAsync();
            var lowStockProducts = await _context.Products
                .Where(p => p.StockQuantity <= 5 && p.StockQuantity > 0)
                .CountAsync();
            var outOfStockProducts = await _context.Products
                .Where(p => p.StockQuantity <= 0)
                .CountAsync();
            var inventoryValue = await _context.Products.SumAsync(p => p.FinalPrice * p.StockQuantity);

            // Customer metrics
            var totalCustomers = await _context.Customers.CountAsync();
            var newCustomersThisMonth = await _context.Customers
                .Where(c => c.RegistrationDate >= monthStart && c.RegistrationDate <= monthEnd)
                .CountAsync();

            return new Dictionary<string, object>
            {
                { "TotalSalesThisMonth", totalSales },
                { "TodaySales", todaySales },
                { "TotalOrdersThisMonth", totalOrders },
                { "TodayOrders", todayOrders },
                { "TotalProducts", totalProducts },
                { "LowStockProducts", lowStockProducts },
                { "OutOfStockProducts", outOfStockProducts },
                { "InventoryValue", inventoryValue },
                { "TotalCustomers", totalCustomers },
                { "NewCustomersThisMonth", newCustomersThisMonth }
            };
        }

        // Sales Reports
        public async Task<List<Order>> GetSalesReport(DateTime startDate, DateTime endDate, 
                                                    string paymentMethod = null,
                                                    int? customerId = null)
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => o.OrderDate.Date >= startDate.Date && o.OrderDate.Date <= endDate.Date);

            if (!string.IsNullOrEmpty(paymentMethod))
                query = query.Where(o => o.PaymentMethod == paymentMethod);

            if (customerId.HasValue)
                query = query.Where(o => o.CustomerID == customerId.Value);

            return await query.OrderByDescending(o => o.OrderDate).ToListAsync();
        }

        // Add missing method for SalesAnalytics needed by ExportService
        public async Task<Dictionary<string, decimal>> GetSalesAnalytics(DateTime startDate, DateTime endDate)
        {
            // Get orders in the date range
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .Where(o => o.OrderDate.Date >= startDate.Date && o.OrderDate.Date <= endDate.Date)
                .ToListAsync();

            // Group by payment method and calculate totals
            var paymentMethodTotals = orders
                .GroupBy(o => o.PaymentMethod)
                .ToDictionary(
                    g => $"Payment_{g.Key}",
                    g => g.Sum(o => o.GrandTotal)
                );

            // Calculate tax totals
            decimal cgstTotal = orders.Sum(o => o.CGST);
            decimal sgstTotal = orders.Sum(o => o.SGST);
            decimal igstTotal = orders.Sum(o => o.IGST);

            // Create complete analytics dictionary
            var analytics = new Dictionary<string, decimal>
            {
                ["Total_Sales"] = orders.Sum(o => o.GrandTotal),
                ["Total_Orders"] = orders.Count,
                ["Average_Order_Value"] = orders.Count > 0 ? orders.Sum(o => o.GrandTotal) / orders.Count : 0,
                ["Total_Discount"] = orders.Sum(o => o.DiscountAmount),
                ["Total_Tax"] = cgstTotal + sgstTotal + igstTotal,
                ["CGST"] = cgstTotal,
                ["SGST"] = sgstTotal,
                ["IGST"] = igstTotal,
                ["Hallmarking_Charges"] = orders.Sum(o => o.HallmarkingCharges)
            };

            // Add payment method totals to analytics
            foreach (var item in paymentMethodTotals)
            {
                analytics[item.Key] = item.Value;
            }

            return analytics;
        }

        // Add missing method for FinancialReports needed by ExportService
        public async Task<Dictionary<string, decimal>> GetFinancialReports(DateTime startDate, DateTime endDate)
        {
            // Get orders in the date range for GST calculation
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .Where(o => o.OrderDate.Date >= startDate.Date && o.OrderDate.Date <= endDate.Date)
                .ToListAsync();

            // Get finance entries in the date range
            var finances = await _context.Finances
                .Where(f => f.TransactionDate.Date >= startDate.Date && f.TransactionDate.Date <= endDate.Date)
                .ToListAsync();

            // Calculate GST collected
            decimal cgstCollected = orders.Sum(o => o.CGST);
            decimal sgstCollected = orders.Sum(o => o.SGST);
            decimal igstCollected = orders.Sum(o => o.IGST);

            // Calculate income and expenses from finance entries
            decimal totalIncome = finances.Where(f => f.TransactionType == "Income").Sum(f => f.Amount);
            decimal totalExpenses = finances.Where(f => f.TransactionType == "Expense").Sum(f => f.Amount);
            decimal netProfit = totalIncome - totalExpenses;

            return new Dictionary<string, decimal>
            {
                ["CGST_Collected"] = cgstCollected,
                ["SGST_Collected"] = sgstCollected,
                ["IGST_Collected"] = igstCollected,
                ["Total_GST_Collected"] = cgstCollected + sgstCollected + igstCollected,
                ["Total_Income"] = totalIncome,
                ["Total_Expenses"] = totalExpenses,
                ["Net_Profit"] = netProfit,
                ["Total_Sales"] = orders.Sum(o => o.GrandTotal),
                ["Order_Count"] = orders.Count
            };
        }

        // Add missing method for RepairAnalytics needed by ExportService
        public async Task<List<RepairAnalyticsData>> GetRepairAnalytics(DateTime startDate, DateTime endDate)
        {
            var repairs = await _context.RepairJobs
                .Where(r => r.ReceiptDate.Date >= startDate.Date && r.ReceiptDate.Date <= endDate.Date)
                .ToListAsync();

            // Group by work type
            var workTypeGroups = repairs
                .GroupBy(r => r.WorkType)
                .Select(g => new RepairAnalyticsData
                {
                    WorkType = g.Key,
                    Status = g.Count().ToString(), // Count of repairs in this category
                    EstimatedAmount = g.Sum(r => r.EstimatedCost),
                    FinalAmount = g.Sum(r => r.FinalAmount)
                })
                .ToList();

            return workTypeGroups;
        }

        // Custom class for repair analytics reporting
        public class RepairAnalyticsData
        {
            public string WorkType { get; set; }
            public string Status { get; set; } // Used to store count as string
            public decimal EstimatedAmount { get; set; }
            public decimal FinalAmount { get; set; }
        }

        // Customer Purchase History
        public async Task<List<Order>> GetCustomerPurchaseHistory(int customerId)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => o.CustomerID == customerId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        // GST Reports
        public async Task<List<GST_Report>> GetGSTReport(DateTime startDate, DateTime endDate)
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.OrderDate.Date >= startDate.Date && o.OrderDate.Date <= endDate.Date)
                .OrderBy(o => o.OrderDate)
                .ToListAsync();

            // Group by month for monthly reporting
            return orders
                .GroupBy(o => new { Month = o.OrderDate.Month, Year = o.OrderDate.Year })
                .Select(g => new GST_Report
                {
                    Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                    TaxableAmount = g.Sum(o => o.TotalAmount),
                    CGST = g.Sum(o => o.CGST),
                    SGST = g.Sum(o => o.SGST),
                    IGST = g.Sum(o => o.IGST),
                    TotalTax = g.Sum(o => o.CGST + o.SGST + o.IGST),
                    InvoiceCount = g.Count(),
                    TotalInvoiceValue = g.Sum(o => o.GrandTotal)
                })
                .OrderBy(r => r.Period)
                .ToList();
        }

        // Helper class for GST reporting
        public class GST_Report
        {
            public string Period { get; set; }
            public decimal TaxableAmount { get; set; }
            public decimal CGST { get; set; }
            public decimal SGST { get; set; }
            public decimal? IGST { get; set; }
            public decimal TotalTax { get; set; }
            public int InvoiceCount { get; set; }
            public decimal TotalInvoiceValue { get; set; }
        }
    }
}