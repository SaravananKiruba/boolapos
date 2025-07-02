# Enhanced Purchase Order Workflow Documentation

## Overview
The Purchase Order system has been enhanced to provide a simpler, more integrated workflow similar to the Order system. The key improvements focus on:

1. **Simplified Calculations** - No GST applicable for Purchase Orders
2. **Two-Step Process** - Item Received → Amount Paid
3. **Individual Item Tracking** - Each product count as separate items with unique IDs
4. **Stock Integration** - Automatic stock management
5. **Finance Integration** - Expense recording

## Key Features

### 1. Simplified Purchase Order Calculation
- **Formula**: `Count × Product Price = Total Amount`
- **No GST**: Unlike sales orders, purchase orders don't include GST
- **Final Total**: `Total Amount - Discount = Grand Total`

### 2. Two-Step Workflow Options

#### Option 1: Item Received
- Mark items as received
- Automatically add to stock
- Create individual stock items with unique Tag IDs and Barcodes
- Update stock availability counts

#### Option 2: Amount Paid
- Record partial or full payments
- Create expense records in Finance
- Track payment status (Pending, Partial, Paid)

### 3. Stock Management Enhancement

#### Stock Addition (from Purchase Orders)
- Each product quantity is treated as individual items
- Each item gets a unique Tag ID: `{ProductCode}{YYYYMMDD}{RandomNumber}`
- Each item gets a unique Barcode: `BC{ProductID}{YYYYMMDD}{RandomNumber}`
- Items are linked to specific Purchase Order Items
- Stock counts are updated automatically

#### Stock Deduction (from Orders)
- Items are deducted using FIFO (First In, First Out) method
- Individual stock items are marked as "Sold"
- Stock availability is updated in real-time
- Items are linked to specific customer orders

## Database Schema Changes

### PurchaseOrder Table Enhancements
```sql
-- New fields added
ItemReceiptStatus VARCHAR(50) -- Pending, Partial, Completed
IsItemsReceived BIT -- Boolean flag
ItemsReceivedDate DATETIME -- When items were received
HasFinanceRecord BIT -- Whether payment recorded
FinanceRecordID VARCHAR(MAX) -- Link to Finance record

-- Removed field
TaxAmount DECIMAL(18,2) -- No GST for Purchase Orders
```

### PurchaseOrderItem Table Enhancements
```sql
-- New fields added
ReceivedBy VARCHAR(100) -- Who received the items
IsAddedToStock BIT -- Whether items added to stock
StockAddedDate DATETIME -- When items were added to stock
```

### Stock Table Enhancements
```sql
-- New fields added
AvailableCount INT -- Count of available items
ReservedCount INT -- Count of reserved items
SoldCount INT -- Count of sold items
PurchaseOrderReceivedDate DATETIME -- When PO items were received
```

### StockItem Table Enhancements
```sql
-- New field added
PurchaseOrderItemID INT -- Link to specific PO item
```

## Service Methods

### PurchaseOrderService New Methods

#### 1. MarkItemsReceived
```csharp
public async Task<bool> MarkItemsReceived(
    int purchaseOrderId, 
    List<int> receivedItemIds = null, 
    string receivedBy = "System")
```
- Marks items as received
- Creates individual stock items with unique IDs
- Updates stock availability counts
- Links items to Purchase Order

#### 2. RecordPurchaseOrderPayment
```csharp
public async Task<bool> RecordPurchaseOrderPayment(
    int purchaseOrderId, 
    decimal paidAmount, 
    string paymentMethod = "Cash", 
    string notes = "")
```
- Records partial or full payments
- Creates Finance expense records
- Updates payment status
- Links to Finance system

### StockService New Methods

#### 1. DeductStockForOrder
```csharp
public async Task<bool> DeductStockForOrder(
    int orderId, 
    List<OrderDetail> orderDetails)
```
- Deducts stock using FIFO method
- Marks individual items as sold
- Updates stock availability
- Links items to customer orders

#### 2. CheckStockAvailability
```csharp
public async Task<Dictionary<int, int>> CheckStockAvailability(
    List<OrderDetail> orderDetails)
```
- Checks stock availability before order processing
- Returns available count per product
- Prevents overselling

## Workflow Process

### Creating a Purchase Order
1. Create Purchase Order with items
2. Calculate totals (Quantity × Unit Price, no GST)
3. Save to database
4. Status: "Pending"

### Receiving Items (Option 1)
1. Call `MarkItemsReceived()`
2. Create individual StockItem records
3. Generate unique Tag IDs and Barcodes
4. Update Stock availability counts
5. Mark items as received
6. Status: "Delivered" or "Partially Delivered"

### Recording Payment (Option 2)
1. Call `RecordPurchaseOrderPayment()`
2. Create Finance expense record
3. Update payment amounts
4. Payment Status: "Partial" or "Paid"

### Processing Customer Orders
1. Check stock availability
2. Reserve required items (FIFO)
3. Create order
4. Call `DeductStockForOrder()`
5. Mark items as sold
6. Update stock counts

## Benefits

### 1. Simplified Operations
- No complex GST calculations for purchases
- Clear two-step process
- Easy to understand workflow

### 2. Better Inventory Control
- Individual item tracking
- Unique identification for each item
- Real-time stock updates
- FIFO inventory management

### 3. Financial Integration
- Automatic expense recording
- Payment tracking
- Complete audit trail

### 4. Scalability
- Handle large inventories
- Support for partial deliveries
- Support for partial payments

## Usage Examples

### Example 1: Complete Purchase Order Flow
```csharp
// 1. Create Purchase Order
var purchaseOrder = new PurchaseOrder
{
    SupplierID = 1,
    PurchaseOrderNumber = "PO001",
    // ... other properties
};

var items = new List<PurchaseOrderItem>
{
    new PurchaseOrderItem
    {
        ProductID = 1,
        Quantity = 10,
        UnitCost = 100.00m
    }
};

var createdPO = await purchaseOrderService.CreatePurchaseOrder(purchaseOrder, items);

// 2. Mark Items Received
await purchaseOrderService.MarkItemsReceived(createdPO.PurchaseOrderID);

// 3. Record Payment
await purchaseOrderService.RecordPurchaseOrderPayment(
    createdPO.PurchaseOrderID, 
    1000.00m, 
    "Bank Transfer");
```

### Example 2: Process Customer Order
```csharp
// 1. Check availability
var availability = await stockService.CheckStockAvailability(orderDetails);

// 2. Create order if stock available
if (availability.All(a => a.Value >= requiredQuantity))
{
    var order = await orderService.CreateOrder(orderData);
    
    // 3. Deduct stock
    await stockService.DeductStockForOrder(order.OrderID, orderDetails);
}
```

## Migration
To apply the database changes:
```bash
dotnet ef migrations add EnhancePurchaseOrderWorkflow
dotnet ef database update
```

This enhanced system provides a comprehensive, integrated approach to purchase order management with individual item tracking and seamless stock management.
