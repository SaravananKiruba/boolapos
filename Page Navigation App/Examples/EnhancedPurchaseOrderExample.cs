using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using Page_Navigation_App.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Page_Navigation_App.Examples
{
    /// <summary>
    /// Example usage of the Enhanced Purchase Order Workflow
    /// This demonstrates the complete flow from Purchase Order to Customer Order
    /// </summary>
    public class EnhancedPurchaseOrderExample
    {
        private readonly AppDbContext _context;
        private readonly PurchaseOrderService _purchaseOrderService;
        private readonly StockService _stockService;
        private readonly OrderService _orderService;
        private readonly LogService _logService;

        public EnhancedPurchaseOrderExample(
            AppDbContext context,
            PurchaseOrderService purchaseOrderService,
            StockService stockService,
            OrderService orderService,
            LogService logService)
        {
            _context = context;
            _purchaseOrderService = purchaseOrderService;
            _stockService = stockService;
            _orderService = orderService;
            _logService = logService;
        }

        /// <summary>
        /// Complete workflow example: Purchase Order → Stock → Customer Order
        /// </summary>
        public async Task<bool> DemonstrateCompleteWorkflow()
        {
            try
            {
                Console.WriteLine("=== Enhanced Purchase Order Workflow Example ===\n");

                // Step 1: Create Purchase Order
                Console.WriteLine("Step 1: Creating Purchase Order...");
                var purchaseOrder = await CreateSamplePurchaseOrder();
                Console.WriteLine($"✓ Purchase Order created: {purchaseOrder.PurchaseOrderNumber}");
                Console.WriteLine($"  Total Amount: ₹{purchaseOrder.GrandTotal:N2} (No GST)");

                // Step 2: Mark Items as Received (Add to Stock)
                Console.WriteLine("\nStep 2: Marking items as received and adding to stock...");
                var receiveSuccess = await _purchaseOrderService.MarkItemsReceived(
                    purchaseOrder.PurchaseOrderID, 
                    null, 
                    "Store Manager");
                
                if (receiveSuccess)
                {
                    Console.WriteLine("✓ Items received and added to stock");
                    
                    // Show stock items created
                    var stockItems = await _stockService.GetStockItemsByProduct(1, "Available");
                    Console.WriteLine($"  Created {stockItems.Count} individual stock items with unique IDs:");
                    foreach (var item in stockItems.Take(3)) // Show first 3
                    {
                        Console.WriteLine($"    - Tag ID: {item.UniqueTagID}, Barcode: {item.Barcode}");
                    }
                    if (stockItems.Count > 3)
                    {
                        Console.WriteLine($"    ... and {stockItems.Count - 3} more items");
                    }
                }

                // Step 3: Record Payment (Create Finance Record)
                Console.WriteLine("\nStep 3: Recording payment...");
                var paymentSuccess = await _purchaseOrderService.RecordPurchaseOrderPayment(
                    purchaseOrder.PurchaseOrderID,
                    purchaseOrder.GrandTotal,
                    "Bank Transfer",
                    "Full payment for purchase order");

                if (paymentSuccess)
                {
                    Console.WriteLine("✓ Payment recorded and expense created in Finance");
                    
                    // Show updated purchase order status
                    var updatedPO = await _purchaseOrderService.GetPurchaseOrderById(purchaseOrder.PurchaseOrderID);
                    Console.WriteLine($"  Payment Status: {updatedPO.PaymentStatus}");
                    Console.WriteLine($"  Item Receipt Status: {updatedPO.ItemReceiptStatus}");
                }

                // Step 4: Create Customer Order (Stock Deduction)
                Console.WriteLine("\nStep 4: Creating customer order (stock deduction)...");
                var customerOrder = await CreateSampleCustomerOrder();
                
                if (customerOrder.Success)
                {
                    Console.WriteLine($"✓ Customer order created: {customerOrder.Order.InvoiceNumber}");
                    Console.WriteLine($"  Total Amount: ₹{customerOrder.Order.GrandTotal:N2}");
                    
                    // Show stock deduction
                    var remainingStock = await _stockService.GetStockItemsByProduct(1, "Available");
                    Console.WriteLine($"  Remaining stock items: {remainingStock.Count}");
                }

                // Step 5: Show Stock Summary
                Console.WriteLine("\nStep 5: Stock Summary...");
                var stockSummary = await _stockService.GetStockSummaryByProduct();
                Console.WriteLine("✓ Stock Summary:");
                foreach (dynamic item in stockSummary.Take(5)) // Show first 5 products
                {
                    Console.WriteLine($"  Product: {item.ProductName}");
                    Console.WriteLine($"    Available: {item.Available}, Sold: {item.Sold}, Total Value: ₹{item.TotalValue:N2}");
                }

                Console.WriteLine("\n=== Workflow completed successfully! ===");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in workflow: {ex.Message}");
                await _logService.LogErrorAsync($"Workflow example failed: {ex.Message}", exception: ex);
                return false;
            }
        }

        private async Task<PurchaseOrder> CreateSamplePurchaseOrder()
        {
            var purchaseOrder = new PurchaseOrder
            {
                SupplierID = 1, // Assuming supplier with ID 1 exists
                PurchaseOrderNumber = $"PO{DateTime.Now:yyyyMMdd}{Random.Shared.Next(100, 999)}",
                OrderDate = DateOnly.FromDateTime(DateTime.Now),
                ExpectedDeliveryDate = DateOnly.FromDateTime(DateTime.Now.AddDays(7)),
                Status = "Pending",
                PaymentMethod = "Bank Transfer",
                CreatedBy = "System Demo"
            };

            var items = new List<PurchaseOrderItem>
            {
                new PurchaseOrderItem
                {
                    ProductID = 1, // Assuming product with ID 1 exists
                    Quantity = 10,
                    UnitCost = 500.00m,
                    Notes = "Sample purchase for demo"
                },
                new PurchaseOrderItem
                {
                    ProductID = 2, // Assuming product with ID 2 exists
                    Quantity = 5,
                    UnitCost = 1000.00m,
                    Notes = "Premium items for demo"
                }
            };

            return await _purchaseOrderService.CreatePurchaseOrder(purchaseOrder, items);
        }

        private async Task<(Order Order, bool Success, string ErrorMessage)> CreateSampleCustomerOrder()
        {
            var order = new Order
            {
                CustomerID = 1, // Assuming customer with ID 1 exists
                OrderDate = DateOnly.FromDateTime(DateTime.Now),
                Status = "Completed",
                PaymentMethod = "Cash",
                DiscountAmount = 0
            };

            var orderDetails = new List<OrderDetail>
            {
                new OrderDetail
                {
                    ProductID = 1,
                    Quantity = 2,
                    // UnitPrice will be set from product price
                }
            };

            return await _orderService.CreateOrderWithStockValidation(order, orderDetails);
        }

        /// <summary>
        /// Simple demo of Purchase Order operations
        /// </summary>
        public async Task<bool> DemoBasicOperations()
        {
            try
            {
                Console.WriteLine("=== Basic Purchase Order Operations Demo ===\n");

                // Create a simple purchase order
                var po = new PurchaseOrder
                {
                    SupplierID = 1,
                    PurchaseOrderNumber = "DEMO-PO-001",
                    CreatedBy = "Demo User"
                };

                var items = new List<PurchaseOrderItem>
                {
                    new PurchaseOrderItem
                    {
                        ProductID = 1,
                        Quantity = 5,
                        UnitCost = 200.00m
                    }
                };

                // Simplified calculation: 5 × 200 = 1000 (No GST)
                var createdPO = await _purchaseOrderService.CreatePurchaseOrder(po, items);
                Console.WriteLine($"Created PO: {createdPO.PurchaseOrderNumber}");
                Console.WriteLine($"Total: ₹{createdPO.GrandTotal:N2} (Simplified: Qty × Price, No GST)");

                // Option 1: Mark items received
                await _purchaseOrderService.MarkItemsReceived(createdPO.PurchaseOrderID);
                Console.WriteLine("✓ Items marked as received and added to stock");

                // Option 2: Record payment
                await _purchaseOrderService.RecordPurchaseOrderPayment(
                    createdPO.PurchaseOrderID, 
                    createdPO.GrandTotal, 
                    "Cash");
                Console.WriteLine("✓ Payment recorded as expense in Finance");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Demo failed: {ex.Message}");
                return false;
            }
        }
    }
}
