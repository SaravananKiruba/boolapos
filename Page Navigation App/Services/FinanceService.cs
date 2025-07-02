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
                    .SumAsync(f => f.Amount);                // OutstandingAmount property has been removed from Customer model
                // Store the calculated value in Finance records instead
                var outstandingAmount = totalOrders - totalPayments;
                await _context.SaveChangesAsync();

                await _logService.LogInformationAsync($"Customer balance updated for ID {customerId}: Outstanding amount = {outstandingAmount}");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error updating customer balance: {ex.Message}");
                return false;
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
        /// Record expense (synchronous version)
        /// </summary>
        public bool RecordExpense(string category, decimal amount, string description, string paymentMode, string referenceNumber = null)
        {
            return RecordExpenseAsync(category, amount, description, paymentMode, referenceNumber).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Get daily sales and payment summary for a date range
        /// </summary>
        public async Task<List<DailySummary>> GetDailyFinanceSummaryAsync(DateOnly fromDate, DateOnly toDate)
        {
            try
            {
                // Convert DateOnly to DateTime for DB queries
                DateTime fromDateTime = fromDate.ToDateTime(TimeOnly.MinValue);
                DateTime toDateTime = toDate.ToDateTime(TimeOnly.MaxValue); // includes full day of toDate

                // Get sales grouped by date
                var sales = await _context.Orders
                    .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate)
                    .GroupBy(o => o.OrderDate)
                    .Select(g => new
                    {
                        Date = g.Key,
                        TotalSales = g.Sum(o => o.GrandTotal),
                        OrderCount = g.Count()
                    })
                    .ToListAsync();

                // Get payments grouped by date
                var payments = await _context.Finances
                    .Where(f => f.TransactionDate >= fromDateTime &&
                                f.TransactionDate <= toDateTime &&
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
                    .Where(f => f.TransactionDate >= fromDateTime &&
                                f.TransactionDate <= toDateTime &&
                                !f.IsPaymentReceived)
                    .GroupBy(f => f.TransactionDate.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        TotalExpenses = g.Sum(f => f.Amount),
                        ExpenseCount = g.Count()
                    })
                    .ToListAsync();

                // Build the list of all dates in the range
                var allDates = Enumerable.Range(0, (toDate.DayNumber - fromDate.DayNumber) + 1)
                                         .Select(offset => fromDate.AddDays(offset).ToDateTime(TimeOnly.MinValue))
                                         .ToList();

                // Merge the results
                var summary = allDates.Select(date => new DailySummary
                {
                    Date = date,
                    TotalSales = sales.FirstOrDefault(s => s.Date == DateOnly.FromDateTime(date))?.TotalSales ?? 0,
                    OrderCount = sales.FirstOrDefault(s => s.Date == DateOnly.FromDateTime(date))?.OrderCount ?? 0,
                    TotalPayments = payments.FirstOrDefault(p => p.Date == date.Date)?.TotalPayments ?? 0,
                    PaymentCount = payments.FirstOrDefault(p => p.Date == date.Date)?.PaymentCount ?? 0,
                    TotalExpenses = expenses.FirstOrDefault(e => e.Date == date.Date)?.TotalExpenses ?? 0,
                    ExpenseCount = expenses.FirstOrDefault(e => e.Date == date.Date)?.ExpenseCount ?? 0
                }).ToList();

                return summary;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error getting finance summary: {ex.Message}");
                return new List<DailySummary>();
            }
        }


        public async Task<ProfitLossReport> GetProfitLossReportAsync(DateOnly fromDate, DateOnly toDate)
        {
            try
            {
                // Convert DateOnly to full-day DateTime range
                DateTime fromDateTime = fromDate.ToDateTime(TimeOnly.MinValue);
                DateTime toDateTime = toDate.ToDateTime(TimeOnly.MaxValue); // Includes full day of toDate

                // Get total sales
                var totalSales = await _context.Orders
                    .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate)
                    .SumAsync(o => o.GrandTotal);

                // Get total expenses
                var totalExpenses = await _context.Finances
                    .Where(f => f.TransactionDate >= fromDateTime &&
                                f.TransactionDate <= toDateTime &&
                                !f.IsPaymentReceived)
                    .SumAsync(f => f.Amount);

               

               

                return new ProfitLossReport
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    TotalSales = totalSales,
                    TotalExpenses = totalExpenses,
                    GrossProfit = totalSales - totalExpenses,
                    
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
                };
            }
        }


        public List<Finance> GetAllFinanceRecords()
        {
            try
            {
                return _context.Finances
                    .Include(f => f.Customer)
                    .OrderByDescending(f => f.TransactionDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error getting all finance records: {ex.Message}");
                return new List<Finance>();
            }
        }        /// <summary>
        /// Get customer dues
        /// </summary>
        public List<dynamic> GetCustomerDues()
        {
            try
            {
                // Since OutstandingAmount is no longer on Customer model,
                // calculate it from orders and payments
                var customers = _context.Customers.ToList();
                var result = new List<dynamic>();
                
                foreach (var customer in customers)
                {
                    var totalOrders = _context.Orders
                        .Where(o => o.CustomerID == customer.CustomerID)
                        .Sum(o => o.GrandTotal);
                        
                    var totalPayments = _context.Finances
                        .Where(f => f.CustomerID == customer.CustomerID && f.IsPaymentReceived)
                        .Sum(f => f.Amount);
                        
                    var outstandingAmount = totalOrders - totalPayments;
                    
                    if (outstandingAmount > 0)
                    {
                        result.Add(new
                        {
                            Customer = customer,
                            OutstandingAmount = outstandingAmount
                        });
                    }
                }
                
                return result.OrderByDescending(x => x.OutstandingAmount).ToList();
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error getting customer dues: {ex.Message}");
                return new List<dynamic>();
            }
        }

        /// <summary>
        /// Get customer dues for a specific customer
        /// </summary>
        public async Task<decimal> GetCustomerDues(int customerId)
        {
            try
            {
                // Calculate outstanding amount from orders and payments
                var totalOrders = await _context.Orders
                    .Where(o => o.CustomerID == customerId)
                    .SumAsync(o => o.GrandTotal);
                    
                var totalPayments = await _context.Finances
                    .Where(f => f.CustomerID == customerId && f.IsPaymentReceived)
                    .SumAsync(f => f.Amount);
                    
                return totalOrders - totalPayments;
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
            try
            {
                // Ensure FinanceID is set
                if (string.IsNullOrEmpty(finance.FinanceID))
                {
                    finance.FinanceID = Guid.NewGuid().ToString();
                }

                // Set default values if not provided
                if (finance.TransactionDate == default)
                {
                    finance.TransactionDate = DateTime.Now;
                }

                // Set currency to INR if not set
                if (string.IsNullOrEmpty(finance.Currency))
                {
                    finance.Currency = "INR";
                }

                // Set category based on transaction type if not provided
                if (string.IsNullOrEmpty(finance.Category))
                {
                    finance.Category = finance.TransactionType switch
                    {
                        "Income" => "Sales",
                        "Expense" => "Operational",
                        "Refund" => "Customer Service",
                        "Deposit" => "Banking",
                        "Withdrawal" => "Banking",
                        "Transfer" => "Banking",
                        _ => "General"
                    };
                }

                // Set description from Notes if Description is empty
                if (string.IsNullOrEmpty(finance.Description) && !string.IsNullOrEmpty(finance.Notes))
                {
                    finance.Description = finance.Notes;
                }

                // For expense transactions, ensure IsPaymentReceived is false
                if (finance.TransactionType == "Expense" || 
                    finance.TransactionType == "Withdrawal" || 
                    finance.TransactionType == "Refund")
                {
                    finance.IsPaymentReceived = false;
                }
                else
                {
                    finance.IsPaymentReceived = true;
                }

                // Validate the finance record
                if (finance.Amount <= 0)
                {
                    _logService.LogError("Invalid finance amount: Amount must be greater than zero");
                    return false;
                }

                if (string.IsNullOrEmpty(finance.TransactionType))
                {
                    _logService.LogError("Transaction type is required");
                    return false;
                }

                if (string.IsNullOrEmpty(finance.PaymentMode))
                {
                    _logService.LogError("Payment mode is required");
                    return false;
                }

                // Add the finance record to the database
                _context.Finances.Add(finance);
                _context.SaveChanges();

                // Update customer balance if CustomerID is provided
                if (finance.CustomerID.HasValue)
                {
                    UpdateCustomerBalanceAsync(finance.CustomerID.Value).GetAwaiter().GetResult();
                }

                _logService.LogInformation($"Finance record added: {finance.TransactionType} - ₹{finance.Amount} via {finance.PaymentMode}");
                return true;
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error adding finance record: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if a finance record exists by ID
        /// </summary>
        public bool FinanceRecordExists(string financeId)
        {
            try
            {
                if (string.IsNullOrEmpty(financeId))
                {
                    return false;
                }

                return _context.Finances.Any(f => f.FinanceID == financeId);
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error checking if finance record exists: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Update finance record
        /// </summary>
        public bool UpdateFinanceRecord(Finance finance)
        {
            try
            {
                _logService.LogInformation($"Attempting to update finance record with ID: {finance.FinanceID}");
                
                var existingRecord = _context.Finances.FirstOrDefault(f => f.FinanceID == finance.FinanceID);
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
        /// Delete finance record
        /// </summary>
        public bool DeleteFinanceRecord(string financeId)
        {
            try
            {
                _logService.LogInformation($"Attempting to delete finance record with ID: {financeId}");
                
                var existingRecord = _context.Finances.FirstOrDefault(f => f.FinanceID == financeId);
                if (existingRecord == null)
                {
                    _logService.LogError($"Finance record not found with ID: {financeId}");
                    return false;
                }

                _context.Finances.Remove(existingRecord);
                _context.SaveChanges();

                // Update customer balance if CustomerID is provided
                if (existingRecord.CustomerID.HasValue)
                {
                    UpdateCustomerBalanceAsync(existingRecord.CustomerID.Value).GetAwaiter().GetResult();
                }

                _logService.LogInformation($"Finance record deleted: {financeId}");
                return true;
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error deleting finance record: {ex.Message}");
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
                    .Include(f => f.Customer)
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
                    .Include(f => f.Customer)
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

        /// <summary>
        /// Record an expense for a purchase order
        /// </summary>
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
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal GrossProfit { get; set; }
    }

}