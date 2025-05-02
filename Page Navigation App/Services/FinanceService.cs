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
                .SumAsync(f => f.RemainingAmount ?? 0);
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
                .SumAsync(f => f.RemainingAmount ?? 0);
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
                ["TotalOutstanding"] = activePlans.Sum(f => f.RemainingAmount ?? 0),
                ["TotalEMIPlans"] = activePlans.Count,
                ["AverageEMIAmount"] = activePlans.Any() 
                    ? activePlans.Average(f => f.InstallmentAmount ?? 0) 
                    : 0,
                ["OverdueAmount"] = activePlans
                    .Where(f => f.NextInstallmentDate < DateTime.Now)
                    .Sum(f => f.InstallmentAmount ?? 0)
            };
        }

        public async Task<(decimal foreClosureAmount, decimal charges)> CalculateForeclosureAmount(int financeId)
        {
            var finance = await _context.Finances.FindAsync(financeId);
            if (finance == null)
                throw new ArgumentException("Finance plan not found");

            var remainingAmount = finance.RemainingAmount ?? 0;
            var charges = CalculateForeclosureCharges(remainingAmount);
            return (remainingAmount, charges);
        }

        public async Task<(decimal amount, int remainingTenure)> CalculateBalanceAfterPrepayment(
            int financeId, 
            decimal prepaymentAmount)
        {
            var finance = await _context.Finances.FindAsync(financeId);
            if (finance == null)
                throw new ArgumentException("Finance plan not found");

            var remainingAmount = (finance.RemainingAmount ?? 0) - prepaymentAmount;
            var remainingTenure = (int)Math.Ceiling(remainingAmount / (finance.InstallmentAmount ?? 1));
            
            return (remainingAmount, remainingTenure);
        }

        public decimal CalculateRemainingInterest(decimal remainingPrincipal, int remainingTenure, decimal interestRate)
        {
            var monthlyRate = (interestRate / 12) / 100;
            var totalAmount = remainingPrincipal * (decimal)Math.Pow(1 + (double)monthlyRate, remainingTenure);
            return totalAmount - remainingPrincipal;
        }

        private decimal CalculateForeclosureCharges(decimal remainingAmount)
        {
            const decimal FORECLOSURE_PENALTY_RATE = 2.0m; // 2% foreclosure charges
            return (remainingAmount * FORECLOSURE_PENALTY_RATE) / 100;
        }

        public async Task<Dictionary<string, decimal>> GetMetalTradeAnalytics(DateTime startDate, DateTime endDate)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .ToListAsync();

            var exchangeOrders = orders.Where(o => o.HasMetalExchange);

            var result = new Dictionary<string, decimal>();

            // Calculate metal received through exchanges
            var exchangeMetals = exchangeOrders
                .GroupBy(o => new { o.ExchangeMetalType, o.ExchangeMetalPurity })
                .Select(g => new
                {
                    Key = $"{g.Key.ExchangeMetalType}_{g.Key.ExchangeMetalPurity}_Received",
                    Weight = g.Sum(o => o.ExchangeMetalWeight),
                    Value = g.Sum(o => o.ExchangeValue)
                });

            foreach (var metal in exchangeMetals)
            {
                result[metal.Key + "_Weight"] = metal.Weight;
                result[metal.Key + "_Value"] = metal.Value;
            }

            // Calculate metal sold in sales
            var salesMetals = orders
                .SelectMany(o => o.OrderDetails)
                .GroupBy(od => new { od.Product.MetalType, od.Product.Purity })
                .Select(g => new
                {
                    Key = $"{g.Key.MetalType}_{g.Key.Purity}_Sold",
                    Weight = g.Sum(od => od.NetWeight),
                    Value = g.Sum(od => od.BaseAmount)
                });

            foreach (var metal in salesMetals)
            {
                result[metal.Key + "_Weight"] = metal.Weight;
                result[metal.Key + "_Value"] = metal.Value;
            }

            return result;
        }

        public async Task<Finance> RecordMetalPurchase(
            string metalType,
            string purity,
            decimal weight,
            decimal ratePerGram,
            string supplier,
            string invoiceNumber)
        {
            var finance = new Finance
            {
                TransactionType = "Metal_Purchase",
                TransactionDate = DateTime.Now,
                Amount = weight * ratePerGram,
                PaymentMode = "Bank_Transfer", // Default for metal purchases
                Category = "Inventory",
                Description = $"Purchase of {weight}g {metalType} {purity} @ {ratePerGram}/g",
                ReferenceNumber = invoiceNumber,
                MetalType = metalType,
                MetalPurity = purity,
                MetalWeight = weight,
                MetalRate = ratePerGram,
                SupplierName = supplier
            };

            await _context.Finances.AddAsync(finance);
            await _context.SaveChangesAsync();

            await _logService.LogAudit(
                "METAL_PURCHASE",
                $"Metal purchase recorded: {finance.Description}");

            return finance;
        }

        public async Task<Dictionary<string, decimal>> GetMetalInventoryValue()
        {
            var result = new Dictionary<string, decimal>();
            
            // Get current rates
            var currentRates = await _context.RateMaster
                .Where(r => r.IsActive)
                .ToListAsync();

            // Group metal purchases and sales
            var purchases = await _context.Finances
                .Where(f => f.TransactionType == "Metal_Purchase")
                .GroupBy(f => new { f.MetalType, f.MetalPurity })
                .Select(g => new
                {
                    g.Key.MetalType,
                    g.Key.MetalPurity,
                    TotalWeight = g.Sum(f => f.MetalWeight),
                    AverageCost = g.Average(f => f.MetalRate)
                })
                .ToListAsync();

            foreach (var purchase in purchases)
            {
                var currentRate = currentRates
                    .FirstOrDefault(r => r.MetalType == purchase.MetalType && 
                                       r.Purity == purchase.MetalPurity);

                if (currentRate != null)
                {
                    result[$"{purchase.MetalType}_{purchase.MetalPurity}_Weight"] = purchase.TotalWeight ?? 0;
                    result[$"{purchase.MetalType}_{purchase.MetalPurity}_CostValue"] = 
                        (purchase.TotalWeight ?? 0) * (purchase.AverageCost ?? 0);
                    result[$"{purchase.MetalType}_{purchase.MetalPurity}_MarketValue"] = 
                        (purchase.TotalWeight ?? 0) * currentRate.Rate;
                }
            }

            return result;
        }

        public async Task<Finance> RecordGoldSchemePayment(
            int customerId,
            decimal amount,
            string paymentMode,
            decimal goldRate,
            decimal goldQuantityGrams)
        {
            var finance = new Finance
            {
                TransactionType = "Gold_Scheme",
                TransactionDate = DateTime.Now,
                CustomerId = customerId,
                Amount = amount,
                PaymentMode = paymentMode,
                Category = "Gold_Scheme_Collection",
                Description = $"Gold scheme payment for {goldQuantityGrams}g @ {goldRate}/g",
                MetalType = "Gold",
                MetalWeight = goldQuantityGrams,
                MetalRate = goldRate
            };

            await _context.Finances.AddAsync(finance);
            await _context.SaveChangesAsync();

            await _logService.LogAudit(
                "GOLD_SCHEME_PAYMENT",
                $"Gold scheme payment recorded for customer {customerId}: Amount {amount}, Gold {goldQuantityGrams}g");

            return finance;
        }

        public async Task<IEnumerable<Finance>> GetCustomerGoldSchemeTransactions(int customerId)
        {
            return await _context.Finances
                .Where(f => f.CustomerId == customerId && 
                           f.TransactionType == "Gold_Scheme")
                .OrderByDescending(f => f.TransactionDate)
                .ToListAsync();
        }

        public async Task<Dictionary<string, decimal>> GetGoldSchemeAnalytics()
        {
            var schemeTransactions = await _context.Finances
                .Where(f => f.TransactionType == "Gold_Scheme")
                .ToListAsync();

            return new Dictionary<string, decimal>
            {
                { "TotalCollections", schemeTransactions.Sum(t => t.Amount) },
                { "TotalGoldQuantity", schemeTransactions.Sum(t => t.MetalWeight ?? 0) },
                { "AverageGoldRate", schemeTransactions.Any() ? 
                    schemeTransactions.Average(t => t.MetalRate ?? 0) : 0 },
                { "ActiveCustomers", schemeTransactions
                    .Select(t => t.CustomerId)
                    .Distinct()
                    .Count() }
            };
        }

        public async Task<decimal> GetTotalOutstandingDues()
        {
            return await _context.Finances
                .Where(f => f.Status == "Active")
                .SumAsync(f => f.RemainingAmount ?? 0);
        }

        public async Task<Dictionary<string, decimal>> GetEMIAnalytics()
        {
            var activePlans = await _context.Finances
                .Where(f => f.TransactionType == "EMI" && f.Status == "Active")
                .ToListAsync();

            return new Dictionary<string, decimal>
            {
                ["TotalPlans"] = activePlans.Count,
                ["TotalOutstanding"] = activePlans.Sum(f => f.RemainingAmount ?? 0),
                ["AverageEMIAmount"] = activePlans.Any() 
                    ? activePlans.Average(f => f.InstallmentAmount ?? 0) 
                    : 0,
                ["OverdueAmount"] = activePlans
                    .Where(f => f.NextInstallmentDate < DateTime.Now)
                    .Sum(f => f.InstallmentAmount ?? 0)
            };
        }

        public async Task<(decimal remainingAmount, decimal charges)> CalculateEMIClosureAmount(int financeId)
        {
            var finance = await _context.Finances.FindAsync(financeId);
            if (finance == null) return (0, 0);

            var charges = CalculateForeclosureCharges(finance.RemainingAmount ?? 0);
            return (finance.RemainingAmount ?? 0, charges);
        }

        public async Task<(decimal remainingAmount, int remainingTenure)> GetEMIStatus(int financeId)
        {
            var finance = await _context.Finances.FindAsync(financeId);
            if (finance == null) return (0, 0);

            var remainingAmount = finance.RemainingAmount ?? 0;
            var remainingTenure = (int)Math.Ceiling(remainingAmount / (finance.InstallmentAmount ?? 1));
            return (remainingAmount, remainingTenure);
        }
    }
}