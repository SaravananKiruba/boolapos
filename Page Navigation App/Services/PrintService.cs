using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;

namespace Page_Navigation_App.Services
{
    /// <summary>
    /// Service to handle printing of invoices, reports, tags, and HUID certificates
    /// </summary>
    public class PrintService
    {
        private readonly AppDbContext _context;
        private readonly LogService _logService;
        private readonly string _templatePath;

        public PrintService(AppDbContext context, LogService logService)
        {
            _context = context;
            _logService = logService;
            _templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
            
            // Ensure templates directory exists
            if (!Directory.Exists(_templatePath))
            {
                Directory.CreateDirectory(_templatePath);
            }
        }

        /// <summary>
        /// Generate and print a sales invoice
        /// </summary>
        public async Task<string> GenerateInvoiceAsync(int orderId, bool isPrintPreview = false)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.OrderID == orderId);

                if (order == null)
                {
                    await _logService.LogErrorAsync($"Order not found with ID: {orderId}");
                    return null;
                }

                // Get business info for invoice header
                var businessInfo = await _context.BusinessInfo.FirstOrDefaultAsync();
                if (businessInfo == null)
                {
                    await _logService.LogWarningAsync("Business information not found for invoice");
                }

                // Create HTML content for invoice
                string invoiceHtml = await GenerateInvoiceHtmlAsync(order, businessInfo);
                
                // Path to save the invoice file
                string fileName = $"Invoice_{order.InvoiceNumber?.Replace("/", "-")}.html";
                string filePath = Path.Combine(_templatePath, fileName);
                
                // Save HTML to file
                await File.WriteAllTextAsync(filePath, invoiceHtml);
                
                if (!isPrintPreview)
                {
                    // Trigger printing (implementation would depend on your printer setup)
                    // This is a placeholder for actual printing code
                    await _logService.LogInformationAsync($"Invoice printed for Order: {orderId}");
                }

