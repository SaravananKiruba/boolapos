using ClosedXML.Excel;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Page_Navigation_App.Model;

namespace Page_Navigation_App.Services
{
    public class ExportService
    {
        private readonly ReportService _reportService;
        private readonly OrderService _orderService;
        private readonly StockService _stockService;
        private readonly string _exportPath;

        public ExportService(
            ReportService reportService,
            OrderService orderService,
            StockService stockService,
            string exportPath)
        {
            _reportService = reportService;
            _orderService = orderService;
            _stockService = stockService;
            _exportPath = exportPath;

            if (!Directory.Exists(_exportPath))
            {
                Directory.CreateDirectory(_exportPath);
            }
        }

       

        public async Task<string> ExportInventoryReport(string format = "xlsx")
        {
            var inventory = await _stockService.SearchStock(string.Empty);
            var fileName = $"InventoryReport_{DateTime.Now:yyyyMMdd}.{format}";
            var filePath = Path.Combine(_exportPath, fileName);

            if (format.ToLower() == "xlsx")
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Inventory");

                // Headers
                worksheet.Cell(1, 1).Value = "Product";
                worksheet.Cell(1, 2).Value = "Metal Type";
                worksheet.Cell(1, 3).Value = "Purity";
                worksheet.Cell(1, 4).Value = "Location";
                worksheet.Cell(1, 5).Value = "Quantity";
                worksheet.Cell(1, 6).Value = "Value";
                worksheet.Cell(1, 7).Value = "Status";

                // Data
                var row = 2;
                foreach (var item in inventory)
                {
                    worksheet.Cell(row, 1).Value = item.Product.ProductName;
                    worksheet.Cell(row, 2).Value = item.Product.MetalType;
                    worksheet.Cell(row, 3).Value = item.Product.Purity;
                    worksheet.Cell(row, 4).Value = item.Location;
                    worksheet.Cell(row, 5).Value = item.Quantity;
                    worksheet.Cell(row, 6).Value = item.Quantity * item.Product.ProductPrice;
                    worksheet.Cell(row, 7).Value = item.StockStatus;
                    row++;
                }

                workbook.SaveAs(filePath);
            }
            else if (format.ToLower() == "csv")
            {
                using var writer = new StreamWriter(filePath);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                var records = inventory.Select(i => new
                {
                    Product = i.Product.ProductName,
                    MetalType = i.Product.MetalType,
                    Purity = i.Product.Purity,
                    Location = i.Location,
                    Quantity = i.Quantity,
                    Value = i.Quantity * i.Product.ProductPrice,
                    Status = i.StockStatus
                });

                csv.WriteRecords(records);
            }

            return filePath;
        }

        public async Task<string> ExportGSTReport(
            DateOnly startDate,
            DateOnly endDate,
            string format = "xlsx")
        {
            var gstData = _reportService.GetFinancialReports(startDate, endDate);
            var fileName = $"GSTReport_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.{format}";
            var filePath = Path.Combine(_exportPath, fileName);

            if (format.ToLower() == "xlsx")
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("GST Report");

                // Headers
                worksheet.Cell(1, 2).Value = "Sales";
                worksheet.Cell(1, 3).Value = "Expenses"; 
                worksheet.Cell(1, 4).Value = "Profit/Loss";

                // Data
                worksheet.Cell(2, 1).Value = "Total";
                worksheet.Cell(2, 2).Value = gstData.TotalSales;
                worksheet.Cell(2, 3).Value = gstData.TotalExpenses;
                worksheet.Cell(2, 4).Value = gstData.GrossProfit;

               

                workbook.SaveAs(filePath);
            }
            else if (format.ToLower() == "csv")
            {
                using var writer = new StreamWriter(filePath);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                // Create a flattened record structure for CSV
                var records = new List<object>
                {
                    new { Category = "Total", Sales = gstData.TotalSales, Expenses = gstData.TotalExpenses, ProfitLoss = gstData.GrossProfit }
                };
                
              

                csv.WriteRecords(records);
            }

            return filePath;
        }

        public async Task<string> ExportCustomerList(string format = "xlsx")
        {
            // TODO: Implement customer export logic
            throw new NotImplementedException();
        }

        public async Task<string> ExportRepairReport(
            DateOnly startDate,
            DateOnly endDate,
            string format = "xlsx")
        {
            var repairs = _reportService.GetRepairAnalytics(startDate, endDate);
            var fileName = $"RepairReport_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.{format}";
            var filePath = Path.Combine(_exportPath, fileName);

            if (format.ToLower() == "xlsx")
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Repair Report");

                // Headers
                worksheet.Cell(1, 1).Value = "Job ID";
                worksheet.Cell(1, 2).Value = "Customer";
                worksheet.Cell(1, 3).Value = "Item Description";
                worksheet.Cell(1, 4).Value = "Work Type";
                worksheet.Cell(1, 5).Value = "Status";
                worksheet.Cell(1, 6).Value = "Receipt Date";
                worksheet.Cell(1, 7).Value = "Delivery Date";
                worksheet.Cell(1, 8).Value = "Estimated Cost";
                worksheet.Cell(1, 9).Value = "Final Amount";

                // Data
                var row = 2;
                foreach (var job in repairs.JobDetails)
                {
                    worksheet.Cell(row, 1).Value = job.RepairID;
                    worksheet.Cell(row, 2).Value = job.CustomerName;
                    worksheet.Cell(row, 3).Value = job.ItemDescription;
                    worksheet.Cell(row, 4).Value = job.WorkType;
                    worksheet.Cell(row, 5).Value = job.Status;
                    worksheet.Cell(row, 6).Value = job.ReceiptDate.ToDateTime(new TimeOnly(0, 0)); // Convert DateOnly to DateTime
                    worksheet.Cell(row, 7).Value = job.DeliveryDate.ToDateTime(new TimeOnly(0, 0)); ; // If DeliveryDate is already DateTime or null, keep it as is
                    worksheet.Cell(row, 8).Value = job.EstimatedCost;
                    worksheet.Cell(row, 9).Value = job.FinalAmount;
                    row++;
                }

                // Add summary section
                row += 2;
                worksheet.Cell(row, 1).Value = "Summary";
                row++;
                worksheet.Cell(row, 1).Value = "Total Jobs:";
                worksheet.Cell(row, 2).Value = repairs.TotalJobs;
                row++;
                worksheet.Cell(row, 1).Value = "Completed Jobs:";
                worksheet.Cell(row, 2).Value = repairs.CompletedJobs;
                row++;
                worksheet.Cell(row, 1).Value = "Pending Jobs:";
                worksheet.Cell(row, 2).Value = repairs.PendingJobs;
                row++;
                worksheet.Cell(row, 1).Value = "Total Revenue:";
                worksheet.Cell(row, 2).Value = repairs.TotalRevenue;

                workbook.SaveAs(filePath);
            }
            else if (format.ToLower() == "csv")
            {
                using var writer = new StreamWriter(filePath);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                var records = repairs.JobDetails.Select(job => new
                {
                    JobID = job.RepairID,
                    Customer = job.CustomerName,
                    ItemDescription = job.ItemDescription,
                    WorkType = job.WorkType,
                    Status = job.Status,
                    ReceiptDate = job.ReceiptDate,
                    DeliveryDate = job.DeliveryDate,
                    EstimatedCost = job.EstimatedCost,
                    FinalAmount = job.FinalAmount
                });

                csv.WriteRecords(records);
            }

            return filePath;
        }
    }
}