using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Page_Navigation_App.Services
{
    public class InvoiceService
    {
        private readonly AppDbContext _context;

        public InvoiceService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateInvoiceNumber()
        {
            var today = DateTime.Today;
            var yearMonth = today.ToString("yyyyMM");
            
            // Find the last invoice number with this prefix
            var lastInvoice = await _context.Orders
                .Where(o => o.InvoiceNumber != null && o.InvoiceNumber.StartsWith($"INV-{yearMonth}"))
                .OrderByDescending(o => o.InvoiceNumber)
                .FirstOrDefaultAsync();
                
            int sequence = 1;
            if (lastInvoice != null && lastInvoice.InvoiceNumber != null)
            {
                // Extract sequence number from last invoice
                var lastSequenceStr = lastInvoice.InvoiceNumber.Split('-').Last();
                if (int.TryParse(lastSequenceStr, out int lastSequence))
                {
                    sequence = lastSequence + 1;
                }
            }
            
            return $"INV-{yearMonth}-{sequence:D4}";
        }

        public async Task<Dictionary<string, object>> GetInvoiceData(int orderId)
        {
            // Get order with all related data
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);
                
            if (order == null)
                throw new Exception($"Order with ID {orderId} not found");
                
            // Get business info for invoice header
            var businessInfo = await _context.Set<BusinessInfo>().FirstOrDefaultAsync();
            if (businessInfo == null)
                throw new Exception("Business information not configured");
                
            // Format order details for the invoice
            var formattedItems = order.OrderDetails.Select(od => new
            {
                ProductName = od.Product.ProductName,
                HSNCode = od.HSNCode ?? "7113",  // Default HSN code for jewelry if not specified
                MetalType = od.Product.MetalType,
                Purity = od.Product.Purity,
                NetWeight = $"{od.NetWeight:F3} g",
                GrossWeight = $"{od.GrossWeight:F3} g",
                Quantity = od.Quantity,
                UnitPrice = $"₹{od.UnitPrice:N2}",
                Amount = $"₹{od.TotalAmount:N2}"
            }).ToList();
            
            // Calculate summary values
            var totalNetWeight = order.OrderDetails.Sum(od => od.NetWeight * od.Quantity);
            var totalGrossWeight = order.OrderDetails.Sum(od => od.GrossWeight * od.Quantity);
            
            // Format currency values with ₹ symbol
            var formattedTotalAmount = $"₹{order.TotalAmount:N2}";
            var formattedCGST = $"₹{order.CGST:N2}";
            var formattedSGST = $"₹{order.SGST:N2}";
            var formattedGrandTotal = $"₹{order.GrandTotal:N2}";
            
            return new Dictionary<string, object>
            {
                ["Order"] = order,
                ["BusinessInfo"] = businessInfo,
                ["Items"] = formattedItems,
                ["TotalNetWeight"] = $"{totalNetWeight:F3} g",
                ["TotalGrossWeight"] = $"{totalGrossWeight:F3} g",
                ["FormattedTotalAmount"] = formattedTotalAmount,
                ["FormattedCGST"] = formattedCGST,
                ["FormattedSGST"] = formattedSGST,
                ["FormattedGrandTotal"] = formattedGrandTotal,
                ["InvoiceDate"] = order.OrderDate.ToString("dd-MMM-yyyy"),
                ["AmountInWords"] = ConvertAmountToWords(order.GrandTotal)
            };
        }

        public async Task<byte[]> GenerateInvoicePDF(int orderId)
        {
            var invoiceData = await GetInvoiceData(orderId);
            
            // In a real implementation, this would use a PDF library like iTextSharp, PDFsharp, etc.
            // For this implementation, we'll simulate PDF generation by creating a text representation
            var invoiceText = GenerateInvoiceText(invoiceData);
            
            return Encoding.UTF8.GetBytes(invoiceText);
        }

        private string GenerateInvoiceText(Dictionary<string, object> invoiceData)
        {
            var sb = new StringBuilder();
            var order = (Order)invoiceData["Order"];
            var businessInfo = (BusinessInfo)invoiceData["BusinessInfo"];
            var items = (IEnumerable<object>)invoiceData["Items"];
            
            // Header
            sb.AppendLine($"{businessInfo.BusinessName}".PadCenter(80));
            sb.AppendLine($"{businessInfo.Address}, {businessInfo.City}".PadCenter(80));
            sb.AppendLine($"{businessInfo.State} - {businessInfo.PostalCode}".PadCenter(80));
            sb.AppendLine($"Phone: {businessInfo.Phone}, Email: {businessInfo.Email}".PadCenter(80));
            sb.AppendLine($"GSTIN: {businessInfo.TaxId}".PadCenter(80));
            sb.AppendLine(new string('-', 80));
            
            // Invoice details
            sb.AppendLine($"TAX INVOICE");
            sb.AppendLine($"Invoice No: {order.InvoiceNumber}");
            sb.AppendLine($"Date: {invoiceData["InvoiceDate"]}");
            sb.AppendLine();
            
            // Customer details
            sb.AppendLine("Customer Details:");
            sb.AppendLine($"Name: {order.Customer.CustomerName}");
            sb.AppendLine($"Address: {order.Customer.Address}");
            sb.AppendLine($"Mobile: {order.Customer.PhoneNumber}");
            if (!string.IsNullOrEmpty(order.Customer.GSTNumber))
                sb.AppendLine($"GSTIN: {order.Customer.GSTNumber}");
            sb.AppendLine();
            
            // Item details
            sb.AppendLine(new string('-', 80));
            sb.AppendLine("S.No Product                      HSN    Net Wt   Gross Wt  Qty  Unit Price    Amount");
            sb.AppendLine(new string('-', 80));
            
            int i = 1;
            foreach (dynamic item in items)
            {
                sb.AppendLine($"{i,-4} {item.ProductName,-28} {item.HSNCode,-6} {item.NetWeight,-8} {item.GrossWeight,-10} {item.Quantity,-4} {item.UnitPrice,-12} {item.Amount}");
                i++;
            }
            
            sb.AppendLine(new string('-', 80));
            
            // Summary
            sb.AppendLine($"Total Net Weight: {invoiceData["TotalNetWeight"],-10} Total Gross Weight: {invoiceData["TotalGrossWeight"]}");
            sb.AppendLine();
            sb.AppendLine($"{"Sub Total:",-60} {invoiceData["FormattedTotalAmount"]}");
            sb.AppendLine($"{"CGST @ 1.5%:",-60} {invoiceData["FormattedCGST"]}");
            sb.AppendLine($"{"SGST @ 1.5%:",-60} {invoiceData["FormattedSGST"]}");
            sb.AppendLine($"{"Grand Total:",-60} {invoiceData["FormattedGrandTotal"]}");
            sb.AppendLine();
            sb.AppendLine($"Amount in words: {invoiceData["AmountInWords"]}");
            sb.AppendLine();
            
            // Terms and conditions
            sb.AppendLine("Terms & Conditions:");
            sb.AppendLine("1. Goods once sold will not be taken back or exchanged.");
            sb.AppendLine("2. Subject to local jurisdiction.");
            sb.AppendLine();
            
            // Footer
            sb.AppendLine(new string('-', 80));
            sb.AppendLine($"Thank you for your business!".PadCenter(80));
            sb.AppendLine($"Visit us again".PadCenter(80));
            
            return sb.ToString();
        }

        private string ConvertAmountToWords(decimal amount)
        {
            // Simple implementation - in a real system, this would be more comprehensive
            var words = new[]
            {
                "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
                "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen"
            };
            
            var tens = new[]
            {
                "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
            };
            
            // Convert to Indian Rupee format
            int rupees = (int)Math.Floor(amount);
            int paise = (int)Math.Round((amount - rupees) * 100);
            
            if (rupees == 0)
                return "Zero Rupees" + (paise > 0 ? $" and {paise} Paise" : "");
                
            // Handle rupees part
            string result = "";
            
            if (rupees > 9999999)
            {
                result += ConvertCrores(rupees / 10000000) + " Crore ";
                rupees %= 10000000;
            }
            
            if (rupees > 99999)
            {
                result += ConvertLakhs(rupees / 100000) + " Lakh ";
                rupees %= 100000;
            }
            
            if (rupees > 999)
            {
                result += ConvertThousands(rupees / 1000) + " Thousand ";
                rupees %= 1000;
            }
            
            if (rupees > 99)
            {
                result += ConvertHundreds(rupees / 100) + " Hundred ";
                rupees %= 100;
            }
            
            if (rupees > 0)
            {
                result += ConvertTens(rupees);
            }
            
            result += "Rupees";
            
            // Handle paise part
            if (paise > 0)
            {
                result += " and " + ConvertTens(paise) + " Paise";
            }
            
            return result;
            
            // Local functions for number conversion
            string ConvertTens(int number)
            {
                if (number < 20)
                    return words[number];
                    
                return tens[number / 10] + (number % 10 > 0 ? " " + words[number % 10] : "");
            }
            
            string ConvertHundreds(int number)
            {
                return words[number];
            }
            
            string ConvertThousands(int number)
            {
                if (number > 99)
                    return ConvertHundreds(number / 100) + " Hundred " + (number % 100 > 0 ? ConvertTens(number % 100) : "");
                    
                return ConvertTens(number);
            }
            
            string ConvertLakhs(int number)
            {
                if (number > 99)
                    return ConvertHundreds(number / 100) + " Hundred " + (number % 100 > 0 ? ConvertTens(number % 100) : "");
                    
                return ConvertTens(number);
            }
            
            string ConvertCrores(int number)
            {
                if (number > 99)
                    return ConvertHundreds(number / 100) + " Hundred " + (number % 100 > 0 ? ConvertTens(number % 100) : "");
                    
                return ConvertTens(number);
            }
        }
    }
    
    // Extension method to pad a string in the center
    public static class StringExtensions
    {
        public static string PadCenter(this string text, int width)
        {
            int paddingRequired = width - text.Length;
            if (paddingRequired <= 0)
                return text;
                
            int leftPadding = paddingRequired / 2;
            int rightPadding = paddingRequired - leftPadding;
            
            return new string(' ', leftPadding) + text + new string(' ', rightPadding);
        }
    }
}