using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Page_Navigation_App.Services
{
    public class SupplierService
    {
        private readonly AppDbContext _context;
        private readonly StockService _stockService;
        private readonly StockLedgerService _ledgerService;

        public SupplierService(
            AppDbContext context, 
            StockService stockService,
            StockLedgerService ledgerService)
        {
            _context = context;
            _stockService = stockService;
            _ledgerService = ledgerService;
        }

        // Create supplier
        public async Task<Supplier> AddSupplier(Supplier supplier)
        {
            try
            {
                supplier.IsActive = true;
                supplier.RegistrationDate = DateTime.Now;
                
                await _context.Suppliers.AddAsync(supplier);
                await _context.SaveChangesAsync();
                return supplier;
            }
            catch
            {
                return null;
            }
        }

        // Read suppliers
        public async Task<Supplier> GetSupplierById(int supplierId)
        {
            return await _context.Suppliers.FindAsync(supplierId);
        }

        public async Task<IEnumerable<Supplier>> GetAllSuppliers(bool includeInactive = false)
        {
            var query = _context.Suppliers.AsQueryable();
            
            if (!includeInactive)
                query = query.Where(s => s.IsActive);
                
            return await query.OrderBy(s => s.Name).ToListAsync();
        }

        public async Task<IEnumerable<Supplier>> SearchSuppliers(string searchTerm)
        {
            return await _context.Suppliers
                .Where(s => s.IsActive && 
                          (s.Name.Contains(searchTerm) || 
                           s.ContactPerson.Contains(searchTerm) || 
                           s.Mobile.Contains(searchTerm) || 
                           s.Email.Contains(searchTerm) || 
                           s.GSTNumber.Contains(searchTerm)))
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        // Update supplier
        public async Task<bool> UpdateSupplier(Supplier supplier)
        {
            try
            {
                var existingSupplier = await _context.Suppliers.FindAsync(supplier.SupplierID);
                if (existingSupplier == null) return false;

                _context.Entry(existingSupplier).CurrentValues.SetValues(supplier);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Delete supplier (soft delete)
        public async Task<bool> DeactivateSupplier(int supplierId)
        {
            try
            {
                var supplier = await _context.Suppliers.FindAsync(supplierId);
                if (supplier == null) return false;

                supplier.IsActive = false;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Purchase operations
        public async Task<Stock> CreatePurchase(Stock purchase)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate supplier and product
                var supplier = await _context.Suppliers.FindAsync(purchase.SupplierID);
                var product = await _context.Products.FindAsync(purchase.ProductID);
                
                if (supplier == null || product == null) return null;
                
                purchase.PurchaseDate = DateTime.Now;
                await _context.Stocks.AddAsync(purchase);
                await _context.SaveChangesAsync();
                
                // Update product stock - Fix decimal to string conversion error
                await _stockService.IncreaseStock(
                    purchase.ProductID, 
                    purchase.QuantityPurchased, 
                    purchase.PurchaseRate, // Changed from UnitPrice to PurchaseRate
                    "Purchase");
                
                await transaction.CommitAsync();
                return purchase;
            }
            catch
            {
                await transaction.RollbackAsync();
                return null;
            }
        }

        public async Task<IEnumerable<Stock>> GetSupplierPurchaseHistory(int supplierId)
        {
            return await _context.Stocks
                .Include(s => s.Product)
                .Include(s => s.Supplier)
                .Where(s => s.SupplierID == supplierId)
                .OrderByDescending(s => s.PurchaseDate)
                .ToListAsync();
        }

        public async Task<bool> UpdatePurchasePaymentStatus(int purchaseId, string paymentStatus)
        {
            try
            {
                var purchase = await _context.Stocks.FindAsync(purchaseId);
                if (purchase == null) return false;
                
                purchase.PaymentStatus = paymentStatus;
                purchase.LastUpdated = DateTime.Now;
                
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Dictionary<string, decimal>> GetSupplierPaymentSummary(int supplierId)
        {
            var purchases = await _context.Stocks
                .Where(s => s.SupplierID == supplierId)
                .ToListAsync();
                
            return new Dictionary<string, decimal>
            {
                ["TotalPurchases"] = purchases.Sum(p => p.TotalAmount),
                ["PaidAmount"] = purchases.Where(p => p.PaymentStatus == "Paid").Sum(p => p.TotalAmount),
                ["PendingAmount"] = purchases.Where(p => p.PaymentStatus == "Pending").Sum(p => p.TotalAmount)
            };
        }

        // Record purchase transaction for a supplier
        public async Task<Finance> RecordPurchase(
            int supplierId, 
            decimal amount, 
            string category = "Purchase", 
            string paymentMode = "Direct",
            string referenceNumber = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var supplier = await _context.Suppliers.FindAsync(supplierId);
                if (supplier == null) return null;

                var financeEntry = new Finance
                {
                    TransactionDate = DateTime.Now,
                    TransactionType = "Expense",
                    Amount = amount,
                    PaymentMode = paymentMode,
                    Category = category,
                    Description = $"Purchase from {supplier.Name}",
                    ReferenceNumber = referenceNumber,
                    SupplierName = supplier.Name,
                    Status = "Completed",
                    CreatedBy = "System"
                };

                await _context.Finances.AddAsync(financeEntry);
                await _context.SaveChangesAsync();
                
                await transaction.CommitAsync();
                return financeEntry;
            }
            catch
            {
                await transaction.RollbackAsync();
                return null;
            }
        }

        // Record payment to a supplier
        public async Task<Finance> RecordPayment(
            int supplierId,
            decimal amount,
            string paymentMode = "Cash",
            string referenceNumber = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var supplier = await _context.Suppliers.FindAsync(supplierId);
                if (supplier == null) return null;

                var financeEntry = new Finance
                {
                    TransactionDate = DateTime.Now,
                    TransactionType = "Expense",
                    Amount = amount,
                    PaymentMode = paymentMode,
                    Category = "Supplier Payment",
                    Description = $"Payment to {supplier.Name}",
                    ReferenceNumber = referenceNumber,
                    SupplierName = supplier.Name,
                    Status = "Completed",
                    CreatedBy = "System"
                };

                await _context.Finances.AddAsync(financeEntry);
                await _context.SaveChangesAsync();
                
                await transaction.CommitAsync();
                return financeEntry;
            }
            catch
            {
                await transaction.RollbackAsync();
                return null;
            }
        }
    }
}