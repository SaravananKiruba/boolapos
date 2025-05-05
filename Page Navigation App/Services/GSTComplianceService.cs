using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;

namespace Page_Navigation_App.Services
{
    /// <summary>
    /// Service to manage GST compliance for jewelry business
    /// </summary>
    public class GSTComplianceService
    {
        private readonly AppDbContext _context;
        private readonly LogService _logService;
        private readonly ConfigurationService _configService;
        private readonly ReportService _reportService;
        
        // Regular expression for validating GST number format
        private readonly Regex _gstRegex = new Regex(@"^\d{2}[A-Z]{5}\d{4}[A-Z]{1}[A-Z\d]{1}[Z]{1}[A-Z\d]{1}$", RegexOptions.Compiled);
        
        // HSN codes for common jewelry items
        private readonly Dictionary<string, string> _jewelryHSNCodes = new Dictionary<string, string>
        {
            { "Gold", "7113 19 10" },
            { "Silver", "7113 11 10" },
            { "Platinum", "7113 19 90" },
            { "Diamond", "7113 19 30" },
            { "Gemstone", "7113 19 20" },
            { "Pearl", "7113 19 40" }
        };
        
        // Default GST rate for jewelry (3%)
        private const decimal DEFAULT_GST_RATE = 3.0m;

        public GSTComplianceService(
            AppDbContext context,
            LogService logService,
            ConfigurationService configService,
            ReportService reportService)
        {
            _context = context;
            _logService = logService;
            _configService = configService;
            _reportService = reportService;
        }

        /// <summary>
        /// Validate if a GST number is in the correct format
        /// </summary>
        public bool IsValidGSTNumber(string gstNumber)
        {
            if (string.IsNullOrWhiteSpace(gstNumber))
                return false;
                
            return _gstRegex.IsMatch(gstNumber);
        }

        /// <summary>
        /// Get the HSN code for a jewelry type
        /// </summary>
        public string GetHSNCode(string jewelryType)
        {
            if (string.IsNullOrWhiteSpace(jewelryType))
                return _jewelryHSNCodes["Gold"]; // Default to gold
                
            foreach (var key in _jewelryHSNCodes.Keys)
            {
                if (jewelryType.Contains(key, StringComparison.OrdinalIgnoreCase))
                {
                    return _jewelryHSNCodes[key];
                }
            }
            
            return _jewelryHSNCodes["Gold"]; // Default to gold if no match
        }

        /// <summary>
        /// Generate a GST compliant invoice number
        /// </summary>
        public async Task<string> GenerateGSTInvoiceNumberAsync()
        {
            try
            {
                // Get business info for invoice prefix
                var businessInfo = await _configService.GetBusinessInfoAsync();
                string prefix = "INV"; // Default prefix
                
                // Get financial year
                DateTime now = DateTime.Now;
                string financialYear;
                
                if (now.Month >= 4) // April or later
                {
                    financialYear = $"{now.Year}-{(now.Year + 1) % 100:D2}";
                }
                else // January to March
                {
                    financialYear = $"{now.Year - 1}-{now.Year % 100:D2}";
                }
                
                // Get last invoice number
                var lastInvoice = await _context.Orders
                    .Where(o => o.InvoiceNumber != null && o.InvoiceNumber.Contains(financialYear))
                    .OrderByDescending(o => o.OrderID)
                    .FirstOrDefaultAsync();
                
                int nextNumber = 1;
                
                if (lastInvoice != null && lastInvoice.InvoiceNumber != null)
                {
                    // Extract the number part
                    string[] parts = lastInvoice.InvoiceNumber.Split('/');
                    if (parts.Length > 0 && int.TryParse(parts[parts.Length - 1], out int lastNumber))
                    {
                        nextNumber = lastNumber + 1;
                    }
                }
                
                // Format: PREFIX/FY/001
                string invoiceNumber = $"{prefix}/{financialYear}/{nextNumber:D3}";
                
                await _logService.LogInformationAsync($"Generated GST invoice number: {invoiceNumber}");
                return invoiceNumber;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error generating GST invoice number: {ex.Message}", exception: ex);
                // Fallback to a simple format
                return $"INV-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4)}";
            }
        }

