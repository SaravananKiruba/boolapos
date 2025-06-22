using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Page_Navigation_App.Services
{
    public class CustomerService
    {
        private readonly AppDbContext _context;
        private readonly FinanceService _financeService;

        public CustomerService(
            AppDbContext context,
            FinanceService financeService)
        {
            _context = context;
            _financeService = financeService;
        }        public async Task<Customer> AddCustomer(Customer customer)
        {
            // Validate mandatory fields before saving
            if (string.IsNullOrWhiteSpace(customer.CustomerName))
                throw new ArgumentException("Customer Name is required.");
                
            if (string.IsNullOrWhiteSpace(customer.PhoneNumber))
                throw new ArgumentException("Phone Number is required.");
                
            if (string.IsNullOrWhiteSpace(customer.CustomerType))
                throw new ArgumentException("Customer Type is required.");
                
            customer.RegistrationDate = DateOnly.FromDateTime(DateTime.Now); 
            customer.IsActive = true;

            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();
            return customer;
        }        public async Task<bool> UpdateCustomer(Customer customer)
        {
            // Validate mandatory fields before updating
            if (string.IsNullOrWhiteSpace(customer.CustomerName))
                throw new ArgumentException("Customer Name is required.");
                
            if (string.IsNullOrWhiteSpace(customer.PhoneNumber))
                throw new ArgumentException("Phone Number is required.");
                
            if (string.IsNullOrWhiteSpace(customer.CustomerType))
                throw new ArgumentException("Customer Type is required.");
            
            var existingCustomer = await _context.Customers.FindAsync(customer.CustomerID);
            if (existingCustomer == null) return false;

            _context.Entry(existingCustomer).CurrentValues.SetValues(customer);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Customer> GetCustomerByPhone(string phoneNumber)
        {
            return await _context.Customers
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);
        }

        public async Task<Customer> GetCustomerById(int customerId)
        {
            return await _context.Customers
                .Include(c => c.Orders)
                .Include(c => c.Payments)
                .FirstOrDefaultAsync(c => c.CustomerID == customerId);
        }

        public async Task<IEnumerable<Customer>> SearchCustomers(
            string searchTerm = null,
            bool? isActive = null)
        {
            var query = _context.Customers
                .Include(c => c.Orders)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(c =>
                    c.CustomerName.Contains(searchTerm) ||
                    c.PhoneNumber.Contains(searchTerm) ||
                    c.Email.Contains(searchTerm) ||
                    c.GSTNumber.Contains(searchTerm));
            }

            if (isActive.HasValue)
            {
                query = query.Where(c => c.IsActive == isActive.Value);
            }

            return await query.ToListAsync();
        }        // Loyalty points methods have been removed as the related properties have been removed from the Customer model

        public async Task<CustomerStats> GetCustomerStats(int customerId)
        {
            var customer = await _context.Customers
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.CustomerID == customerId);

            if (customer == null)
                return null;

            var totalPurchases = customer.Orders.Sum(o => o.GrandTotal);
            var pendingAmount = await _financeService.GetCustomerDues(customerId); 
            return new CustomerStats
            {
                TotalOrders = customer.Orders.Count,
                TotalPurchases = totalPurchases,
                PendingAmount = pendingAmount,
                LastPurchaseDate = customer.Orders
                    .OrderByDescending(o => o.OrderDate)
                    .Select(o => o.OrderDate)
                    .FirstOrDefault(),
                PreferredPaymentMode = customer.Orders
                    .GroupBy(o => o.PaymentType)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault()
            };
        }

        public async Task<IEnumerable<Customer>> GetInactiveCustomers(int daysInactive)
        {
            var cutoffDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-daysInactive));

            return await _context.Customers
                .Include(c => c.Orders)
                .Where(c => !c.Orders.Any(o => o.OrderDate >= cutoffDate))
                .ToListAsync();
        }


        public async Task<Dictionary<string, int>> GetCustomerSegmentation()
        {
            var customers = await _context.Customers
                .Include(c => c.Orders)
                .ToListAsync();

            return new Dictionary<string, int>
            {
                { "Platinum", customers.Count(c => c.Orders.Sum(o => o.GrandTotal) > 500000) },
                { "Gold", customers.Count(c => 
                    {
                        var total = c.Orders.Sum(o => o.GrandTotal);
                        return total >= 200000 && total <= 500000;
                    })
                },
                { "Silver", customers.Count(c => 
                    {
                        var total = c.Orders.Sum(o => o.GrandTotal);
                        return total >= 50000 && total < 200000;
                    })
                },
                { "Regular", customers.Count(c => c.Orders.Sum(o => o.GrandTotal) < 50000) }
            };
        }

        public async Task<IEnumerable<Customer>> GetAllCustomers(bool includeInactive = false)
        {
            var query = _context.Customers.AsQueryable();

            if (!includeInactive)
                query = query.Where(c => c.IsActive);

            return await query
                .Include(c => c.Orders)
                .OrderBy(c => c.CustomerName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Customer>> FilterCustomers(
            string searchTerm = null,
            bool? isActive = null)
        {
            var query = _context.Customers
                .Include(c => c.Orders)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(c =>
                    c.CustomerName.Contains(searchTerm) ||
                    c.PhoneNumber.Contains(searchTerm) ||
                    c.Email.Contains(searchTerm) ||
                    c.GSTNumber.Contains(searchTerm));
            }

            if (isActive.HasValue)
            {
                query = query.Where(c => c.IsActive == isActive.Value);
            }

            return await query.ToListAsync();
        }        public async Task<bool> DeleteCustomer(int customerId)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null) return false;

            try
            {
                // Check for dependencies before deleting
                bool hasOrders = await _context.Orders.AnyAsync(o => o.CustomerID == customerId);
                bool hasPayments = await _context.Finances.AnyAsync(f => f.CustomerID == customerId);

                if (hasOrders ||  hasPayments)
                {
                    // Instead of hard delete, perform a soft delete by marking as inactive
                    customer.IsActive = false;
                }
                else
                {
                    // Hard delete if no dependencies
                    _context.Customers.Remove(customer);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error deleting customer: {ex.Message}");
                
                // Implement a fallback - just mark as inactive
                try
                {
                    customer.IsActive = false;
                    await _context.SaveChangesAsync();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }    public class CustomerStats
    {
        public int TotalOrders { get; set; }
        public decimal TotalPurchases { get; set; }        
        public decimal PendingAmount { get; set; }
        public DateOnly? LastPurchaseDate { get; set; }
        public string PreferredPaymentMode { get; set; }
    }
}