using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Reporting.WinForms;
using Page_Navigation_App.Model;

namespace Page_Navigation_App.Services
{
    public class PrintService
    {
        private readonly OrderService _orderService;
        private readonly RepairJobService _repairService;
        private readonly RateMasterService _rateService;
        private readonly ProductService _productService;
        private readonly CustomerService _customerService;

        public PrintService(
            OrderService orderService,
            RepairJobService repairService,
            RateMasterService rateService,
            ProductService productService,
            CustomerService customerService)
        {
            _orderService = orderService;
            _repairService = repairService;
            _rateService = rateService;
            _productService = productService;
            _customerService = customerService;
        }

        public async Task<byte[]> GenerateInvoice(int orderId)
        {
            var order = await _orderService.GetOrderWithDetails(orderId);
            if (order == null) return null;

            using var report = new LocalReport();
            report.ReportPath = "Invoice.rdlc";

            // Prepare invoice data
            var reportData = new
            {
                InvoiceNumber = order.InvoiceNumber,
                OrderDate = order.OrderDate,
                CustomerName = order.Customer.CustomerName,
                CustomerAddress = order.Customer.Address,
                CustomerGST = order.Customer.GSTNumber,
                Items = order.OrderDetails.Select(od => new
                {
                    ProductName = od.Product.ProductName,
                    MetalType = od.Product.MetalType,
                    Purity = od.Product.Purity,
                    GrossWeight = od.GrossWeight,
                    NetWeight = od.NetWeight,
                    Rate = od.MetalRate,
                    MakingCharges = od.MakingCharges,
                    StoneValue = od.StoneValue,
                    Amount = od.BaseAmount,
                    CGST = od.CGSTAmount,
                    SGST = od.SGSTAmount,
                    IGST = od.IGSTAmount,
                    Total = od.FinalAmount
                }).ToList(),
                SubTotal = order.SubTotal,
                DiscountAmount = order.DiscountAmount,
                TaxAmount = order.TaxAmount,
                GrandTotal = order.GrandTotal,
                PaymentType = order.PaymentType,
                EMIDetails = order.PaymentType == "EMI" 
                    ? $"{order.EMIMonths} months x Rs.{order.EMIAmount}" 
                    : null,
                ExchangeDetails = order.HasMetalExchange
                    ? $"{order.ExchangeMetalType} {order.ExchangeMetalPurity} - {order.ExchangeMetalWeight}g (Rs.{order.ExchangeValue})"
                    : null
            };

            report.DataSources.Add(new ReportDataSource("InvoiceData", new[] { reportData }));
            report.DataSources.Add(new ReportDataSource("InvoiceItems", reportData.Items));

            return report.Render("PDF");
        }

        public async Task<byte[]> GenerateRepairReceipt(int repairId)
        {
            var repair = await _repairService.GetRepairJobById(repairId);
            if (repair == null) return null;

            using var report = new LocalReport();
            report.ReportPath = "RepairReceipt.rdlc";

            var reportData = new
            {
                ReceiptNumber = repair.RepairID.ToString(), // Changed from RepairJobID to RepairID
                CustomerName = repair.Customer?.CustomerName,
                CustomerPhone = repair.Customer?.PhoneNumber,
                ReceiptDate = repair.ReceiptDate,
                ItemDetails = repair.ItemDescription, // Changed from ItemDetails to ItemDescription
                WorkType = repair.WorkType,
                EstimatedAmount = repair.EstimatedCost, // Changed from EstimatedAmount to EstimatedCost
                Status = repair.Status
            };

            report.DataSources.Add(new ReportDataSource("RepairData", new[] { reportData }));
            return report.Render("PDF");
        }

        public async Task<byte[]> GenerateRateCard()
        {
            var rates = await _rateService.GetAllCurrentRates();
            using var report = new LocalReport();
            report.ReportPath = "RateCard.rdlc";

            var reportData = rates.Select(r => new
            {
                MetalTypePurity = $"{r.MetalType} {r.Purity}",
                Rate = r.SaleRate,
                Date = DateTime.Now.ToString("dd-MMM-yyyy")
            });

            report.DataSources.Add(new ReportDataSource("RateData", reportData));
            return report.Render("PDF");
        }

        public async Task<byte[]> GenerateDailyReport(DateTime date)
        {
            using var report = new LocalReport();
            report.ReportPath = "DailyReport.rdlc";

            var orders = await _orderService.GetOrdersByDate(date, date);
            var repairs = await _repairService.GetRepairsByDate(date, date);

            var reportData = new
            {
                ReportDate = date,
                Sales = new
                {
                    TotalOrders = orders.Count(),
                    CashSales = orders.Where(o => o.PaymentType == "Cash")
                        .Sum(o => o.GrandTotal),
                    CardSales = orders.Where(o => o.PaymentType == "Card")
                        .Sum(o => o.GrandTotal),
                    UPISales = orders.Where(o => o.PaymentType == "UPI")
                        .Sum(o => o.GrandTotal),
                    EMISales = orders.Where(o => o.PaymentType == "EMI")
                        .Sum(o => o.GrandTotal)
                },
                Repairs = new
                {
                    TotalRepairs = repairs.Count(),
                    PendingRepairs = repairs.Count(r => r.Status == "Pending"),
                    CompletedRepairs = repairs.Count(r => r.Status == "Completed"),
                    RepairValue = repairs.Sum(r => r.FinalAmount)
                },
                TaxSummary = new
                {
                    CGST = orders.Sum(o => o.OrderDetails.Sum(od => od.CGSTAmount)),
                    SGST = orders.Sum(o => o.OrderDetails.Sum(od => od.SGSTAmount)),
                    IGST = orders.Sum(o => o.OrderDetails.Sum(od => od.IGSTAmount))
                }
            };

            report.DataSources.Add(new ReportDataSource("DailyData", new[] { reportData }));
            return report.Render("PDF");
        }

