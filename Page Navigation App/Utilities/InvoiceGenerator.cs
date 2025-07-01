using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;
using Page_Navigation_App.Model;
using System.Reflection;

namespace Page_Navigation_App.Utilities
{
    public class InvoiceGenerator
    {
        // Register QuestPDF license (community license is free for commercial use up to $5000 annual revenue)
        static InvoiceGenerator()
        {
            QuestPDF.Settings.License = LicenseType.Community;
            
            // Disable glyph check to avoid issues with special characters like the Rupee symbol
            QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;
        }

        public static string GenerateInvoice(Order order, List<OrderDetail> orderDetails, string businessName, string businessAddress)
        {
            try
            {
                // Create the file path in the user's Documents folder
                string invoiceDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BOOLA_Invoices");
                if (!Directory.Exists(invoiceDirectory))
                {
                    Directory.CreateDirectory(invoiceDirectory);
                }

                string fileName = $"Invoice_Order_{order.OrderID}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                string filePath = Path.Combine(invoiceDirectory, fileName);

                // Generate the PDF
                Document.Create(container =>
                {                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(50);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                        page.Header().Element(header =>
                        {
                            header.Row(row =>
                            {
                                // Logo and business info
                                row.RelativeItem().Column(column =>
                                {
                                    column.Item().Text(businessName).FontSize(20).Bold();
                                    column.Item().Text(businessAddress).FontSize(10);
                                    column.Item().Text($"Phone: +91 9876543210");
                                    column.Item().Text($"Email: info@boola.com");
                                });

                                // Invoice details
                                row.RelativeItem().Column(column =>
                                {
                                    column.Item().AlignRight().Text($"INVOICE").FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                                    column.Item().AlignRight().Text($"Invoice #: {order.OrderID}");
                                    column.Item().AlignRight().Text($"Date: {order.OrderDate.ToString("dd/MM/yyyy")}");
                                    column.Item().AlignRight().Text($"Payment Method: {order.PaymentType}");
                                });
                            });
                        });

                        page.Content().Element(content =>
                        {
                            // Customer information
                            content.Column(column =>
                            {
                                column.Item().PaddingVertical(10).Row(row =>
                                {
                                    row.RelativeItem().Component(new CustomerComponent(order.Customer));
                                });

                                // Items table
                                column.Item().Element(table =>
                                {
                                    table.Table(table =>
                                    {
                                        // Define columns
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.ConstantColumn(30);
                                            columns.RelativeColumn(3);
                                            columns.RelativeColumn();
                                            columns.RelativeColumn();
                                            columns.RelativeColumn();
                                            columns.RelativeColumn();
                                        });

                                        // Table header
                                        table.Header(header =>
                                        {
                                            header.Cell().Border(1).Background(Colors.Grey.Lighten3).Text("#");
                                            header.Cell().Border(1).Background(Colors.Grey.Lighten3).Text("Product");
                                            header.Cell().Border(1).Background(Colors.Grey.Lighten3).Text("Type");
                                            header.Cell().Border(1).Background(Colors.Grey.Lighten3).Text("Purity");
                                            header.Cell().Border(1).Background(Colors.Grey.Lighten3).AlignRight().Text("Price");
                                            header.Cell().Border(1).Background(Colors.Grey.Lighten3).AlignRight().Text("Total");
                                        });

                                        // Table content
                    for (int i = 0; i < orderDetails.Count; i++)
                                        {
                                            var item = orderDetails[i];
                                            
                                            // Handle null product reference
                                            string productName = item.Product?.ProductName ?? "Unknown Product";
                                            string metalType = item.Product?.MetalType ?? "N/A";
                                            string purity = item.Product?.Purity ?? "N/A";
                                              table.Cell().Border(1).Text($"{i + 1}");
                                            table.Cell().Border(1).Text(productName);
                                            table.Cell().Border(1).Text(metalType);
                                            table.Cell().Border(1).Text(purity);
                                            table.Cell().Border(1).AlignRight().Text($"₹ {item.UnitPrice:N2}");
                                            table.Cell().Border(1).AlignRight().Text($"₹ {item.TotalAmount:N2}");
                                        }
                                    });
                                });

                                // Summary
                                column.Item().PaddingTop(20).AlignRight().Element(summary =>
                                {
                                    summary.Column(column =>
                                    {
                                        column.Item().Text(txt =>
                                        {                                            txt.Span("Subtotal: ").Bold();
                                            txt.Span($"₹ {order.TotalAmount:N2}");
                                        });

                                        column.Item().Text(txt =>
                                        {
                                            txt.Span("Discount: ").Bold();
                                            txt.Span($"₹ {order.DiscountAmount:N2}");
                                        });

                                        column.Item().Text(txt =>
                                        {
                                            txt.Span("Price Before Tax: ").Bold();
                                            txt.Span($"₹ {order.PriceBeforeTax:N2}");
                                        });

                                        column.Item().Text(txt =>
                                        {
                                            txt.Span("Tax (3%): ").Bold();
                                            txt.Span($"₹ {order.GrandTotal - order.PriceBeforeTax:N2}");
                                        });

                                        column.Item().BorderTop(1).BorderColor(Colors.Grey.Medium).PaddingTop(5).Text(txt =>
                                        {                                            txt.Span("Grand Total: ").Bold().FontSize(12);
                                            txt.Span($"₹ {order.GrandTotal:N2}").Bold().FontSize(12);
                                        });
                                    });
                                });
                            });
                        });

                        page.Footer().AlignCenter().Text(text =>
                        {
                            text.Span("Thank you for your business!").FontSize(12);
                            text.Span(" | ");
                            text.Span($"Generated on {DateTime.Now:dd/MM/yyyy HH:mm:ss}").FontSize(9);
                            text.Span(" | ");
                            text.Span("Page ").FontSize(9);
                            text.CurrentPageNumber().FontSize(9);
                            text.Span(" of ").FontSize(9);
                            text.TotalPages().FontSize(9);
                        });
                    });
                }).GeneratePdf(filePath);

                return filePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating invoice: {ex.Message}");
                throw;
            }
        }
    }

    // Component for customer information
    public class CustomerComponent : IComponent
    {
        private readonly Customer _customer;

        public CustomerComponent(Customer customer)
        {
            _customer = customer;
        }

        public void Compose(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Text("Bill To:").Bold();
                column.Item().Text(_customer?.CustomerName ?? "Unknown Customer");                column.Item().Text(_customer?.Address ?? "");
                column.Item().Text($"Phone: {_customer?.PhoneNumber ?? ""}");
                column.Item().Text($"Email: {_customer?.Email ?? ""}");
                column.Item().Text($"Customer Type: {_customer?.CustomerType ?? ""}");
            });
        }
    }
}
