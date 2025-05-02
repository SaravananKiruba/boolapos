using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Tests
{
    [TestClass]
    public class JewelryShopPOSTestSuite
    {
        [TestMethod]
        public void RunAllWorkflowTests()
        {
            Console.WriteLine("============================================");
            Console.WriteLine("JEWELRY SHOP POS SYSTEM - AUTOMATED TESTING");
            Console.WriteLine("============================================");
            
            // Phase 1 – Core Operations
            Console.WriteLine("\n📌 PHASE 1 – CORE OPERATIONS");
            
            Console.WriteLine("\n🔁 Workflow 1: Add New Customer → Make a Sale");
            RunWorkflow1Tests();
            
            Console.WriteLine("\n🔁 Workflow 2: Stock Entry → Inventory Ledger Update");
            RunWorkflow2Tests();

            // Phase 2 – Repairs, Purchase, Financials
            Console.WriteLine("\n📌 PHASE 2 – REPAIRS, PURCHASE, FINANCIALS");
            
            Console.WriteLine("\n🔁 Workflow 3: Create Repair Job → Track Status → Complete");
            RunWorkflow3Tests();
            
            Console.WriteLine("\n🔁 Workflow 4: Vendor Purchase → Inventory Update");
            RunWorkflow4Tests();
            
            Console.WriteLine("\n🔁 Workflow 5: Day Start → Transactions → Day Close");
            RunWorkflow5Tests();

            // Phase 3 – Reports, Dashboard, Communication
            Console.WriteLine("\n📌 PHASE 3 – REPORTS, DASHBOARD, COMMUNICATION");
            
            Console.WriteLine("\n🔁 Workflow 6: View Dashboard → Download Reports");
            RunWorkflow6Tests();
            
            Console.WriteLine("\n🔁 Workflow 7: Print Custom Invoice");
            RunWorkflow7Tests();
            
            Console.WriteLine("\n============================================");
            Console.WriteLine("ALL AUTOMATED TESTS COMPLETED SUCCESSFULLY");
            Console.WriteLine("============================================");
        }

        private void RunWorkflow1Tests()
        {
            var testClass = new Workflow1_CustomerSaleTests();
            try
            {
                testClass.TestInitialize();
                
                Console.WriteLine("✓ Testing: Add Customer");
                testClass.AddCustomer_ShouldCreateNewCustomer().Wait();
                
                Console.WriteLine("✓ Testing: Add Product to Inventory");
                testClass.AddProductToInventory_ShouldCreateNewProduct().Wait();
                
                Console.WriteLine("✓ Testing: Create Order with Customer and Product");
                testClass.CreateOrder_WithSelectedCustomerAndProduct_ShouldCreateOrder().Wait();
                
                Console.WriteLine("✅ All Workflow 1 tests passed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                throw;
            }
            finally
            {
                testClass.TestCleanup();
            }
        }

        private void RunWorkflow2Tests()
        {
            var testClass = new Workflow2_InventoryLedgerTests();
            try
            {
                testClass.TestInitialize();
                
                Console.WriteLine("✓ Testing: Add Gold Item to Inventory");
                testClass.AddGoldItemToInventory_ShouldBeVisibleInStockList().Wait();
                
                Console.WriteLine("✓ Testing: Perform Sale - Reduce Inventory");
                testClass.PerformSale_ShouldReduceInventoryInRealTime().Wait();
                
                Console.WriteLine("✓ Testing: Exchange Product - Update Ledger");
                testClass.ExchangeProduct_ShouldUpdateLedgerCorrectly().Wait();
                
                Console.WriteLine("✅ All Workflow 2 tests passed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                throw;
            }
            finally
            {
                testClass.TestCleanup();
            }
        }

        private void RunWorkflow3Tests()
        {
            var testClass = new Workflow3_RepairJobTests();
            try
            {
                testClass.TestInitialize();
                
                Console.WriteLine("✓ Testing: Create Repair Job");
                testClass.CreateRepairJob_ShouldRegisterNewJob().Wait();
                
                Console.WriteLine("✓ Testing: Update Repair Job Status");
                testClass.UpdateRepairJobStatus_ShouldChangeStatus().Wait();
                
                Console.WriteLine("✓ Testing: Send Repair Completion Notification");
                testClass.SendNotification_WhenRepairCompleted_ShouldRecordNotification().Wait();
                
                Console.WriteLine("✓ Testing: View Customer Repair History");
                testClass.ViewRepairJobHistory_ForCustomer_ShouldReturnAllJobs().Wait();
                
                Console.WriteLine("✅ All Workflow 3 tests passed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                throw;
            }
            finally
            {
                testClass.TestCleanup();
            }
        }

        private void RunWorkflow4Tests()
        {
            var testClass = new Workflow4_VendorPurchaseTests();
            try
            {
                testClass.TestInitialize();
                
                Console.WriteLine("✓ Testing: Add Vendor with GST");
                testClass.AddVendorWithGST_ShouldCreateSupplier().Wait();
                
                Console.WriteLine("✓ Testing: Create Purchase Entry");
                testClass.CreatePurchaseEntry_ShouldRecordPurchaseAndUpdateInventory().Wait();
                
                Console.WriteLine("✓ Testing: Check Vendor Payment History");
                testClass.CheckVendorPaymentHistory_ShouldReturnAllPayments().Wait();
                
                Console.WriteLine("✅ All Workflow 4 tests passed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                throw;
            }
            finally
            {
                testClass.TestCleanup();
            }
        }

        private void RunWorkflow5Tests()
        {
            var testClass = new Workflow5_DayStartCloseTests();
            try
            {
                testClass.TestInitialize();
                
                Console.WriteLine("✓ Testing: Open Daily Cash Register");
                testClass.OpenDailyCashRegister_ShouldRecordOpeningBalance().Wait();
                
                Console.WriteLine("✓ Testing: Record Multiple Transactions");
                testClass.RecordMultipleTransactions_ShouldUpdateFinancialRecords().Wait();
                
                Console.WriteLine("✓ Testing: Close Day Operation");
                testClass.CloseDayOperation_ShouldCalculateAndRecordClosingBalance().Wait();
                
                Console.WriteLine("✓ Testing: Print Cash Register Summary");
                testClass.PrintCashRegisterSummary_ShouldProvideAccurateReport().Wait();
                
                Console.WriteLine("✅ All Workflow 5 tests passed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                throw;
            }
            finally
            {
                testClass.TestCleanup();
            }
        }

        private void RunWorkflow6Tests()
        {
            var testClass = new Workflow6_DashboardReportsTests();
            try
            {
                testClass.TestInitialize();
                
                Console.WriteLine("✓ Testing: View Dashboard");
                testClass.ViewDashboard_ShouldShowCorrectTotals().Wait();
                
                Console.WriteLine("✓ Testing: Export Sales Report");
                testClass.ExportSalesReport_ShouldIncludeAllSales().Wait();
                
                Console.WriteLine("✓ Testing: Export Customer Purchase History");
                testClass.ExportCustomerPurchaseHistory_ShouldShowCustomerOrders().Wait();
                
                Console.WriteLine("✓ Testing: Export GST Summary");
                testClass.ExportGSTSummary_ShouldCalculateCorrectTaxes().Wait();
                
                Console.WriteLine("✅ All Workflow 6 tests passed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                throw;
            }
            finally
            {
                testClass.TestCleanup();
            }
        }

        private void RunWorkflow7Tests()
        {
            var testClass = new Workflow7_CustomInvoiceTests();
            try
            {
                testClass.TestInitialize();
                
                Console.WriteLine("✓ Testing: Generate Invoice");
                testClass.GenerateInvoice_ShouldContainCorrectOrderDetails().Wait();
                
                Console.WriteLine("✓ Testing: Verify Invoice GST Details");
                testClass.VerifyInvoiceDetails_ShouldIncludeGSTDetails().Wait();
                
                Console.WriteLine("✓ Testing: Print Invoice with Formatting");
                testClass.PrintInvoice_ShouldFormatCurrencyAndWeightCorrectly().Wait();
                
                Console.WriteLine("✅ All Workflow 7 tests passed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                throw;
            }
            finally
            {
                testClass.TestCleanup();
            }
        }
    }
}