        public async Task<byte[]> GenerateHallmarkCertificate(int productId)
        {
            var product = await _productService.GetProductById(productId);
            if (product == null) return null;

            using var report = new LocalReport();
            report.ReportPath = "HallmarkCertificate.rdlc";

            var reportData = new
            {
                CertificateNumber = product.HallmarkNumber,
                ProductName = product.ProductName,
                MetalType = product.MetalType,
                Purity = product.Purity,
                GrossWeight = product.GrossWeight,
                NetWeight = product.NetWeight,
                CertificationDate = DateTime.Now,
                Design = product.Design,
                StoneDetails = product.StoneDetails,
                StoneWeight = product.StoneWeight
            };

            report.DataSources.Add(new ReportDataSource("HallmarkData", new[] { reportData }));
            return report.Render("PDF");
        }

        public async Task<byte[]> GenerateMetalPurchaseReport(DateTime startDate, DateTime endDate)
        {
            var orders = await _orderService.GetOrdersByDate(startDate, endDate);
            using var report = new LocalReport();
            report.ReportPath = "MetalPurchaseReport.rdlc";

            var metalSummary = orders
                .SelectMany(o => o.OrderDetails)
                .GroupBy(od => new { od.Product.MetalType, od.Product.Purity })
                .Select(g => new
                {
                    MetalType = g.Key.MetalType,
                    Purity = g.Key.Purity,
                    TotalGrossWeight = g.Sum(od => od.GrossWeight),
                    TotalNetWeight = g.Sum(od => od.NetWeight),
                    AverageRate = g.Average(od => od.MetalRate),
                    TotalValue = g.Sum(od => od.BaseAmount)
                })
                .ToList();

            var exchangeMetals = orders
                .Where(o => o.HasMetalExchange)
                .GroupBy(o => new { MetalType = o.ExchangeMetalType, Purity = o.ExchangeMetalPurity.ToString() })
                .Select(g => new
                {
                    MetalType = g.Key.MetalType,
                    Purity = g.Key.Purity,
                    TotalWeight = g.Sum(o => o.ExchangeMetalWeight),
                    AverageRate = g.Average(o => o.ExchangeValue / o.ExchangeMetalWeight),
                    TotalValue = g.Sum(o => o.ExchangeValue)
                })
                .ToList();

            report.DataSources.Add(new ReportDataSource("PurchaseData", metalSummary));
            report.DataSources.Add(new ReportDataSource("ExchangeData", exchangeMetals));
            return report.Render("PDF");
        }

        public async Task<byte[]> GenerateGoldSchemeStatement(int customerId)
        {
            var customer = await _customerService.GetCustomerById(customerId);
            if (customer == null || !customer.IsGoldSchemeEnrolled) return null;

            using var report = new LocalReport();
            report.ReportPath = "GoldSchemeStatement.rdlc";

            var reportData = new
            {
                CustomerName = customer.CustomerName,
                SchemeStartDate = customer.RegistrationDate,
                TotalPurchases = customer.TotalPurchases,
                OutstandingAmount = customer.OutstandingAmount,
                LoyaltyPoints = customer.LoyaltyPoints,
                // Add more scheme-specific details here
            };

            report.DataSources.Add(new ReportDataSource("SchemeData", new[] { reportData }));
            return report.Render("PDF");
        }

        public async Task<byte[]> GenerateValuationCertificate(int productId)
        {
            var product = await _productService.GetProductById(productId);
            if (product == null) return null;

            using var report = new LocalReport();
            report.ReportPath = "ValuationCertificate.rdlc";

            var currentRate = await _rateService.GetCurrentRate(product.MetalType, product.Purity);

            var reportData = new
            {
                ProductName = product.ProductName,
                MetalType = product.MetalType,
                Purity = product.Purity,
                GrossWeight = product.GrossWeight,
                NetWeight = product.NetWeight,
                StoneDetails = product.StoneDetails,
                StoneWeight = product.StoneWeight,
                HallmarkNumber = product.HallmarkNumber,
                CurrentRate = currentRate?.Rate ?? 0,
                MetalValue = product.NetWeight * (currentRate?.Rate ?? 0),
                StoneValue = product.StoneValue,
                MakingCharges = product.MakingCharges,
                TotalValue = product.FinalPrice,
                ValuationDate = DateTime.Now,
                ValidUntil = DateTime.Now.AddMonths(3)
            };

            report.DataSources.Add(new ReportDataSource("ValuationData", new[] { reportData }));
            return report.Render("PDF");
        }

        public async Task<bool> PrintFile(byte[] content, string printerName = null)
        {
            try
            {
                // Save to temporary file
                var tempFile = Path.GetTempFileName();
                await File.WriteAllBytesAsync(tempFile, content);

                // Print using default printer or specified printer
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempFile,
                    Verb = "Print",
                    CreateNoWindow = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };

                using var process = System.Diagnostics.Process.Start(startInfo);
                process?.WaitForExit();

                // Cleanup
                File.Delete(tempFile);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}