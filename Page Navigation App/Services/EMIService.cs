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
    /// Service to manage EMI (Equated Monthly Installment) operations
    /// </summary>
    public class EMIService
    {
        private readonly AppDbContext _context;
        private readonly LogService _logService;
        private readonly FinanceService _financeService;

        public EMIService(
            AppDbContext context,
            LogService logService,
            FinanceService financeService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _financeService = financeService ?? throw new ArgumentNullException(nameof(financeService));
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

                // Create EMI records
                for (int i = 0; i < numberOfMonths; i++)
                {
                    var dueDate = startDate.AddMonths(i);
                    decimal amount = emiAmount;
                    
                    // Adjust last EMI to account for rounding errors
                    if (i == numberOfMonths - 1)
                    {
                        decimal totalPaid = emiAmount * (numberOfMonths - 1);
                        amount = order.GrandTotal - totalPaid;
                    }

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
                await _logService.LogErrorAsync($"Error creating EMI plan: {ex.Message}", exception: ex);
                return false;
            }
        }

        /// <summary>
        /// Get all pending EMIs for a customer
        /// </summary>
        public async Task<IEnumerable<EMI>> GetPendingEMIsForCustomerAsync(int customerId)
        {
            try
            {
                return await _context.EMIs
                    .Where(e => e.CustomerID == customerId && e.Status != "Paid")
                    .OrderBy(e => e.DueDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error getting pending EMIs for customer {customerId}: {ex.Message}", exception: ex);
                return Enumerable.Empty<EMI>();
            }
        }

        /// <summary>
        /// Record a payment for an EMI
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
                    await _logService.LogWarningAsync($"EMI {emiId} is already paid");
                    return false;
                }

                // Record payment using finance service
                var payment = new Finance
                {
                    CustomerID = emi.CustomerID,
                    OrderID = emi.OrderID,
                    Amount = amount,
                    PaymentMode = paymentMode,
                    TransactionDate = DateTime.Now,
                    ReferenceNumber = referenceNumber,
                    IsPaymentReceived = true,
                    Notes = $"EMI Payment #{emi.EMINumber}",
                    TransactionType = "EMI Payment"
                };

                await _financeService.RecordPaymentAsync(payment);

                // Update EMI status
                emi.Status = amount >= emi.Amount ? "Paid" : "Partial";
                emi.PaidAmount = amount;
                emi.PaymentDate = DateTime.Now;

                await _context.SaveChangesAsync();
                await _logService.LogInformationAsync($"EMI payment recorded for EMI {emiId}: {amount}");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error recording EMI payment: {ex.Message}", exception: ex);
                return false;
            }
        }

        /// <summary>
        /// Get EMIs for an order
        /// </summary>
        public async Task<IEnumerable<EMI>> GetEMIsForOrderAsync(int orderId)
        {
            try
            {
                return await _context.EMIs
                    .Where(e => e.OrderID == orderId)
                    .OrderBy(e => e.EMINumber)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error getting EMIs for order {orderId}: {ex.Message}", exception: ex);
                return Enumerable.Empty<EMI>();
            }
        }
    }
}