using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Page_Navigation_App.Services
{
    public class ReportService
    {
        private readonly AppDbContext _context;
        private readonly StockService _stockService;
        private readonly RateMasterService _rateService;

        public ReportService(
            AppDbContext context,
            StockService stockService,
            RateMasterService rateService)
        {
            _context = context;
            _stockService = stockService;
            _rateService = rateService;
        }

        public async Task<Dictionary<string, decimal>> GetDashboardSummary()
        {
            var today = DateTime.Now.Date;
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .Where(o => o.OrderDate >= thisMonth)
                .ToListAsync();

            var repairs = await _context.RepairJobs
                .Where(r => r.ReceiptDate >= thisMonth)
                .ToListAsync();

            var stockValue = await _stockService.GetTotalStockValue();
            var deadStock = await _stockService.GetDeadStock();

            return new Dictionary<string, decimal>
            {
                { "MonthlyRevenue", orders.Sum(o => o.GrandTotal) },
                { "RepairRevenue", repairs.Sum(r => r.FinalAmount) },
                { "TotalStockValue", stockValue },
                { "DeadStockValue", deadStock.Sum(s => s.Quantity * s.Product.BasePrice) },
                { "OrderCount", orders.Count },
                { "RepairCount", repairs.Count }
            };
        }

        public async Task<Dictionary<string, decimal>> GetStockAnalytics()
        {
            var products = await _context.Products
                .Include(p => p.Stocks)
                .ToListAsync();

            var metalTypes = new[] { "Gold", "Silver", "Platinum" };
            var result = new Dictionary<string, decimal>();

            foreach (var metalType in metalTypes)
            {
                var metalProducts = products.Where(p => p.MetalType == metalType);
                result[$"{metalType}Weight"] = metalProducts.Sum(p => 
                    p.Stocks.Sum(s => s.Quantity * p.NetWeight));
                result[$"{metalType}Value"] = metalProducts.Sum(p => 
                    p.Stocks.Sum(s => s.Quantity * p.BasePrice));
            }

            return result;
        }

        public async Task<Dictionary<string, decimal>> GetSalesAnalytics(
            DateTime startDate,
            DateTime endDate)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .ToListAsync();

            return new Dictionary<string, decimal>
            {
                { "TotalSales", orders.Sum(o => o.GrandTotal) },
                { "RetailSales", orders.Where(o => o.OrderType == "Retail").Sum(o => o.GrandTotal) },
                { "WholesaleSales", orders.Where(o => o.OrderType == "Wholesale").Sum(o => o.GrandTotal) },
                { "ExchangeSales", orders.Where(o => o.HasMetalExchange).Sum(o => o.GrandTotal) },
                { "EMISales", orders.Where(o => o.PaymentType == "EMI").Sum(o => o.GrandTotal) },
                { "GoldSales", orders.Sum(o => o.OrderDetails
                    .Where(od => od.Product.MetalType == "Gold")
                    .Sum(od => od.FinalAmount)) },
                { "SilverSales", orders.Sum(o => o.OrderDetails
                    .Where(od => od.Product.MetalType == "Silver")
                    .Sum(od => od.FinalAmount)) },
                { "PlatinumSales", orders.Sum(o => o.OrderDetails
                    .Where(od => od.Product.MetalType == "Platinum")
                    .Sum(od => od.FinalAmount)) }
            };
        }

        public async Task<IEnumerable<RepairJob>> GetRepairAnalytics(
            DateTime startDate,
            DateTime endDate)
        {
            return await _context.RepairJobs
                .Include(r => r.Customer)
                .Where(r => r.ReceiptDate >= startDate && r.ReceiptDate <= endDate)
                .GroupBy(r => r.WorkType)
                .Select(g => new RepairJob
                {
                    WorkType = g.Key,
                    EstimatedCost = g.Sum(r => r.EstimatedCost), // Changed to EstimatedCost which is the actual property name
                    FinalAmount = g.Sum(r => r.FinalAmount),
                    Notes = g.Count().ToString() // Using Notes instead of Status to store count
                })
                .ToListAsync();
        }

        public async Task<Dictionary<string, decimal>> GetFinancialReports(
            DateTime startDate,
            DateTime endDate)
        {
            var transactions = await _context.Finances
                .Where(f => f.TransactionDate >= startDate && 
                           f.TransactionDate <= endDate)
                .ToListAsync();

            var result = new Dictionary<string, decimal>();

            // Income breakdown
            var incomeTransactions = transactions
                .Where(t => t.TransactionType == "Income")
                .GroupBy(t => t.PaymentMode);

            foreach (var group in incomeTransactions)
            {
                result[$"Income_{group.Key}"] = group.Sum(t => t.Amount);
            }

            // Expense breakdown
            var expenseTransactions = transactions
                .Where(t => t.TransactionType == "Expense")
                .GroupBy(t => t.Category);

            foreach (var group in expenseTransactions)
            {
                result[$"Expense_{group.Key}"] = group.Sum(t => t.Amount);
            }

            // GST Summary
            result["CGST_Collected"] = transactions.Sum(t => t.CGSTAmount ?? 0);
            result["SGST_Collected"] = transactions.Sum(t => t.SGSTAmount ?? 0);
            result["IGST_Collected"] = transactions.Sum(t => t.IGSTAmount ?? 0);

            // Totals
            result["TotalIncome"] = transactions
                .Where(t => t.TransactionType == "Income")
                .Sum(t => t.Amount);
            result["TotalExpense"] = transactions
                .Where(t => t.TransactionType == "Expense")
                .Sum(t => t.Amount);
            result["NetProfit"] = result["TotalIncome"] - result["TotalExpense"];

            return result;
        }

        public async Task<Dictionary<string, int>> GetCustomerAnalytics(
            DateTime startDate,
            DateTime endDate)
        {
            var customers = await _context.Customers
                .Include(c => c.Orders)
                .Include(c => c.RepairJobs)
                .ToListAsync();

            var newCustomers = customers
                .Count(c => c.Orders.Any(o => o.OrderDate >= startDate && 
                                            o.OrderDate <= endDate) ||
                           c.RepairJobs.Any(r => r.ReceiptDate >= startDate && 
                                               r.ReceiptDate <= endDate));

            var repeatCustomers = customers
                .Count(c => c.Orders.Count(o => o.OrderDate >= startDate && 
                                              o.OrderDate <= endDate) > 1 ||
                           c.RepairJobs.Count(r => r.ReceiptDate >= startDate && 
                                                 r.ReceiptDate <= endDate) > 1);

            return new Dictionary<string, int>
            {
                { "TotalCustomers", customers.Count },
                { "NewCustomers", newCustomers },
                { "RepeatCustomers", repeatCustomers },
                { "RetailCustomers", customers.Count(c => c.Orders.Any(o => o.OrderType == "Retail")) },
                { "WholesaleCustomers", customers.Count(c => c.Orders.Any(o => o.OrderType == "Wholesale")) },
                { "RepairCustomers", customers.Count(c => c.RepairJobs.Any()) }
            };
        }

        public async Task<Dictionary<string, decimal>> GetMetalWiseAnalytics(
            DateTime startDate,
            DateTime endDate)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .ToListAsync();

            var result = new Dictionary<string, decimal>();

            // Metal-wise weight sold
            var metalWeights = orders
                .SelectMany(o => o.OrderDetails)
                .GroupBy(od => od.Product.MetalType)
                .ToDictionary(
                    g => $"{g.Key}WeightSold",
                    g => g.Sum(od => od.NetWeight)
                );

            foreach (var kv in metalWeights)
            {
                result[kv.Key] = kv.Value;
            }

            // Metal-wise revenue
            var metalRevenue = orders
                .SelectMany(o => o.OrderDetails)
                .GroupBy(od => od.Product.MetalType)
                .ToDictionary(
                    g => $"{g.Key}Revenue",
                    g => g.Sum(od => od.FinalAmount)
                );

            foreach (var kv in metalRevenue)
            {
                result[kv.Key] = kv.Value;
            }

            // Exchange metal received
            var exchangeMetals = orders
                .Where(o => o.HasMetalExchange)
                .GroupBy(o => o.ExchangeMetalType)
                .ToDictionary(
                    g => $"{g.Key}ExchangeReceived",
                    g => g.Sum(o => o.ExchangeMetalWeight)
                );

            foreach (var kv in exchangeMetals)
            {
                result[kv.Key] = kv.Value;
            }

            return result;
        }

        public async Task<Dictionary<string, decimal>> GetPurityWiseAnalytics(
            string metalType,
            DateTime startDate,
            DateTime endDate)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o => o.OrderDate >= startDate && 
                           o.OrderDate <= endDate &&
                           o.OrderDetails.Any(od => od.Product.MetalType == metalType))
                .ToListAsync();

            var orderDetails = orders
                .SelectMany(o => o.OrderDetails)
                .Where(od => od.Product.MetalType == metalType);

            return orderDetails
                .GroupBy(od => od.Product.Purity)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(od => od.NetWeight)
                );
        }

        public async Task<Dictionary<string, decimal>> GetMakingChargesAnalytics(
            DateTime startDate,
            DateTime endDate)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.Category)
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .ToListAsync();

            var result = new Dictionary<string, decimal>();

            // Category-wise making charges collected
            var categoryMakingCharges = orders
                .SelectMany(o => o.OrderDetails)
                .GroupBy(od => od.Product.Category.CategoryName)
                .ToDictionary(
                    g => $"Making_{g.Key}",
                    g => g.Sum(od => od.MakingCharges)
                );

            foreach (var kv in categoryMakingCharges)
            {
                result[kv.Key] = kv.Value;
            }

            // Metal-wise making charges
            var metalMakingCharges = orders
                .SelectMany(o => o.OrderDetails)
                .GroupBy(od => od.Product.MetalType)
                .ToDictionary(
                    g => $"Making_{g.Key}",
                    g => g.Sum(od => od.MakingCharges)
                );

            foreach (var kv in metalMakingCharges)
            {
                result[kv.Key] = kv.Value;
            }

            return result;
        }

        public async Task<Dictionary<string, decimal>> GetStoneValueAnalytics(
            DateTime startDate,
            DateTime endDate)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.Category)
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .ToListAsync();

            var result = new Dictionary<string, decimal>();

            // Category-wise stone values
            var categoryStoneValues = orders
                .SelectMany(o => o.OrderDetails)
                .GroupBy(od => od.Product.Category.CategoryName)
                .ToDictionary(
                    g => $"Stones_{g.Key}",
                    g => g.Sum(od => od.StoneValue)
                );

            foreach (var kv in categoryStoneValues)
            {
                result[kv.Key] = kv.Value;
            }

            // Total stone weight
            result["TotalStoneWeight"] = orders
                .SelectMany(o => o.OrderDetails)
                .Sum(od => od.Product.StoneWeight);

            // Total stone value
            result["TotalStoneValue"] = orders
                .SelectMany(o => o.OrderDetails)
                .Sum(od => od.StoneValue);

            return result;
        }

        public async Task<Dictionary<string, decimal>> GetRepairTypeAnalytics(
            DateTime startDate,
            DateTime endDate)
        {
            var repairs = await _context.RepairJobs
                .Where(r => r.ReceiptDate >= startDate && r.ReceiptDate <= endDate)
                .ToListAsync();

            // Group by work type
            var byWorkType = repairs
                .GroupBy(r => r.WorkType)
                .ToDictionary(
                    g => $"Repairs_{g.Key}",
                    g => g.Average(r => r.FinalAmount)
                );

            var result = new Dictionary<string, decimal>();

            // Add work type averages
            foreach (var kv in byWorkType)
            {
                result[kv.Key] = kv.Value;
            }

            // Add totals
            result["TotalRepairs"] = repairs.Count;
            result["TotalRepairAmount"] = repairs.Sum(r => r.FinalAmount);
            result["AverageRepairAmount"] = repairs.Average(r => r.FinalAmount);
            result["PendingRepairs"] = repairs.Count(r => r.Status == "Pending");
            result["CompletedRepairs"] = repairs.Count(r => r.Status == "Completed");

            return result;
        }
    }
}