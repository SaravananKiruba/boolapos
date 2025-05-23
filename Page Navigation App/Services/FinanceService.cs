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
    /// Service to manage financial transactions, payments and EMI handling
    /// </summary>
    public class FinanceService
    {
        private readonly AppDbContext _context;
        private readonly LogService _logService;

        public FinanceService(AppDbContext context, LogService logService)
        {
            _context = context;
            _logService = logService;
        }

        /// <summary>
        /// Record a payment for an order
        /// </summary>
        public async Task<bool> RecordPaymentAsync(Finance payment)
        {
            try
            {
                // Set default transaction date if not provided
                if (payment.TransactionDate == default)
                {
                    payment.TransactionDate = DateTime.Now;
                }

                // Validate payment
                if (payment.Amount <= 0)
                {
                    await _logService.LogErrorAsync("Invalid payment amount: Amount must be greater than zero");
                    return false;
                }

                // Get the order if OrderID is provided
                if (payment.OrderID.HasValue)
                {
                    var order = await _context.Orders.FindAsync(payment.OrderID.Value);
                    if (order == null)
                    {
                        await _logService.LogErrorAsync($"Order not found with ID: {payment.OrderID.Value}");
                        return false;
                    }

                    // Calculate remaining amount to be paid
                    var existingPayments = await _context.Finances
                        .Where(f => f.OrderID == payment.OrderID.Value && f.IsPaymentReceived)
                        .SumAsync(f => f.Amount);

                    decimal remainingAmount = order.GrandTotal - existingPayments;

                    // Validate payment amount
                    if (payment.Amount > remainingAmount)
                    {
                        await _logService.LogWarningAsync($"Payment amount ({payment.Amount}) exceeds remaining balance ({remainingAmount}) for order {payment.OrderID.Value}");
                        payment.Notes = $"{payment.Notes} | Warning: Payment exceeds the remaining balance.";
                    }
                }

                // Add payment record
                await _context.Finances.AddAsync(payment);
                await _context.SaveChangesAsync();

                // Update customer account if CustomerID is provided
                if (payment.CustomerID.HasValue)
                {
                    await UpdateCustomerBalanceAsync(payment.CustomerID.Value);
                }

                await _logService.LogInformationAsync($"Payment recorded: {payment.Amount} via {payment.PaymentMode}");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error recording payment: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Update a customer's balance based on orders and payments
        /// </summary>
        public async Task<bool> UpdateCustomerBalanceAsync(int customerId)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(customerId);
                if (customer == null)
                {
                    await _logService.LogErrorAsync($"Customer not found with ID: {customerId}");
                    return false;
                }

                // Calculate total orders amount
                var totalOrders = await _context.Orders
                    .Where(o => o.CustomerID == customerId)
                    .SumAsync(o => o.GrandTotal);

                // Calculate total payments
                var totalPayments = await _context.Finances
                    .Where(f => f.CustomerID == customerId && f.IsPaymentReceived)
                    .SumAsync(f => f.Amount);

                // Update outstanding amount
                customer.OutstandingAmount = totalOrders - totalPayments;
                await _context.SaveChangesAsync();

                await _logService.LogInformationAsync($"Customer balance updated for ID {customerId}: Outstanding amount = {customer.OutstandingAmount}");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error updating customer balance: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Create an EMI plan for an order
        /// </summary>
        public async Task<bool> CreateEMIPlanAsync(int orderId, int customerId, int numberOfMonths, DateTime startDate)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    await _logService.LogErrorAsync($"Order not found with ID: {orderId}");
                    return false;
                }

                var customer = await _context.Customers.FindAsync(customerId);
                if (customer == null)
                {
                    await _logService.LogErrorAsync($"Customer not found with ID: {customerId}");
                    return false;
                }

                // Calculate EMI amount
                decimal emiAmount = Math.Round(order.GrandTotal / numberOfMonths, 2);
                decimal lastEmiAmount = order.GrandTotal - (emiAmount * (numberOfMonths - 1)); // Adjust last EMI for rounding

                // Create EMI records
                for (int i = 0; i < numberOfMonths; i++)
                {
                    DateTime dueDate = startDate.AddMonths(i);
                    decimal amount = (i == numberOfMonths - 1) ? lastEmiAmount : emiAmount;

                    var emi = new EMI
                    {
                        OrderID = orderId,
                        CustomerID = customerId,
                        EMINumber = i + 1,
                        Amount = amount,
                        DueDate = dueDate,
                        Status = "Pending",
                        CreatedDate = DateTime.Now
                    };

                    await _context.EMIs.AddAsync(emi);
                }

                // Update order with EMI information
                order.EMIMonths = numberOfMonths;
                order.EMIAmount = emiAmount;
                order.PaymentMethod = order.PaymentMethod + " (EMI)";

                await _context.SaveChangesAsync();
                await _logService.LogInformationAsync($"EMI plan created for order {orderId}: {numberOfMonths} months, {emiAmount} per month");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error creating EMI plan: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Record an EMI payment
        /// </summary>
        public async Task<bool> RecordEMIPaymentAsync(int emiId, decimal amount, string paymentMode, string referenceNumber = null)
        {
            try
            {
                var emi = await _context.EMIs.FindAsync(emiId);
                if (emi == null)
                {
                    await _logService.LogErrorAsync($"EMI not found with ID: {emiId}");
                    return false;
                }

                if (emi.Status == "Paid")
                {
                    await _logService.LogErrorAsync($"EMI {emiId} is already paid");
                    return false;
                }

                // Validate payment amount
                if (amount < emi.Amount)
                {
                    await _logService.LogWarningAsync($"Partial payment: {amount} is less than EMI amount {emi.Amount}");
                }

                // Record payment
                var payment = new Finance
                {
                    CustomerID = emi.CustomerID,
                    OrderID = emi.OrderID,
                    Amount = amount,
                    PaymentMode = paymentMode,
                    TransactionDate = DateTime.Now,
                    ReferenceNumber = referenceNumber,
                    IsPaymentReceived = true,
                    Notes = $"EMI Payment #{emi.EMINumber}"
                };

                await _context.Finances.AddAsync(payment);

                // Update EMI status
                emi.Status = amount >= emi.Amount ? "Paid" : "Partial";
                emi.PaidAmount = amount;
                emi.PaymentDate = DateTime.Now;

                await _context.SaveChangesAsync();
                await UpdateCustomerBalanceAsync(emi.CustomerID);

                await _logService.LogInformationAsync($"EMI payment recorded for EMI {emiId}: {amount}");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error recording EMI payment: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get overdue EMIs for notifications
        /// </summary>
        public async Task<List<EMI>> GetOverdueEMIsAsync()
        {
            try
            {
                return await _context.EMIs
                    .Include(e => e.Customer)
                    .Include(e => e.Order)
                    .Where(e => e.Status != "Paid" && e.DueDate < DateTime.Now)
                    .OrderBy(e => e.DueDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error getting overdue EMIs: {ex.Message}");
                return new List<EMI>();
            }
        }

        /// <summary>
        /// Get upcoming EMIs due in the next specified days
        /// </summary>
        public async Task<List<EMI>> GetUpcomingEMIsAsync(int daysAhead = 7)
        {
            try
            {
                DateTime today = DateTime.Now.Date;
                DateTime endDate = today.AddDays(daysAhead);

                return await _context.EMIs
                    .Include(e => e.Customer)
                    .Include(e => e.Order)
                    .Where(e => e.Status != "Paid" && 
                                e.DueDate >= today && 
                                e.DueDate <= endDate)
                    .OrderBy(e => e.DueDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error getting upcoming EMIs: {ex.Message}");
                return new List<EMI>();
            }
        }

        /// <summary>
        /// Record expenses (such as store expenses, salaries, etc.)
        /// </summary>
        public async Task<bool> RecordExpenseAsync(
            string category, 
            decimal amount, 
            string description,
            string paymentMode, 
            string referenceNumber = null)
        {
            try
            {
                if (amount <= 0)
                {
                    await _logService.LogErrorAsync("Invalid expense amount: Amount must be greater than zero");
                    return false;
                }

                var expense = new Finance
                {
                    Category = category,
                    Amount = amount,
                    Notes = description,
                    PaymentMode = paymentMode,
                    TransactionDate = DateTime.Now,
                    ReferenceNumber = referenceNumber,
                    IsPaymentReceived = false, // This is an outgoing payment
                    TransactionType = "Expense"
                };

                await _context.Finances.AddAsync(expense);
                await _context.SaveChangesAsync();

                await _logService.LogInformationAsync($"Expense recorded: {amount} for {category} - {description}");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error recording expense: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get daily sales and payment summary for a date range
        /// </summary>
        public async Task<List<DailySummary>> GetDailyFinanceSummaryAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                // Convert to date only
                fromDate = fromDate.Date;
                toDate = toDate.Date.AddDays(1).AddSeconds(-1); // Include all of toDate

                // Get sales grouped by date
                var sales = await _context.Orders
                    .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate)
                    .GroupBy(o => o.OrderDate.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        TotalSales = g.Sum(o => o.GrandTotal),
                        OrderCount = g.Count()
                    })
                    .ToListAsync();

                // Get payments grouped by date
                var payments = await _context.Finances
                    .Where(f => f.TransactionDate >= fromDate && 
                                f.TransactionDate <= toDate && 
                                f.IsPaymentReceived)
                    .GroupBy(f => f.TransactionDate.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        TotalPayments = g.Sum(f => f.Amount),
                        PaymentCount = g.Count()
                    })
                    .ToListAsync();

                // Get expenses grouped by date
                var expenses = await _context.Finances
                    .Where(f => f.TransactionDate >= fromDate && 
                                f.TransactionDate <= toDate && 
                                !f.IsPaymentReceived)
                    .GroupBy(f => f.TransactionDate.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        TotalExpenses = g.Sum(f => f.Amount),
                        ExpenseCount = g.Count()
                    })
                    .ToListAsync();

                // Create a list of all dates in the range
                var allDates = new List<DateTime>();
                for (var date = fromDate; date <= toDate; date = date.AddDays(1))
                {
                    allDates.Add(date.Date);
                }

                // Create the unified summary
                var summary = allDates.Select(date => new DailySummary
                {
                    Date = date,
                    TotalSales = sales.FirstOrDefault(s => s.Date == date)?.TotalSales ?? 0,
                    OrderCount = sales.FirstOrDefault(s => s.Date == date)?.OrderCount ?? 0,
                    TotalPayments = payments.FirstOrDefault(p => p.Date == date)?.TotalPayments ?? 0,
                    PaymentCount = payments.FirstOrDefault(p => p.Date == date)?.PaymentCount ?? 0,
                    TotalExpenses = expenses.FirstOrDefault(e => e.Date == date)?.TotalExpenses ?? 0,
                    ExpenseCount = expenses.FirstOrDefault(e => e.Date == date)?.ExpenseCount ?? 0
                }).ToList();

                return summary;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error getting finance summary: {ex.Message}");
                return new List<DailySummary>();
            }
        }

        /// <summary>
        /// Calculate profit and loss for a date range
        /// </summary>
        public async Task<ProfitLossReport> GetProfitLossReportAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                // Convert to date only
                fromDate = fromDate.Date;
                toDate = toDate.Date.AddDays(1).AddSeconds(-1); // Include all of toDate

                // Get total sales
                var totalSales = await _context.Orders
                    .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate)
                    .SumAsync(o => o.GrandTotal);

                // Get total expenses
                var totalExpenses = await _context.Finances
                    .Where(f => f.TransactionDate >= fromDate && 
                                f.TransactionDate <= toDate && 
                                !f.IsPaymentReceived)
                    .SumAsync(f => f.Amount);

                // Get sales by category
                var salesByCategory = await _context.OrderDetails
                    .Include(od => od.Order)
                    .Include(od => od.Product)
                    .ThenInclude(p => p.Category)
                    .Where(od => od.Order.OrderDate >= fromDate && od.Order.OrderDate <= toDate)
                    .GroupBy(od => od.Product.Category.CategoryName)
                    .Select(g => new CategorySalesReport
                    {
                        CategoryName = g.Key,
                        SalesAmount = g.Sum(od => od.TotalPrice)
                    })
                    .ToListAsync();

                // Get expenses by category
                var expensesByCategory = await _context.Finances
                    .Where(f => f.TransactionDate >= fromDate && 
                                f.TransactionDate <= toDate && 
                                !f.IsPaymentReceived)
                    .GroupBy(f => f.Category)
                    .Select(g => new CategoryExpenseReport
                    {
                        CategoryName = g.Key,
                        ExpenseAmount = g.Sum(f => f.Amount)
                    })
                    .ToListAsync();

                return new ProfitLossReport
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    TotalSales = totalSales,
                    TotalExpenses = totalExpenses,
                    GrossProfit = totalSales - totalExpenses,
                    SalesByCategory = salesByCategory,
                    ExpensesByCategory = expensesByCategory
                };
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error getting profit/loss report: {ex.Message}");
                return new ProfitLossReport
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    TotalSales = 0,
                    TotalExpenses = 0,
                    GrossProfit = 0,
                    SalesByCategory = new List<CategorySalesReport>(),
                    ExpensesByCategory = new List<CategoryExpenseReport>()
                };
            }
        }

        /// <summary>
        /// Get all finance records
        /// </summary>
        public List<Finance> GetAllFinanceRecords()
        {
            try
            {
                return _context.Finances
                    .OrderByDescending(f => f.TransactionDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error getting all finance records: {ex.Message}");
                return new List<Finance>();
            }
        }

        /// <summary>
        /// Get customer dues
        /// </summary>
        public List<Customer> GetCustomerDues()
        {
            try
            {
                return _context.Customers
                    .Where(c => c.OutstandingAmount > 0)
                    .OrderByDescending(c => c.OutstandingAmount)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error getting customer dues: {ex.Message}");
                return new List<Customer>();
            }
        }

        /// <summary>
        /// Get customer dues for a specific customer
        /// </summary>
        public async Task<decimal> GetCustomerDues(int customerId)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(customerId);
                if (customer == null)
                    return 0;
                    
                return customer.OutstandingAmount;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error getting customer dues for ID {customerId}: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Add finance record (synchronous version)
        /// </summary>
        public bool AddFinanceRecord(Finance finance)
        {
            // Call the async version and get the result synchronously
            return RecordPaymentAsync(finance).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Update finance record
        /// </summary>
        public bool UpdateFinanceRecord(Finance finance)
        {
            try
            {
                var existingRecord = _context.Finances.Find(finance.FinanceID);
                if (existingRecord == null)
                {
                    _logService.LogError($"Finance record not found with ID: {finance.FinanceID}");
                    return false;
                }

                existingRecord.Amount = finance.Amount;
                existingRecord.PaymentMode = finance.PaymentMode;
                existingRecord.Category = finance.Category;
                existingRecord.Notes = finance.Notes;
                existingRecord.ReferenceNumber = finance.ReferenceNumber;
                existingRecord.TransactionType = finance.TransactionType;
                
                // Other properties that might need updating
                existingRecord.IsPaymentReceived = finance.IsPaymentReceived;
                
                _context.SaveChanges();
                
                // Update customer balance if CustomerID is provided
                if (existingRecord.CustomerID.HasValue)
                {
                    UpdateCustomerBalanceAsync(existingRecord.CustomerID.Value).GetAwaiter().GetResult();
                }
                
                _logService.LogInformation($"Finance record updated: {finance.FinanceID}");
                return true;
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error updating finance record: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get transactions by date range
        /// </summary>
        public List<Finance> GetTransactionsByDateRange(DateTime fromDate, DateTime toDate)
        {
            try
            {
                return _context.Finances
                    .Where(f => f.TransactionDate >= fromDate && f.TransactionDate <= toDate)
                    .OrderByDescending(f => f.TransactionDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error getting transactions by date range: {ex.Message}");
                return new List<Finance>();
            }
        }

        /// <summary>
        /// Get transactions by type
        /// </summary>
        public List<Finance> GetTransactionsByType(string transactionType)
        {
            try
            {
                return _context.Finances
                    .Where(f => f.TransactionType == transactionType)
                    .OrderByDescending(f => f.TransactionDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error getting transactions by type: {ex.Message}");
                return new List<Finance>();
            }
        }
    }

    // Financial report data classes
    public class DailySummary
    {
        public DateTime Date { get; set; }
        public decimal TotalSales { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalPayments { get; set; }
        public int PaymentCount { get; set; }
        public decimal TotalExpenses { get; set; }
        public int ExpenseCount { get; set; }
        public decimal NetCashFlow => TotalPayments - TotalExpenses;
        public string DateFormatted => Date.ToString("dd-MMM-yyyy");
    }

    public class ProfitLossReport
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal GrossProfit { get; set; }
        public List<CategorySalesReport> SalesByCategory { get; set; } = new List<CategorySalesReport>();
        public List<CategoryExpenseReport> ExpensesByCategory { get; set; } = new List<CategoryExpenseReport>();
    }

    public class CategorySalesReport
    {
        public string CategoryName { get; set; }
        public decimal SalesAmount { get; set; }
    }

    public class CategoryExpenseReport
    {
        public string CategoryName { get; set; }
        public decimal ExpenseAmount { get; set; }
    }
}