# Jewelry Shop POS System Automated Tests

This directory contains automated tests for the Jewelry Shop POS System. These tests validate the core workflows and functionalities of the application.

## Test Structure

The tests are organized by workflow, corresponding to real business processes:

1. **Workflow1_CustomerSaleTests**: Tests for customer creation and sales processes
2. **Workflow2_InventoryLedgerTests**: Tests for inventory management and stock operations
3. **Workflow3_RepairJobTests**: Tests for jewelry repair operations
4. **Workflow4_VendorPurchaseTests**: Tests for vendor management and purchases
5. **Workflow5_DayStartCloseTests**: Tests for daily financial operations
6. **Workflow6_DashboardReportsTests**: Tests for reporting and dashboard features
7. **Workflow7_CustomInvoiceTests**: Tests for invoice generation and formatting

Additionally, the `JewelryShopPOSTestSuite` class provides a convenient way to run all tests in sequence.

## Running the Tests

### Using Visual Studio Test Explorer

1. Open the solution in Visual Studio
2. Open the Test Explorer window (Test > Test Explorer)
3. Click "Run All Tests" to execute all tests, or right-click on a specific test class to run only those tests

### Using Command Line

```
dotnet test Tests.csproj
```

To run a specific test class:

```
dotnet test Tests.csproj --filter "FullyQualifiedName~Workflow1_CustomerSaleTests"
```

To run the entire test suite through the runner class:

```
dotnet test Tests.csproj --filter "FullyQualifiedName~JewelryShopPOSTestSuite.RunAllWorkflowTests"
```

## Test Coverage

These tests cover the following key scenarios:

### Phase 1 – Core Operations
- Customer registration and management
- Product browsing and selection
- Completing sales transactions
- Inventory updates after sales
- Stock entry and inventory ledger management

### Phase 2 – Operations and Financial Management
- Repair job creation and status tracking
- Vendor/supplier management
- Purchase orders and inventory updates
- Day start/day close procedures
- Multi-mode payment handling

### Phase 3 – Reporting and Communication
- Dashboard metrics verification
- Report generation (sales, inventory, GST)
- Custom invoice generation with proper formatting
- Tax calculations and GST compliance

## Implementation Details

The tests use:
- Microsoft.EntityFrameworkCore.InMemory for database testing
- Moq for mocking dependencies
- MSTest for the testing framework

Each test class includes:
- `TestInitialize`: Sets up the test environment with required data
- Test methods: Individual test cases for specific functionalities
- `TestCleanup`: Cleans up resources after tests complete

## Adding New Tests

To add new tests:

1. Create a new test class following the naming pattern `Workflow{n}_{Description}Tests.cs`
2. Implement the required test methods
3. Add a corresponding runner method in `JewelryShopPOSTestSuite` if needed