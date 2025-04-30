using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Page_Navigation_App.Services
{
    public class FinanceService
    {
        private readonly AppDbContext _context;
        private readonly LogService _logService;

        public FinanceService(AppDbContext context, LogService logService)
        {
            _context = context;
            _logService = logService;
        }

        public async Task<decimal> GetCustomerDues(int customerId)
        {
            return await _context.Finances
                .Where(f => f.CustomerId == customerId && f.Status == "Active")
                .SumAsync(f => f.RemainingAmount);
        }

        public IEnumerable<Finance> GetAllTransactions()
        {
            return _context.Finances
                .OrderByDescending(f => f.TransactionDate)
                .ToList();
        }

        public bool UpdateTransaction(Finance transaction)
        {
            try
            {
                _context.Finances.Update(transaction);
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool AddTransaction(Finance transaction)
        {
            try
            {
                transaction.TransactionDate = DateTime.Now;
                _context.Finances.Add(transaction);
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public IEnumerable<Finance> FilterTransactionsByType(string type)
        {
            return _context.Finances
                .Where(f => f.TransactionType == type)
                .OrderByDescending(f => f.TransactionDate)
                .ToList();
        }

        public IEnumerable<Finance> FilterTransactionsByDate(DateTime startDate, DateTime endDate)
        {
            return _context.Finances
                .Where(f => f.TransactionDate >= startDate && f.TransactionDate <= endDate)
                .OrderByDescending(f => f.TransactionDate)
                .ToList();
        }

        public async Task<Finance> AddFinanceRecord(Finance finance)
        {
            await _context.Finances.AddAsync(finance);
            await _context.SaveChangesAsync();
            return finance;
        }

        public async Task<Finance> CreateEMIPlan(
            decimal totalAmount,
            int numberOfInstallments,
            decimal interestRate,
            int customerId,
            int orderReference)
        {
            var finance = new Finance
            {
                CustomerId = customerId,
                OrderReference = orderReference,
                TotalAmount = totalAmount,
                InterestRate = interestRate,
                NumberOfInstallments = numberOfInstallments,
                InstallmentAmount = CalculateEMIAmount(totalAmount, numberOfInstallments, interestRate),
                RemainingAmount = totalAmount,
                StartDate = DateTime.Now,
                NextInstallmentDate = DateTime.Now.AddMonths(1),
                Status = "Active"
            };

            await _context.Finances.AddAsync(finance);
            await _context.SaveChangesAsync();

            await _logService.LogAudit(
                "EMI_CREATED",
                $"EMI plan created for customer {customerId}, Amount: {totalAmount}, Installments: {numberOfInstallments}");

            return finance;
        }

        private decimal CalculateEMIAmount(decimal principal, int tenure, decimal interestRate)
        {
            var monthlyRate = (interestRate / 12) / 100;
            var EMI = principal * monthlyRate * (decimal)Math.Pow(1 + (double)monthlyRate, tenure) 
                     / (decimal)(Math.Pow(1 + (double)monthlyRate, tenure) - 1);
            return Math.Round(EMI, 2);
        }

        public async Task<bool> RecordEMIPayment(int financeId, decimal amount)
        {
            var finance = await _context.Finances.FindAsync(financeId);
            if (finance == null)
                return false;

            finance.RemainingAmount -= amount;
            finance.LastPaymentDate = DateTime.Now;
            finance.NextInstallmentDate = DateTime.Now.AddMonths(1);

            if (finance.RemainingAmount <= 0)
            {
                finance.Status = "Completed";
            }

            await _context.SaveChangesAsync();

            await _logService.LogAudit(
                "EMI_PAYMENT",
                $"EMI payment recorded for finance ID {financeId}, Amount: {amount}");

            return true;
        }

        public async Task<IEnumerable<Finance>> GetOverdueEMIs()
        {
            return await _context.Finances
                .Where(f => f.Status == "Active" && f.NextInstallmentDate < DateTime.Now)
                .Include(f => f.Customer)
                .ToListAsync();
        }

        public async Task<IEnumerable<Finance>> GetCustomerEMIs(int customerId)
        {
            return await _context.Finances
                .Where(f => f.CustomerId == customerId)
                .OrderByDescending(f => f.StartDate)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalOutstandingAmount()
        {
            return await _context.Finances
                .Where(f => f.Status == "Active")
                .SumAsync(f => f.RemainingAmount);
        }

        public async Task<IEnumerable<Finance>> GetUpcomingInstallments(DateTime startDate, DateTime endDate)
        {
            return await _context.Finances
                .Where(f => f.Status == "Active" 
                    && f.NextInstallmentDate >= startDate 
                    && f.NextInstallmentDate <= endDate)
                .Include(f => f.Customer)
                .OrderBy(f => f.NextInstallmentDate)
                .ToListAsync();
        }

        public async Task SendPaymentReminders()
        {
            var upcomingPayments = await _context.Finances
                .Where(f => f.Status == "Active" 
                    && f.NextInstallmentDate <= DateTime.Now.AddDays(7))
                .Include(f => f.Customer)
                .ToListAsync();

            foreach (var payment in upcomingPayments)
            {
                // TODO: Integrate with NotificationService to send reminders
                await _logService.LogInfo(
                    $"Payment reminder would be sent to customer {payment.CustomerId} " +
                    $"for amount {payment.InstallmentAmount} due on {payment.NextInstallmentDate}");
            }
        }

        public async Task<Dictionary<string, decimal>> GetFinancialSummary()
        {
            var activePlans = await _context.Finances
                .Where(f => f.Status == "Active")
                .ToListAsync();

            return new Dictionary<string, decimal>
            {
                ["TotalOutstanding"] = activePlans.Sum(f => f.RemainingAmount),
                ["TotalEMIPlans"] = activePlans.Count,
                ["AverageEMIAmount"] = activePlans.Any() 
                    ? activePlans.Average(f => f.InstallmentAmount) 
                    : 0,
                ["OverdueAmount"] = activePlans
                    .Where(f => f.NextInstallmentDate < DateTime.Now)
                    .Sum(f => f.InstallmentAmount)
            };
        }

        public async Task<(decimal foreClosureAmount, decimal charges)> CalculateForeclosureAmount(int financeId)
        {
            var finance = await _context.Finances.FindAsync(financeId);
            if (finance == null)
                throw new ArgumentException("Finance plan not found");

            var foreClosurePenaltyRate = 2.0m; // 2% foreclosure charges
            var charges = (finance.RemainingAmount * foreClosurePenaltyRate) / 100;
            return (finance.RemainingAmount, charges);
        }

        public async Task<(decimal amount, int remainingTenure)> CalculateBalanceAfterPrepayment(
            int financeId, 
            decimal prepaymentAmount)
        {
            var finance = await _context.Finances.FindAsync(financeId);
            if (finance == null)
                throw new ArgumentException("Finance plan not found");

            var remainingAmount = finance.RemainingAmount - prepaymentAmount;
            var remainingTenure = (int)Math.Ceiling(remainingAmount / finance.InstallmentAmount);
            
            return (remainingAmount, remainingTenure);
        }

        public decimal CalculateRemainingInterest(decimal remainingPrincipal, int remainingTenure, decimal interestRate)
        {
            var monthlyRate = (interestRate / 12) / 100;
            var totalAmount = remainingPrincipal * (decimal)Math.Pow(1 + (double)monthlyRate, remainingTenure);
            return totalAmount - remainingPrincipal;
        }
    }
}