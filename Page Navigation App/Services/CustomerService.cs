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
        private readonly NotificationService _notificationService;
        private readonly FinanceService _financeService;

        public CustomerService(
            AppDbContext context,
            NotificationService notificationService,
            FinanceService financeService)
        {
            _context = context;
            _notificationService = notificationService;
            _financeService = financeService;
        }

        public async Task<Customer> AddCustomer(Customer customer)
        {
            customer.RegistrationDate = DateTime.Now;
            customer.LoyaltyPoints = 0;
            customer.IsActive = true;

            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();
            return customer;
        }

        public async Task<bool> UpdateCustomer(Customer customer)
        {
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
                .Include(c => c.RepairJobs)
                .FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);
        }

        public async Task<IEnumerable<Customer>> SearchCustomers(
            string searchTerm = null,
            bool? isActive = null)
        {
            var query = _context.Customers
                .Include(c => c.Orders)
                .Include(c => c.RepairJobs)
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
        }

        public async Task<bool> AddLoyaltyPoints(int customerId, decimal purchaseAmount)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null) return false;

            // Add 1 point per 100 rupees spent
            int pointsToAdd = (int)(purchaseAmount / 100);
            customer.LoyaltyPoints += pointsToAdd;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RedeemLoyaltyPoints(
            int customerId, 
            int pointsToRedeem,
            decimal discountValue)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null || customer.LoyaltyPoints < pointsToRedeem)
                return false;

            customer.LoyaltyPoints -= pointsToRedeem;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task CheckAndSendBirthdayWishes()
        {
            var today = DateTime.Now.Date;
            var customers = await _context.Customers
                .Where(c => c.DateOfBirth.HasValue &&
                           c.DateOfBirth.Value.Month == today.Month &&
                           c.DateOfBirth.Value.Day == today.Day &&
                           c.IsActive)
                .ToListAsync();

            foreach (var customer in customers)
            {
                await _notificationService.SendBirthdayWishes(customer);
            }
        }

        public async Task CheckAndSendAnniversaryWishes()
        {
            var today = DateTime.Now.Date;
            var customers = await _context.Customers
                .Where(c => c.DateOfAnniversary.HasValue &&
                           c.DateOfAnniversary.Value.Month == today.Month &&
                           c.DateOfAnniversary.Value.Day == today.Day &&
                           c.IsActive)
                .ToListAsync();

            foreach (var customer in customers)
            {
                await _notificationService.SendAnniversaryWishes(customer);
            }
        }

        public async Task<CustomerStats> GetCustomerStats(int customerId)
        {
            var customer = await _context.Customers
                .Include(c => c.Orders)
                .Include(c => c.RepairJobs)
                .FirstOrDefaultAsync(c => c.CustomerID == customerId);

            if (customer == null)
                return null;

            var totalPurchases = customer.Orders.Sum(o => o.GrandTotal);
            var totalRepairs = customer.RepairJobs.Sum(r => r.FinalAmount);
            var pendingAmount = await _financeService.GetCustomerDues(customerId);

            return new CustomerStats
            {
                TotalOrders = customer.Orders.Count,
                TotalPurchases = totalPurchases,
                TotalRepairJobs = customer.RepairJobs.Count,
                TotalRepairAmount = totalRepairs,
                LoyaltyPoints = customer.LoyaltyPoints,
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
            var cutoffDate = DateTime.Now.AddDays(-daysInactive);
            
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

        public async Task<IEnumerable<Customer>> GetAllCustomers()
        {
            return await _context.Customers
                .Include(c => c.Orders)
                .Include(c => c.RepairJobs)
                .Include(c => c.Payments)
                .OrderBy(c => c.CustomerName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Customer>> FilterCustomers(
            string searchTerm = null,
            bool? isActive = null)
        {
            var query = _context.Customers
                .Include(c => c.Orders)
                .Include(c => c.RepairJobs)
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
        }
    }

    public class CustomerStats
    {
        public int TotalOrders { get; set; }
        public decimal TotalPurchases { get; set; }
        public int TotalRepairJobs { get; set; }
        public decimal TotalRepairAmount { get; set; }
        public int LoyaltyPoints { get; set; }
        public decimal PendingAmount { get; set; }
        public DateTime? LastPurchaseDate { get; set; }
        public string PreferredPaymentMode { get; set; }
    }
}