        /// <summary>
        /// Calculate GST for an order based on location
        /// </summary>
        public async Task<GSTCalculation> CalculateGSTAsync(Order order)
        {
            try
            {
                if (order == null)
                {
                    await _logService.LogErrorAsync("Cannot calculate GST for null order");
                    return null;
                }
                
                // Get business info
                var businessInfo = await _configService.GetBusinessInfoAsync();
                string businessState = businessInfo.State;
                
                // Get customer state (default to business state if not available)
                string customerState = businessState;
                if (order.Customer != null)
                {
                    // Assume the state information might be in a different property or address field
                    customerState = businessState; // Fallback to same-state transactions
                }
                
                decimal totalAmount = order.TotalAmount;
                
                // For this implementation, we'll consider all transactions as intra-state
                bool isInterState = false;
                
                // CGST and SGST for intra-state, IGST for inter-state
                decimal cgst = 0;
                decimal sgst = 0;
                decimal igst = 0;
                
                if (isInterState)
                {
                    // Inter-state: apply IGST
                    igst = Math.Round(totalAmount * (DEFAULT_GST_RATE / 100), 2);
                }
                else
                {
                    // Intra-state: apply CGST and SGST (equal split)
                    cgst = Math.Round(totalAmount * (DEFAULT_GST_RATE / 200), 2);
                    sgst = cgst; // SGST is always equal to CGST
                }
                
                // Calculate grand total
                decimal grandTotal = totalAmount + cgst + sgst + igst;
                
                var calculation = new GSTCalculation
                {
                    OrderID = order.OrderID,
                    SubTotal = totalAmount,
                    CGST = cgst,
                    SGST = sgst,
                    IGST = igst,
                    GrandTotal = grandTotal,
                    IsInterState = isInterState,
                    BusinessState = businessState,
                    CustomerState = customerState,
                    GSTRate = DEFAULT_GST_RATE
                };
                
                return calculation;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error calculating GST: {ex.Message}", exception: ex);
                return null;
            }
        }

        /// <summary>
        /// Apply GST calculation to an order
        /// </summary>
        public async Task<bool> ApplyGSTToOrderAsync(int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .FirstOrDefaultAsync(o => o.OrderID == orderId);
                
                if (order == null)
                {
                    await _logService.LogErrorAsync($"Order not found: {orderId}");
                    return false;
                }
                
                var calculation = await CalculateGSTAsync(order);
                
                if (calculation == null)
                {
                    await _logService.LogErrorAsync($"Failed to calculate GST for order: {orderId}");
                    return false;
                }
                
                // Apply calculations to order
                order.CGST = calculation.CGST;
                order.SGST = calculation.SGST;
                order.IGST = calculation.IGST;
                order.GrandTotal = calculation.GrandTotal;
                
                // Update order
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
                
                await _logService.LogInformationAsync($"Applied GST to order {orderId}: CGST={calculation.CGST}, SGST={calculation.SGST}, IGST={calculation.IGST}");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error applying GST to order: {ex.Message}", exception: ex);
                return false;
            }
        }

        /// <summary>
        /// Generate GST report for a period - delegates to ReportService
        /// </summary>
        public async Task<GSTReport> GenerateGSTReportAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                // Delegate to ReportService to avoid duplicate implementation
                return await _reportService.GenerateGSTReportAsync(fromDate, toDate);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error generating GST report: {ex.Message}", exception: ex);
                return new GSTReport
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    TotalInvoices = 0,
                    TotalSales = 0
                };
            }
        }
    }

    /// <summary>
    /// GST calculation results
    /// </summary>
    public class GSTCalculation
    {
        public int OrderID { get; set; }
        public decimal SubTotal { get; set; }
        public decimal CGST { get; set; }
        public decimal SGST { get; set; }
        public decimal IGST { get; set; }
        public decimal GrandTotal { get; set; }
        public bool IsInterState { get; set; }
        public string BusinessState { get; set; }
        public string CustomerState { get; set; }
        public decimal GSTRate { get; set; }
    }

    /// <summary>
    /// State-wise tax breakdown
    /// </summary>
    public class StateWiseTax
    {
        public string State { get; set; }
        public decimal TaxableValue { get; set; }
        public decimal CGST { get; set; }
        public decimal SGST { get; set; }
        public decimal IGST { get; set; }
        public bool IsInterState { get; set; }
    }

    /// <summary>
    /// HSN code wise tax breakdown
    /// </summary>
    public class HSNWiseTax
    {
        public string HSNCode { get; set; }
        public string Description { get; set; }
        public decimal TaxableValue { get; set; }
        public decimal CGST { get; set; }
        public decimal SGST { get; set; }
        public decimal IGST { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>
    /// Monthly tax summary
    /// </summary>
    public class MonthlySummary
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalTax { get; set; }
        public int OrderCount { get; set; }
    }
}