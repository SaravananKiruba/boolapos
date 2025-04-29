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
        private readonly FinanceService _financeService;

        public SupplierService(AppDbContext context, FinanceService financeService)
        {
            _context = context;
            _financeService = financeService;
        }

        public async Task<Supplier> AddSupplier(Supplier supplier)
        {
            supplier.IsActive = true;
            await _context.Suppliers.AddAsync(supplier);
            await _context.SaveChangesAsync();
            return supplier;
        }

        public async Task<bool> UpdateSupplier(Supplier supplier)
        {
            var existingSupplier = await _context.Suppliers.FindAsync(supplier.SupplierID);
            if (existingSupplier == null) return false;

            _context.Entry(existingSupplier).CurrentValues.SetValues(supplier);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Supplier> GetSupplierWithHistory(int supplierId)
        {
            return await _context.Suppliers
                .Include(s => s.Products)
                .Include(s => s.Payments)
                .FirstOrDefaultAsync(s => s.SupplierID == supplierId);
        }

        public async Task<IEnumerable<Supplier>> SearchSuppliers(
            string searchTerm = null,
            bool? isActive = null)
        {
            var query = _context.Suppliers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(s =>
                    s.SupplierName.Contains(searchTerm) ||
                    s.ContactNumber.Contains(searchTerm) ||
                    s.GSTNumber.Contains(searchTerm));
            }

            if (isActive.HasValue)
            {
                query = query.Where(s => s.IsActive == isActive.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<bool> RecordPurchase(
            int supplierId,
            decimal amount,
            string description,
            string paymentMode,
            string referenceNumber = null)
        {
            var supplier = await _context.Suppliers.FindAsync(supplierId);
            if (supplier == null) return false;

            // Update supplier balance
            supplier.CurrentBalance += amount;

            // Record transaction
            var transaction = new Finance
            {
                TransactionDate = DateTime.Now,
                Amount = amount,
                TransactionType = "Expense",
                PaymentMode = paymentMode,
                Description = description,
                Category = "Purchase",
                ReferenceNumber = referenceNumber,
                // Link to supplier instead of customer
                CustomerID = null
            };

            await _financeService.AddTransaction(transaction);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RecordPayment(
            int supplierId,
            decimal amount,
            string paymentMode,
            string referenceNumber = null)
        {
            var supplier = await _context.Suppliers.FindAsync(supplierId);
            if (supplier == null) return false;

            // Update supplier balance
            supplier.CurrentBalance -= amount;

            // Record transaction
            var transaction = new Finance
            {
                TransactionDate = DateTime.Now,
                Amount = amount,
                TransactionType = "Expense",
                PaymentMode = paymentMode,
                Description = $"Payment to supplier: {supplier.SupplierName}",
                Category = "Supplier Payment",
                ReferenceNumber = referenceNumber,
                // Link to supplier instead of customer
                CustomerID = null
            };

            await _financeService.AddTransaction(transaction);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Supplier>> GetSuppliersByDueAmount(decimal minAmount)
        {
            return await _context.Suppliers
                .Where(s => s.CurrentBalance >= minAmount)
                .OrderByDescending(s => s.CurrentBalance)
                .ToListAsync();
        }

        public async Task<Dictionary<string, decimal>> GetSupplierPaymentSummary(
            DateTime startDate,
            DateTime endDate)
        {
            var transactions = await _context.Finances
                .Where(f => f.TransactionDate >= startDate &&
                           f.TransactionDate <= endDate &&
                           (f.Category == "Purchase" || f.Category == "Supplier Payment"))
                .ToListAsync();

            return new Dictionary<string, decimal>
            {
                { "TotalPurchases", transactions.Where(t => t.Category == "Purchase").Sum(t => t.Amount) },
                { "TotalPayments", transactions.Where(t => t.Category == "Supplier Payment").Sum(t => t.Amount) },
                { "OutstandingBalance", await _context.Suppliers.SumAsync(s => s.CurrentBalance) }
            };
        }

        public async Task<bool> DeactivateSupplier(int supplierId)
        {
            var supplier = await _context.Suppliers.FindAsync(supplierId);
            if (supplier == null) return false;

            supplier.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}