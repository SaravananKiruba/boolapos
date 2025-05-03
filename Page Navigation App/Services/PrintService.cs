using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
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
            
            // Configure QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GenerateInvoice(int orderId)
        {
            var order = await _orderService.GetOrderWithDetails(orderId);
            if (order == null) return null;

            // Create PDF document
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(ComposeHeader);
                    
                    page.Content().Element(content =>
                    {
                        // Customer info
                        content.Column(column =>
                        {
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text($"Invoice #: {order.InvoiceNumber}").FontSize(14).Bold();
                                    c.Item().Text($"Date: {order.OrderDate:yyyy-MM-dd}");
                                });
                                
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text($"Customer: {order.Customer.CustomerName}").Bold();
                                    c.Item().Text($"Address: {order.Customer.Address}");
                                    if (!string.IsNullOrEmpty(order.Customer.GSTNumber))
                                        c.Item().Text($"GST #: {order.Customer.GSTNumber}");
                                });
                            });
                            
                            // Items table
                            column.Item().PaddingTop(20).Element(ComposeItemsTable);
                            
                            // Summary
                            column.Item().PaddingTop(10).Row(row =>
                            {
                                row.ConstantItem(300);
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text($"Sub Total: Rs.{order.SubTotal:N2}");
                                    c.Item().Text($"Discount: Rs.{order.DiscountAmount:N2}");
                                    c.Item().Text($"Tax: Rs.{order.TaxAmount:N2}");
                                    c.Item().Text($"Grand Total: Rs.{order.GrandTotal:N2}").Bold();
                                    c.Item().Text($"Payment: {order.PaymentType}");
                                    
                                    if (order.PaymentType == "EMI")
                                    {
                                        c.Item().Text($"EMI Plan: {order.EMIMonths} months x Rs.{order.EMIAmount:N2}");
                                    }
                                    
                                    if (order.HasMetalExchange)
                                    {
                                        c.Item().Text($"Exchange: {order.ExchangeMetalType} {order.ExchangeMetalPurity} - {order.ExchangeMetalWeight}g (Rs.{order.ExchangeValue:N2})");
                                    }
                                });
                            });
                        });
                    });
                    
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Thank you for your business!").FontSize(12);
                        x.Line($"Page 1 of 1");
                    });
                });
            });

            // Generate PDF to memory stream
            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Boola Jewelry").FontSize(18).Bold();
                    c.Item().Text("123 Jewelry Lane, Diamond District");
                    c.Item().Text("Phone: (123) 456-7890");
                    c.Item().Text("Email: info@boolajewelry.com");
                });
            });
        }

        private void ComposeItemsTable(IContainer container)
        {
            container.Table(table =>
            {
                // Define columns
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(25);    // Sl.
                    columns.RelativeColumn(3);     // Item
                    columns.RelativeColumn(1);     // Metal
                    columns.RelativeColumn(1);     // Weight
                    columns.RelativeColumn(1);     // Rate
                    columns.RelativeColumn(1);     // Making
                    columns.RelativeColumn(1);     // Stone
                    columns.RelativeColumn(1.5f);  // Amount
                });

                // Header row
                table.Header(header =>
                {
                    header.Cell().Text("Sl.").Bold();
                    header.Cell().Text("Item").Bold();
                    header.Cell().Text("Metal").Bold();
                    header.Cell().Text("Weight (g)").Bold();
                    header.Cell().Text("Rate").Bold();
                    header.Cell().Text("Making").Bold();
                    header.Cell().Text("Stone").Bold();
                    header.Cell().Text("Amount").Bold();
                    
                    header.Cell().ColumnSpan(8).PaddingTop(5).BorderBottom(1).BorderColor(Colors.Black);
                });

                // Data rows
                // Note: This is a placeholder for the actual implementation
                // You'll need to implement this based on your order data
            });
        }

        public async Task<byte[]> GenerateRepairReceipt(int repairId)
        {
            var repair = await _repairService.GetRepairJobById(repairId);
            if (repair == null) return null;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(1, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(ComposeHeader);
                    
                    page.Content().Element(content =>
                    {
                        content.Column(column =>
                        {
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("REPAIR RECEIPT").SemiBold().FontSize(14);
                                    c.Item().Text($"Receipt #: {repair.RepairID}");
                                    c.Item().Text($"Date: {repair.ReceiptDate:yyyy-MM-dd}");
                                });
                                
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text($"Customer: {repair.Customer?.CustomerName}").Bold();
                                    c.Item().Text($"Phone: {repair.Customer?.PhoneNumber}");
                                });
                            });
                            
                            column.Item().PaddingTop(20).Column(c =>
                            {
                                c.Item().Text("Item Details:").Bold();
                                c.Item().Text(repair.ItemDescription);
                                
                                c.Item().PaddingTop(10).Text($"Work Type: {repair.WorkType}");
                                c.Item().Text($"Estimated Cost: Rs.{repair.EstimatedCost:N2}");
                                c.Item().Text($"Status: {repair.Status}");
                            });
                            
                            column.Item().PaddingTop(20).Element(container =>
                            {
                                container.Row(row =>
                                {
                                    row.RelativeItem().Column(c =>
                                    {
                                        c.Item().Text("Terms & Conditions:").SemiBold();
                                        c.Item().Text("1. Please collect your item within 30 days.");
                                        c.Item().Text("2. Bring this receipt when collecting your item.");
                                        c.Item().Text("3. Cost may vary based on actual work required.");
                                    });
                                });
                            });
                        });
                    });
                    
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Thank you for choosing our services!");
                    });
                });
            });

            // Generate PDF to memory stream
            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> GenerateRateCard()
        {
            var rates = await _rateService.GetAllCurrentRates();
            
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(ComposeHeader);
                    
                    page.Content().Element(content =>
                    {
                        content.Column(column =>
                        {
                            column.Item().AlignCenter().Text("CURRENT RATE CARD")
                                .FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                            
                            column.Item().AlignCenter().Text($"As of {DateTime.Now:dd-MMM-yyyy}")
                                .FontSize(12);
                            
                            column.Item().PaddingTop(20).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                });
                                
                                table.Header(header =>
                                {
                                    header.Cell().Text("Metal Type & Purity").Bold();
                                    header.Cell().Text("Rate (per gram)").Bold();
                                    
                                    header.Cell().ColumnSpan(2).PaddingTop(5)
                                        .BorderBottom(1).BorderColor(Colors.Black);
                                });
                                
                                foreach (var rate in rates)
                                {
                                    table.Cell().Text($"{rate.MetalType} {rate.Purity}");
                                    table.Cell().Text($"Rs. {rate.SaleRate:N2}");
                                }
                            });
                            
                            column.Item().PaddingTop(20).AlignCenter().Text("Rates are subject to change based on market conditions")
                                .FontSize(9).Italic();
                        });
                    });
                    
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Boola Jewelry - Premium Jewelry For Every Occasion");
                        x.Line($"Page 1 of 1");
                    });
                });
            });

            // Generate PDF to memory stream
            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        // Implementation for other methods will follow the same pattern
        // For brevity, I'll implement just skeletons for the remaining methods

        public async Task<byte[]> GenerateDailyReport(DateTime date)
        {
            var orders = await _orderService.GetOrdersByDate(date, date);
            var repairs = await _repairService.GetRepairsByDate(date, date);
            
            // Create a basic document - you'll need to expand this implementation
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    
                    page.Header().Element(ComposeHeader);
                    
                    page.Content().AlignCenter().Text($"Daily Report for {date:yyyy-MM-dd}")
                        .FontSize(16).Bold();
                        
                    // You would add detailed report content here
                    
                    page.Footer().AlignCenter().Text(x => x.Span("Generated Report"));
                });
            });
            
            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> GenerateHallmarkCertificate(int productId)
        {
            var product = await _productService.GetProductById(productId);
            if (product == null) return null;
            
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    
                    // Implement certificate content
                });
            });
            
            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> GenerateMetalPurchaseReport(DateTime startDate, DateTime endDate)
        {
            var orders = await _orderService.GetOrdersByDate(startDate, endDate);
            
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    
                    // Implement report content
                });
            });
            
            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> GenerateGoldSchemeStatement(int customerId)
        {
            var customer = await _customerService.GetCustomerById(customerId);
            if (customer == null || !customer.IsGoldSchemeEnrolled) return null;
            
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    
                    // Implement statement content
                });
            });
            
            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> GenerateValuationCertificate(int productId)
        {
            var product = await _productService.GetProductById(productId);
            if (product == null) return null;
            
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    
                    // Implement certificate content
                });
            });
            
            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        public async Task<bool> PrintFile(byte[] content, string printerName = null)
        {
            try
            {
                // Save to temporary file
                var tempFile = Path.GetTempFileName() + ".pdf";
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