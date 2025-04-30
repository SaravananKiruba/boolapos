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
            try
            {
                _context.Suppliers.Update(supplier);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Supplier> GetSupplierWithHistory(int supplierId)
        {
            return await _context.Suppliers
                .Include(s => s.Products)
                .Include(s => s.Payments)
                .FirstOrDefaultAsync(s => s.SupplierID == supplierId);
        }

        public async Task<IEnumerable<Supplier>> GetAllSuppliers()
        {
            return await _context.Suppliers
                .Include(s => s.Products)
                .Include(s => s.Payments)
                .ToListAsync();
        }

        public async Task<IEnumerable<Supplier>> SearchSuppliers(
            string searchTerm = null,
            bool? isActive = null)
        {
            var query = _context.Suppliers.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(s =>
                    s.SupplierName.Contains(searchTerm) ||
                    s.ContactNumber.Contains(searchTerm) ||
                    s.ContactPerson.Contains(searchTerm) ||
                    s.Email.Contains(searchTerm));
            }

            if (isActive.HasValue)
            {
                query = query.Where(s => s.IsActive == isActive.Value);
            }

            return await query
                .Include(s => s.Products)
                .Include(s => s.Payments)
                .ToListAsync();
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

            supplier.CurrentBalance += amount;
            
            var finance = new Finance
            {
                CustomerId = supplierId,
                TotalAmount = amount,
                RemainingAmount = amount,
                Status = "Purchase",
                OrderReference = 0, // Since this is a purchase, not an order
                StartDate = DateTime.Now
            };

            await _financeService.AddFinanceRecord(finance);
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

            supplier.CurrentBalance -= amount;

            var finance = new Finance
            {
                CustomerId = supplierId,
                TotalAmount = amount,
                RemainingAmount = 0,
                Status = "Payment",
                OrderReference = 0, // Since this is a payment, not an order
                StartDate = DateTime.Now,
                LastPaymentDate = DateTime.Now
            };

            await _financeService.AddFinanceRecord(finance);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}