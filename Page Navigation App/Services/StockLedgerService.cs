using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Page_Navigation_App.Services
{
    public class StockLedgerService
    {
        private readonly AppDbContext _context;

        public StockLedgerService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<StockLedger> AddLedgerEntry(
            int productId,
            decimal quantity,
            string transactionType,
            string referenceId,
            string referenceNumber = null,
            string notes = null)
        {
            try
            {
                // Ensure product exists
                var product = await _context.Products.FindAsync(productId);
                if (product == null) return null;

                var ledgerEntry = new StockLedger
                {
                    ProductID = productId,
                    TransactionDate = DateTime.Now,
                    Quantity = quantity,
                    TransactionType = transactionType,
                    ReferenceID = referenceId,
                    ReferenceNumber = referenceNumber,
                    Notes = notes
                };

                await _context.StockLedgers.AddAsync(ledgerEntry);
                await _context.SaveChangesAsync();
                
                return ledgerEntry;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<IEnumerable<StockLedger>> GetProductLedger(int productId)
        {
            return await _context.StockLedgers
                .Where(l => l.ProductID == productId)
                .OrderByDescending(l => l.TransactionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<StockLedger>> GetLedgerEntries(
            DateTime fromDate, 
            DateTime toDate,
            string transactionType = null)
        {
            var query = _context.StockLedgers
                .Include(l => l.Product)
                .Where(l => l.TransactionDate >= fromDate && l.TransactionDate <= toDate);

            if (!string.IsNullOrEmpty(transactionType))
            {
                query = query.Where(l => l.TransactionType == transactionType);
            }

            return await query.OrderByDescending(l => l.TransactionDate).ToListAsync();
        }

        public async Task<IEnumerable<StockLedger>> GetLedgerByReference(string referenceId)
        {
            return await _context.StockLedgers
                .Include(l => l.Product)
                .Where(l => l.ReferenceID == referenceId)
                .OrderByDescending(l => l.TransactionDate)
                .ToListAsync();
        }

        public async Task<Dictionary<string, decimal>> GetProductMovementSummary(
            DateTime fromDate, 
            DateTime toDate)
        {
            var entries = await _context.StockLedgers
                .Where(l => l.TransactionDate >= fromDate && l.TransactionDate <= toDate)
                .ToListAsync();

            return new Dictionary<string, decimal>
            {
                ["Purchases"] = entries.Where(e => e.TransactionType == "Purchase").Sum(e => e.Quantity),
                ["Sales"] = entries.Where(e => e.TransactionType == "Sale").Sum(e => e.Quantity),
                ["Returns"] = entries.Where(e => e.TransactionType == "Return").Sum(e => e.Quantity),
                ["Exchanges"] = entries.Where(e => e.TransactionType == "Exchange").Sum(e => e.Quantity)
            };
        }
    }
}