                await _logService.LogInformationAsync($"Invoice generated for Order: {orderId}, File: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error generating invoice: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Generate HTML for an invoice
        /// </summary>
        private async Task<string> GenerateInvoiceHtmlAsync(Order order, BusinessInfo businessInfo)
        {
            string huidList = string.IsNullOrEmpty(order.HUIDReferences) ? "N/A" : order.HUIDReferences;
            string tagList = string.IsNullOrEmpty(order.TagReferences) ? "N/A" : order.TagReferences;
            
            string html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Invoice #{order.InvoiceNumber}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; }}
        .invoice-header {{ display: flex; justify-content: space-between; margin-bottom: 20px; }}
        .business-info {{ width: 50%; }}
        .invoice-info {{ width: 50%; text-align: right; }}
        .customer-info {{ margin-bottom: 20px; }}
        table {{ width: 100%; border-collapse: collapse; }}
        table th, table td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        table th {{ background-color: #f2f2f2; }}
        .totals {{ margin-top: 20px; text-align: right; }}
        .footer {{ margin-top: 50px; text-align: center; font-size: 12px; }}
        .gst-info {{ margin-top: 20px; border-top: 1px solid #ddd; padding-top: 10px; }}
    </style>
</head>
<body>
    <div class='invoice-header'>
        <div class='business-info'>
            <h2>{businessInfo?.BusinessName ?? "Jewelry Shop"}</h2>
            <p>{businessInfo?.Address ?? ""}</p>
            <p>Phone: {businessInfo?.PhoneNumber ?? ""}</p>
            <p>GST No: {businessInfo?.GSTNumber ?? ""}</p>
        </div>
        <div class='invoice-info'>
            <h1>TAX INVOICE</h1>
            <p>Invoice #: {order.InvoiceNumber}</p>
            <p>Date: {order.OrderDate:dd-MMM-yyyy}</p>
            <p>HUID: {huidList}</p>
            <p>Tag#: {tagList}</p>
        </div>
    </div>
    
    <div class='customer-info'>
        <h3>Bill To:</h3>
        <p>Customer: {order.Customer?.CustomerName}</p>
        <p>Address: {order.Customer?.Address}</p>
        <p>Phone: {order.Customer?.PhoneNumber}</p>
        {(!string.IsNullOrEmpty(order.Customer?.GSTNumber) ? $"<p>GST No: {order.Customer?.GSTNumber}</p>" : "")}
    </div>
    
    <table>
        <thead>
            <tr>
                <th>#</th>
                <th>Description</th>
                <th>HSN Code</th>
                <th>Metal</th>
                <th>Weight (g)</th>
                <th>Qty</th>
                <th>Unit Price</th>
                <th>Total</th>
            </tr>
        </thead>
        <tbody>";

            int itemNo = 1;
            foreach (var item in order.OrderDetails)
            {
                html += $@"
            <tr>
                <td>{itemNo++}</td>
                <td>{item.Product.ProductName}</td>
                <td>{order.HSNCode ?? "7113"}</td>
                <td>{item.Product.MetalType} {item.Product.Purity}</td>
                <td>{item.Product.GrossWeight}</td>
                <td>{item.Quantity}</td>
                <td>₹{item.UnitPrice:N2}</td>
                <td>₹{item.TotalPrice:N2}</td>
            </tr>";
            }

            html += $@"
        </tbody>
    </table>
    
    <div class='totals'>
        <p>Subtotal: ₹{order.TotalAmount:N2}</p>
        <p>Discount: ₹{order.DiscountAmount:N2}</p>
        <p>CGST @1.5%: ₹{order.CGST:N2}</p>
        <p>SGST @1.5%: ₹{order.SGST:N2}</p>
        <p>IGST @3%: ₹{order.IGST:N2}</p>
        <p><strong>Grand Total: ₹{order.GrandTotal:N2}</strong></p>
        {(order.HasMetalExchange ? $@"
        <p>Metal Exchange: {order.ExchangeMetalType} {order.ExchangeMetalPurity}</p>
        <p>Exchange Weight: {order.ExchangeMetalWeight:N3}g</p>
        <p>Exchange Value: ₹{order.ExchangeValue:N2}</p>" : "")}
    </div>
    
    <div class='gst-info'>
        <p>Payment Method: {order.PaymentMethod}</p>
        {(order.EMIMonths > 0 ? $"<p>EMI Plan: {order.EMIMonths} months x ₹{order.EMIAmount:N2}</p>" : "")}
        <p>Declaration: Goods once sold will not be taken back or exchanged. All disputes subject to local jurisdiction.</p>
    </div>
    
    <div class='footer'>
        <p>Thank you for your business!</p>
        <p>{businessInfo?.TagLine ?? "Quality Jewelry Since 1990"}</p>
    </div>
</body>
</html>";

            return html;
        }

        /// <summary>
        /// Generate and print a repair job receipt
        /// </summary>
        public async Task<string> GenerateRepairReceiptAsync(int repairId, bool isPrintPreview = false)
        {
            try
            {
                var repair = await _context.RepairJobs
                    .Include(r => r.Customer)
                    .FirstOrDefaultAsync(r => r.RepairID == repairId);

                if (repair == null)
                {
                    await _logService.LogErrorAsync($"Repair job not found with ID: {repairId}");
                    return null;
                }

                // Get business info for receipt header
                var businessInfo = await _context.BusinessInfo.FirstOrDefaultAsync();

                // Create HTML content for repair receipt
                string repairHtml = await GenerateRepairReceiptHtmlAsync(repair, businessInfo);
                
                // Path to save the receipt file
                string fileName = $"RepairReceipt_{repairId}.html";
                string filePath = Path.Combine(_templatePath, fileName);
                
                // Save HTML to file
                await File.WriteAllTextAsync(filePath, repairHtml);
                
                if (!isPrintPreview)
                {
                    // Trigger printing (implementation would depend on your printer setup)
                    await _logService.LogInformationAsync($"Repair receipt printed for Repair ID: {repairId}");
                }

                await _logService.LogInformationAsync($"Repair receipt generated for Repair ID: {repairId}, File: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error generating repair receipt: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Generate HTML for a repair receipt
        /// </summary>
        private async Task<string> GenerateRepairReceiptHtmlAsync(RepairJob repair, BusinessInfo businessInfo)
        {
            string htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Repair Receipt #{repair.RepairID}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; }}
        .receipt-header {{ display: flex; justify-content: space-between; margin-bottom: 20px; }}
        .business-info {{ width: 50%; }}
        .receipt-info {{ width: 50%; text-align: right; }}
        .customer-info {{ margin-bottom: 20px; }}
        .repair-details {{ margin-bottom: 20px; }}
        .terms {{ margin-top: 20px; border-top: 1px solid #ddd; padding-top: 10px; }}
        .signatures {{ margin-top: 40px; display: flex; justify-content: space-between; }}
        .signature-line {{ width: 45%; border-top: 1px solid #000; padding-top: 5px; text-align: center; }}
        .footer {{ margin-top: 50px; text-align: center; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='receipt-header'>
        <div class='business-info'>
            <h2>{businessInfo?.BusinessName ?? "Jewelry Shop"}</h2>
            <p>{businessInfo?.Address ?? ""}</p>
            <p>Phone: {businessInfo?.PhoneNumber ?? ""}</p>
        </div>
        <div class='receipt-info'>
            <h1>REPAIR RECEIPT</h1>
            <p>Receipt #: R-{repair.RepairID}</p>
            <p>Date: {repair.ReceiptDate:dd-MMM-yyyy}</p>
            <p>Expected Delivery: {repair.EstimatedDeliveryDate?.ToString("dd-MMM-yyyy") ?? "To be determined"}</p>
            <p>Status: {repair.Status}</p>
        </div>
    </div>
    
    <div class='customer-info'>
        <h3>Customer Information:</h3>
        <p>Name: {repair.Customer?.CustomerName}</p>
        <p>Phone: {repair.Customer?.PhoneNumber}</p>
        <p>Address: {repair.Customer?.Address}</p>
    </div>
    
    <div class='repair-details'>
        <h3>Item Details:</h3>
        <p>Description: {repair.ItemDescription}</p>
        <p>Metal Type: {repair.MetalType ?? "N/A"}</p>
        <p>Purity: {repair.Purity ?? "N/A"}</p>
        <p>Weight: {repair.ItemWeight}g</p>
        <p>Work Type: {repair.WorkType}</p>
        <p>Work Description: {repair.WorkDescription}</p>
        {(!string.IsNullOrEmpty(repair.StoneDetails) ? $"<p>Stone Details: {repair.StoneDetails}</p>" : "")}
        <p>Estimated Cost: ₹{repair.EstimatedCost:N2}</p>
        {(repair.AdvanceAmount > 0 ? $"<p>Advance Received: ₹{repair.AdvanceAmount:N2}</p>" : "")}
        <p>Final Amount: {(repair.FinalAmount > 0 ? $"₹{repair.FinalAmount:N2}" : "To be determined")}</p>
    </div>
    
    <div class='terms'>
        <h3>Terms & Conditions:</h3>
        <ol>
            <li>Customer must present this receipt when collecting the item.</li>
            <li>The shop is not responsible for items not collected within 90 days of completion.</li>
            <li>Any change in design or additional work will incur extra charges.</li>
            <li>Metal or stone replacement will be charged separately.</li>
            <li>Final charges may vary based on actual work required.</li>
        </ol>
    </div>
    
    <div class='signatures'>
        <div class='signature-line'>Customer's Signature</div>
        <div class='signature-line'>Authorized Signature</div>
    </div>
    
    <div class='footer'>
        <p>Thank you for choosing us for your jewelry repair needs!</p>
        <p>{businessInfo?.TagLine ?? "Quality Jewelry Service Since 1990"}</p>
    </div>
</body>
</html>";

            return htmlContent;
        }

        /// <summary>
        /// Generate and print product tags with barcode/RFID
        /// </summary>
        public async Task<string> GenerateProductTagAsync(int productId, bool isPrintPreview = false)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.ProductID == productId);

                if (product == null)
                {
                    await _logService.LogErrorAsync($"Product not found with ID: {productId}");
                    return null;
                }

                // Ensure product has a tag number
                if (string.IsNullOrEmpty(product.TagNumber))
                {
                    await _logService.LogErrorAsync($"Product {productId} does not have a tag number assigned");
                    return null;
                }

                // Create HTML content for tag
                string tagHtml = GenerateTagHtml(product);
                
                // Path to save the tag file
                string fileName = $"Tag_{product.TagNumber.Replace("/", "-")}.html";
                string filePath = Path.Combine(_templatePath, fileName);
                
                // Save HTML to file
                await File.WriteAllTextAsync(filePath, tagHtml);
                
                if (!isPrintPreview)
                {
                    // Trigger printing (implementation would depend on your printer setup)
                    await _logService.LogInformationAsync($"Tag printed for Product: {productId}");
                }

                await _logService.LogInformationAsync($"Tag generated for Product: {productId}, File: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error generating product tag: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Generate HTML for a product tag
        /// </summary>
        private string GenerateTagHtml(Product product)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Product Tag - {product.TagNumber}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; }}
        .tag-container {{ width: 250px; height: 150px; border: 1px solid #000; padding: 10px; margin: 10px; }}
        .tag-header {{ text-align: center; font-weight: bold; font-size: 14px; margin-bottom: 5px; }}
        .tag-content {{ font-size: 12px; }}
        .tag-barcode {{ text-align: center; margin: 5px 0; }}
        .tag-barcode img {{ width: 90%; height: 40px; }}
        .tag-footer {{ font-size: 10px; text-align: center; margin-top: 5px; }}
    </style>
</head>
<body>
    <div class='tag-container'>
        <div class='tag-header'>{product.Category?.CategoryName ?? "Jewelry"}</div>
        <div class='tag-content'>
            <p><strong>Name:</strong> {product.ProductName}</p>
            <p><strong>Metal:</strong> {product.MetalType} {product.Purity}</p>
            <p><strong>Weight:</strong> {product.GrossWeight}g</p>
            <p><strong>ID:</strong> {product.ProductID}</p>
            {(!string.IsNullOrEmpty(product.HUID) ? $"<p><strong>HUID:</strong> {product.HUID}</p>" : "")}
        </div>
        <div class='tag-barcode'>
            <!-- Barcode would be generated and inserted here -->
            {product.TagNumber}
        </div>
        <div class='tag-footer'>
            <p>Price: ₹{product.FinalPrice:N2}</p>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Generate and print a report based on report data
        /// </summary>
        public async Task<string> GenerateReportAsync(string reportType, object reportData, bool isPrintPreview = false)
        {
            try
            {
                string reportHtml;
                string title;

                switch (reportType.ToLower())
                {
                    case "inventory":
                        reportHtml = GenerateInventoryReportHtml((InventoryReport)reportData);
                        title = "Inventory Report";
                        break;
                    case "sales":
                        var salesReport = reportData as DashboardData;
                        reportHtml = GenerateSalesReportHtml(salesReport);
                        title = "Sales Report";
                        break;
                    case "gst":
                        var gstReport = reportData as GSTReport;
                        reportHtml = GenerateGstReportHtml(gstReport);
                        title = "GST Report";
                        break;
                    case "customer":
                        var customerReport = reportData as CustomerReport;
                        reportHtml = GenerateCustomerReportHtml(customerReport);
                        title = "Customer Report";
                        break;
                    default:
                        await _logService.LogErrorAsync($"Unsupported report type: {reportType}");
                        return null;
                }

                // Path to save the report file
                string fileName = $"{title.Replace(" ", "")}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
                string filePath = Path.Combine(_templatePath, fileName);
                
                // Save HTML to file
                await File.WriteAllTextAsync(filePath, reportHtml);
                
                if (!isPrintPreview)
                {
                    // Trigger printing (implementation would depend on your printer setup)
                    await _logService.LogInformationAsync($"{title} printed");
                }

                await _logService.LogInformationAsync($"{title} generated, File: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error generating report: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Generate HTML for an inventory report
        /// </summary>
        private string GenerateInventoryReportHtml(InventoryReport report)
        {
            string productRows = "";
            foreach (var product in report.ProductDetails)
            {
                productRows += $@"
                <tr>
                    <td>{product.ProductID}</td>
                    <td>{product.ProductName}</td>
                    <td>{product.Category}</td>
                    <td>{product.MetalType} {product.Purity}</td>
                    <td>{product.GrossWeight:N3}g</td>
                    <td>{product.NetWeight:N3}g</td>
                    <td>{product.StockQuantity}</td>
                    <td>₹{product.Value:N2}</td>
                    <td>{product.HUID ?? "N/A"}</td>
                </tr>";
            }

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Inventory Report</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; }}
        h1, h2 {{ text-align: center; }}
        .summary {{ display: flex; justify-content: space-around; margin: 20px 0; }}
        .summary-item {{ text-align: center; padding: 10px; border: 1px solid #ddd; width: 150px; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
        table th, table td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        table th {{ background-color: #f2f2f2; }}
        .footer {{ margin-top: 30px; text-align: center; font-size: 12px; }}
    </style>
</head>
<body>
    <h1>Inventory Report</h1>
    <p style='text-align: center;'>Generated on {DateTime.Now:dd-MMM-yyyy HH:mm}</p>
    
    <div class='summary'>
        <div class='summary-item'>
            <h3>Total Products</h3>
            <p>{report.TotalProducts}</p>
        </div>
        <div class='summary-item'>
            <h3>Total Value</h3>
            <p>₹{report.TotalValue:N2}</p>
        </div>
        <div class='summary-item'>
            <h3>Gold Items</h3>
            <p>{report.GoldItems} ({report.GoldWeight:N3}g)</p>
        </div>
        <div class='summary-item'>
            <h3>Silver Items</h3>
            <p>{report.SilverItems} ({report.SilverWeight:N3}g)</p>
        </div>
        <div class='summary-item'>
            <h3>Low Stock</h3>
            <p>{report.LowStockItems}</p>
        </div>
    </div>
    
    <h2>Product Details</h2>
    <table>
        <thead>
            <tr>
                <th>ID</th>
                <th>Name</th>
                <th>Category</th>
                <th>Metal & Purity</th>
                <th>Gross Weight</th>
                <th>Net Weight</th>
                <th>Stock</th>
                <th>Value</th>
                <th>HUID</th>
            </tr>
        </thead>
        <tbody>
            {productRows}
        </tbody>
    </table>
    
    <div class='footer'>
        <p>This is a computer-generated report.</p>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Generate HTML for a sales report
        /// </summary>
        private string GenerateSalesReportHtml(DashboardData report)
        {
            // Generate top selling products rows
            string topProductsRows = "";
            foreach (var product in report.TopSellingProducts)
            {
                topProductsRows += $@"
                <tr>
                    <td>{product.ProductName}</td>
                    <td>{product.Quantity}</td>
                    <td>₹{product.Amount:N2}</td>
                </tr>";
            }

            // Generate monthly sales rows
            string monthlySalesRows = "";
            foreach (var month in report.MonthlySalesData)
            {
                monthlySalesRows += $@"
                <tr>
                    <td>{month.Month}</td>
                    <td>₹{month.Amount:N2}</td>
                </tr>";
            }

            // Generate category sales rows
            string categorySalesRows = "";
            foreach (var category in report.SalesByCategory)
            {
                categorySalesRows += $@"
                <tr>
                    <td>{category.CategoryName}</td>
                    <td>₹{category.Amount:N2}</td>
                </tr>";
            }

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Sales Report</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; }}
        h1, h2 {{ text-align: center; }}
        .summary {{ display: flex; justify-content: space-around; margin: 20px 0; }}
        .summary-item {{ text-align: center; padding: 10px; border: 1px solid #ddd; width: 150px; }}
        .report-section {{ margin-top: 30px; }}
        table {{ width: 100%; border-collapse: collapse; }}
        table th, table td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        table th {{ background-color: #f2f2f2; }}
        .footer {{ margin-top: 30px; text-align: center; font-size: 12px; }}
    </style>
</head>
<body>
    <h1>Sales Report</h1>
    <p style='text-align: center;'>Generated on {DateTime.Now:dd-MMM-yyyy HH:mm}</p>
    
    <div class='summary'>
        <div class='summary-item'>
            <h3>Total Sales</h3>
            <p>₹{report.TotalSales:N2}</p>
        </div>
        <div class='summary-item'>
            <h3>Order Count</h3>
            <p>{report.OrderCount}</p>
        </div>
        <div class='summary-item'>
            <h3>New Customers</h3>
            <p>{report.NewCustomers}</p>
        </div>
        <div class='summary-item'>
            <h3>Gold Sales</h3>
            <p>₹{report.GoldSalesAmount:N2}</p>
        </div>
        <div class='summary-item'>
            <h3>Silver Sales</h3>
            <p>₹{report.SilverSalesAmount:N2}</p>
        </div>
    </div>
    
    <div class='report-section'>
        <h2>Top Selling Products</h2>
        <table>
            <thead>
                <tr>
                    <th>Product</th>
                    <th>Quantity</th>
                    <th>Amount</th>
                </tr>
            </thead>
            <tbody>
                {topProductsRows}
            </tbody>
        </table>
    </div>
    
    <div class='report-section'>
        <h2>Monthly Sales</h2>
        <table>
            <thead>
                <tr>
                    <th>Month</th>
                    <th>Amount</th>
                </tr>
            </thead>
            <tbody>
                {monthlySalesRows}
            </tbody>
        </table>
    </div>
    
    <div class='report-section'>
        <h2>Sales by Category</h2>
        <table>
            <thead>
                <tr>
                    <th>Category</th>
                    <th>Amount</th>
                </tr>
            </thead>
            <tbody>
                {categorySalesRows}
            </tbody>
        </table>
    </div>
    
    <div class='footer'>
        <p>This is a computer-generated report.</p>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Generate HTML for a GST report
        /// </summary>
        private string GenerateGstReportHtml(GSTReport report)
        {
            // Generate invoice rows
            string invoiceRows = "";
            foreach (var invoice in report.InvoiceDetails)
            {
                invoiceRows += $@"
                <tr>
                    <td>{invoice.InvoiceNumber}</td>
                    <td>{invoice.InvoiceDate:dd-MMM-yyyy}</td>
                    <td>{invoice.CustomerName}</td>
                    <td>{invoice.CustomerGST ?? "N/A"}</td>
                    <td>{invoice.HSNCode ?? "7113"}</td>
                    <td>₹{invoice.TaxableValue:N2}</td>
                    <td>₹{invoice.CGST:N2}</td>
                    <td>₹{invoice.SGST:N2}</td>
                    <td>₹{invoice.IGST:N2}</td>
                    <td>₹{invoice.Total:N2}</td>
                </tr>";
            }

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>GST Report</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; }}
        h1, h2 {{ text-align: center; }}
        .summary {{ display: flex; justify-content: space-around; flex-wrap: wrap; margin: 20px 0; }}
        .summary-item {{ text-align: center; padding: 10px; border: 1px solid #ddd; width: 150px; margin: 5px; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
        table th, table td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        table th {{ background-color: #f2f2f2; }}
        .footer {{ margin-top: 30px; text-align: center; font-size: 12px; }}
        .period {{ text-align: center; font-weight: bold; margin-bottom: 20px; }}
    </style>
</head>
<body>
    <h1>GST Report</h1>
    <p style='text-align: center;'>Generated on {DateTime.Now:dd-MMM-yyyy HH:mm}</p>
    
    <div class='period'>
        Period: {report.FromDate:dd-MMM-yyyy} to {report.ToDate:dd-MMM-yyyy}
    </div>
    
    <div class='summary'>
        <div class='summary-item'>
            <h3>Total Invoices</h3>
            <p>{report.TotalInvoices}</p>
        </div>
        <div class='summary-item'>
            <h3>Total Sales</h3>
            <p>₹{report.TotalSales:N2}</p>
        </div>
        <div class='summary-item'>
            <h3>Total CGST</h3>
            <p>₹{report.TotalCGST:N2}</p>
        </div>
        <div class='summary-item'>
            <h3>Total SGST</h3>
            <p>₹{report.TotalSGST:N2}</p>
        </div>
        <div class='summary-item'>
            <h3>Total IGST</h3>
            <p>₹{report.TotalIGST:N2}</p>
        </div>
        <div class='summary-item'>
            <h3>Total Tax</h3>
            <p>₹{report.TotalTax:N2}</p>
        </div>
        <div class='summary-item'>
            <h3>B2B Sales</h3>
            <p>₹{report.B2BSales:N2}</p>
        </div>
        <div class='summary-item'>
            <h3>B2C Sales</h3>
            <p>₹{report.B2CSales:N2}</p>
        </div>
    </div>
    
    <h2>Invoice Details</h2>
    <table>
        <thead>
            <tr>
                <th>Invoice #</th>
                <th>Date</th>
                <th>Customer</th>
                <th>GST No.</th>
                <th>HSN</th>
                <th>Taxable Value</th>
                <th>CGST</th>
                <th>SGST</th>
                <th>IGST</th>
                <th>Total</th>
            </tr>
        </thead>
        <tbody>
            {invoiceRows}
        </tbody>
    </table>
    
    <div class='footer'>
        <p>This is a computer-generated report for GST filing purposes.</p>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Generate HTML for a customer report
        /// </summary>
        private string GenerateCustomerReportHtml(CustomerReport report)
        {
            // Generate customer rows
            string customerRows = "";
            foreach (var customer in report.CustomerDetails)
            {
                customerRows += $@"
                <tr>
                    <td>{customer.CustomerID}</td>
                    <td>{customer.CustomerName}</td>
                    <td>{customer.PhoneNumber}</td>
                    <td>₹{customer.TotalPurchases:N2}</td>
                    <td>{customer.OrderCount}</td>
                    <td>{customer.LastPurchaseDate?.ToString("dd-MMM-yyyy") ?? "N/A"}</td>
                    <td>{customer.RepairJobCount}</td>
                    <td>₹{customer.PendingAmount:N2}</td>
                    <td>{customer.LoyaltyPoints}</td>
                </tr>";
            }

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Customer Report</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; }}
        h1, h2 {{ text-align: center; }}
        .summary {{ display: flex; justify-content: space-around; margin: 20px 0; }}
        .summary-item {{ text-align: center; padding: 10px; border: 1px solid #ddd; width: 180px; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
        table th, table td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        table th {{ background-color: #f2f2f2; }}
        .footer {{ margin-top: 30px; text-align: center; font-size: 12px; }}
        .period {{ text-align: center; font-weight: bold; margin-bottom: 20px; }}
    </style>
</head>
<body>
    <h1>Customer Report</h1>
    <p style='text-align: center;'>Generated on {DateTime.Now:dd-MMM-yyyy HH:mm}</p>
    
    <div class='period'>
        Period: {report.FromDate:dd-MMM-yyyy} to {report.ToDate:dd-MMM-yyyy}
    </div>
    
    <div class='summary'>
        <div class='summary-item'>
            <h3>Total Customers</h3>
            <p>{report.TotalCustomers}</p>
        </div>
        <div class='summary-item'>
            <h3>Total Purchases</h3>
            <p>₹{report.TotalPurchases:N2}</p>
        </div>
        <div class='summary-item'>
            <h3>Avg. Purchase</h3>
            <p>₹{(report.TotalCustomers > 0 ? (report.TotalPurchases / report.TotalCustomers) : 0):N2}</p>
        </div>
    </div>
    
    <h2>Customer Details</h2>
    <table>
        <thead>
            <tr>
                <th>ID</th>
                <th>Name</th>
                <th>Phone</th>
                <th>Total Purchases</th>
                <th>Orders</th>
                <th>Last Purchase</th>
                <th>Repairs</th>
                <th>Outstanding</th>
                <th>Loyalty Points</th>
            </tr>
        </thead>
        <tbody>
            {customerRows}
        </tbody>
    </table>
    
    <div class='footer'>
        <p>This is a computer-generated report.</p>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Generate and print a HUID/Hallmark certificate
        /// </summary>
        public async Task<string> GenerateHUIDCertificateAsync(string huid, bool isPrintPreview = false)
        {
            try
            {
                // Find product by HUID
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .FirstOrDefaultAsync(p => p.HUID == huid);

                if (product == null)
                {
                    await _logService.LogErrorAsync($"Product not found with HUID: {huid}");
                    return null;
                }

                // Get business info for certificate
                var businessInfo = await _context.BusinessInfo.FirstOrDefaultAsync();

                // Create HTML content for certificate
                string certificateHtml = GenerateHUIDCertificateHtml(product, businessInfo);
                
                // Path to save the certificate file
                string fileName = $"HUID_Certificate_{huid}.html";
                string filePath = Path.Combine(_templatePath, fileName);
                
                // Save HTML to file
                await File.WriteAllTextAsync(filePath, certificateHtml);
                
                if (!isPrintPreview)
                {
                    // Trigger printing (implementation would depend on your printer setup)
                    await _logService.LogInformationAsync($"HUID Certificate printed for HUID: {huid}");
                }

                await _logService.LogInformationAsync($"HUID Certificate generated for HUID: {huid}, File: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error generating HUID certificate: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Generate HTML for a HUID/Hallmark certificate
        /// </summary>
        private string GenerateHUIDCertificateHtml(Product product, BusinessInfo businessInfo)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>HUID Certificate - {product.HUID}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; }}
        .certificate {{ border: 5px double #9c7c38; padding: 20px; max-width: 800px; margin: 0 auto; }}
        .header {{ text-align: center; margin-bottom: 20px; }}
        .title {{ font-size: 24px; color: #9c7c38; margin-bottom: 10px; text-transform: uppercase; }}
        .logo {{ margin-bottom: 10px; }}
        .content {{ margin-bottom: 30px; }}
        .content table {{ width: 100%; border-collapse: collapse; }}
        .content table td {{ padding: 8px; }}
        .content table td:first-child {{ font-weight: bold; width: 40%; }}
        .footer {{ text-align: center; margin-top: 30px; }}
        .signature {{ display: flex; justify-content: space-between; margin-top: 50px; }}
        .signature-box {{ text-align: center; width: 45%; }}
        .signature-line {{ border-top: 1px solid #000; padding-top: 5px; }}
        .watermark {{ position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%) rotate(-45deg); font-size: 100px; opacity: 0.1; }}
    </style>
</head>
<body>
    <div class='certificate'>
        <div class='watermark'>HALLMARKED</div>
        
        <div class='header'>
            <div class='logo'>
                <!-- Insert your logo here -->
                <img src='{businessInfo?.LogoUrl ?? ""}' alt='Logo' style='max-width: 150px;'>
            </div>
            <div class='title'>Hallmark Unique ID Certificate</div>
            <p>{businessInfo?.BusinessName ?? "Jewelry Shop"}</p>
            <p>BIS Registration No: {businessInfo?.BISRegistrationNumber ?? "XXXXXXXXXX"}</p>
        </div>
        
        <div class='content'>
            <p>This is to certify that the jewelry item described below has been hallmarked in accordance with Bureau of Indian Standards (BIS) regulations:</p>
            
            <table>
                <tr>
                    <td>HUID Number:</td>
                    <td><strong>{product.HUID}</strong></td>
                </tr>
                <tr>
                    <td>Product Description:</td>
                    <td>{product.ProductName}</td>
                </tr>
                <tr>
                    <td>Category:</td>
                    <td>{product.Category?.CategoryName ?? "Jewelry"}</td>
                </tr>
                <tr>
                    <td>Metal Type:</td>
                    <td>{product.MetalType}</td>
                </tr>
                <tr>
                    <td>Purity:</td>
                    <td>{product.Purity}</td>
                </tr>
                <tr>
                    <td>Gross Weight:</td>
                    <td>{product.GrossWeight:N3}g</td>
                </tr>
                <tr>
                    <td>Net Weight:</td>
                    <td>{product.NetWeight:N3}g</td>
                </tr>
                <tr>
                    <td>Stone Details:</td>
                    <td>{product.StoneDetails ?? "No stones"}</td>
                </tr>
                <tr>
                    <td>Manufacturer:</td>
                    <td>{product.Supplier?.SupplierName ?? "In-house"}</td>
                </tr>
                <tr>
                    <td>Date of Hallmarking:</td>
                    <td>{DateTime.Now:dd-MMM-yyyy}</td>
                </tr>
            </table>
        </div>
        
        <div class='signature'>
            <div class='signature-box'>
                <div class='signature-line'>Authorized Signature</div>
                <p>{businessInfo?.OwnerName ?? "Jeweler"}</p>
            </div>
            <div class='signature-box'>
                <div class='signature-line'>BIS Hallmarking Center</div>
                <p>Assaying Officer</p>
            </div>
        </div>
        
        <div class='footer'>
            <p>This certificate verifies the authenticity of the hallmarked jewelry item described above.</p>
            <p>For verification, please contact {businessInfo?.PhoneNumber ?? ""} or visit {businessInfo?.Website ?? ""}</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}