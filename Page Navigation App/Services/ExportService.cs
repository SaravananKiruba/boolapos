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

        public async Task<string> ExportSalesReport(
            DateTime startDate,
            DateTime endDate,
            string format = "xlsx")
        {
            var analytics = await _reportService.GetSalesAnalytics(startDate, endDate);
            var fileName = $"SalesReport_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.{format}";
            var filePath = Path.Combine(_exportPath, fileName);

            if (format.ToLower() == "xlsx")
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Sales Report");

                // Add headers
                worksheet.Cell(1, 1).Value = "Category";
                worksheet.Cell(1, 2).Value = "Amount";

                // Add data
                var row = 2;
                foreach (var item in analytics)
                {
                    worksheet.Cell(row, 1).Value = item.Key;
                    worksheet.Cell(row, 2).Value = item.Value;
                    row++;
                }

                workbook.SaveAs(filePath);
            }
            else if (format.ToLower() == "csv")
            {
                using var writer = new StreamWriter(filePath);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                var records = analytics.Select(a => new
                {
                    Category = a.Key,
                    Amount = a.Value
                });

                csv.WriteRecords(records);
            }

            return filePath;
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
                    worksheet.Cell(row, 6).Value = item.Quantity * item.Product.BasePrice;
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
                    Value = i.Quantity * i.Product.BasePrice,
                    Status = i.StockStatus
                });

                csv.WriteRecords(records);
            }

            return filePath;
        }

        public async Task<string> ExportGSTReport(
            DateTime startDate,
            DateTime endDate,
            string format = "xlsx")
        {
            var gstData = await _reportService.GetFinancialReports(startDate, endDate);
            var fileName = $"GSTReport_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.{format}";
            var filePath = Path.Combine(_exportPath, fileName);

            if (format.ToLower() == "xlsx")
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("GST Report");

                // Headers
                worksheet.Cell(1, 1).Value = "Tax Type";
                worksheet.Cell(1, 2).Value = "Amount";

                // Data
                worksheet.Cell(2, 1).Value = "CGST";
                worksheet.Cell(2, 2).Value = gstData["CGST_Collected"];
                worksheet.Cell(3, 1).Value = "SGST";
                worksheet.Cell(3, 2).Value = gstData["SGST_Collected"];
                worksheet.Cell(4, 1).Value = "IGST";
                worksheet.Cell(4, 2).Value = gstData["IGST_Collected"];

                workbook.SaveAs(filePath);
            }
            else if (format.ToLower() == "csv")
            {
                using var writer = new StreamWriter(filePath);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                var records = new[]
                {
                    new { TaxType = "CGST", Amount = gstData["CGST_Collected"] },
                    new { TaxType = "SGST", Amount = gstData["SGST_Collected"] },
                    new { TaxType = "IGST", Amount = gstData["IGST_Collected"] }
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
            DateTime startDate,
            DateTime endDate,
            string format = "xlsx")
        {
            var repairs = await _reportService.GetRepairAnalytics(startDate, endDate);
            var fileName = $"RepairReport_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.{format}";
            var filePath = Path.Combine(_exportPath, fileName);

            if (format.ToLower() == "xlsx")
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Repair Report");

                // Headers
                worksheet.Cell(1, 1).Value = "Work Type";
                worksheet.Cell(1, 2).Value = "Count";
                worksheet.Cell(1, 3).Value = "Estimated Amount";
                worksheet.Cell(1, 4).Value = "Final Amount";

                // Data
                var row = 2;
                foreach (var repair in repairs)
                {
                    worksheet.Cell(row, 1).Value = repair.WorkType;
                    worksheet.Cell(row, 2).Value = repair.Status; // Using Status field for count
                    worksheet.Cell(row, 3).Value = repair.EstimatedAmount;
                    worksheet.Cell(row, 4).Value = repair.FinalAmount;
                    row++;
                }

                workbook.SaveAs(filePath);
            }
            else if (format.ToLower() == "csv")
            {
                using var writer = new StreamWriter(filePath);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                var records = repairs.Select(r => new
                {
                    WorkType = r.WorkType,
                    Count = r.Status, // Using Status field for count
                    EstimatedAmount = r.EstimatedAmount,
                    FinalAmount = r.FinalAmount
                });

                csv.WriteRecords(records);
            }

            return filePath;
        }
    }
}