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

        public PrintService(
            OrderService orderService,
            RepairJobService repairService,
            RateMasterService rateService)
        {
            _orderService = orderService;
            _repairService = repairService;
            _rateService = rateService;
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
                ReceiptNumber = repair.RepairJobID.ToString(),
                CustomerName = repair.Customer?.CustomerName,
                CustomerPhone = repair.Customer?.PhoneNumber,
                ReceiptDate = repair.ReceiptDate,
                ItemDetails = repair.ItemDetails,
                WorkType = repair.WorkType,
                EstimatedAmount = repair.EstimatedAmount,
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