using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Page_Navigation_App.Services
{
    public class RepairJobService
    {
        private readonly AppDbContext _context;

        public RepairJobService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<RepairJob> CreateRepairJob(RepairJob job)
        {
            job.ReceiptDate = DateOnly.FromDateTime(DateTime.Now);
            job.Status = "Pending";
            await _context.RepairJobs.AddAsync(job);
            await _context.SaveChangesAsync();
            return job;
        }

        public async Task<RepairJob> UpdateStatus(int jobId, string newStatus)
        {
            var job = await _context.RepairJobs
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.RepairID == jobId);

            if (job == null) return null;

            job.Status = newStatus;
            
            if (newStatus == "Delivered")
            {
                job.CompletionDate = DateOnly.FromDateTime(DateTime.Now);
            }

            await _context.SaveChangesAsync();
            return job;
        }

        public async Task<bool> UpdateImage(int jobId, string imagePath)
        {
            var job = await _context.RepairJobs.FindAsync(jobId);
            if (job == null) return false;

            job.ImagePath = imagePath;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<RepairJob>> GetPendingJobs()
        {
            return await _context.RepairJobs
                .Where(r => r.Status == "Pending" || r.Status == "In Process")
                .Include(r => r.Customer)
                .OrderBy(r => r.ExpectedDeliveryDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<RepairJob>> GetCustomerRepairHistory(int customerId)
        {
            return await _context.RepairJobs
                .Where(r => r.CustomerId == customerId)
                .OrderByDescending(r => r.ReceiptDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<RepairJob>> GetJobsDueToday()
        {
            var today = DateTime.Now.Date;
            return await _context.RepairJobs
                .Where(r => r.ExpectedDeliveryDate.HasValue && 
                           r.ExpectedDeliveryDate.Value.Date == today &&
                           r.Status != "Delivered")
                .Include(r => r.Customer)
                .ToListAsync();
        }

        public async Task<bool> UpdateFinalAmount(int jobId, decimal finalAmount)
        {
            var job = await _context.RepairJobs.FindAsync(jobId);
            if (job == null) return false;

            job.FinalAmount = finalAmount;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<RepairJob>> SearchJobs(
            string searchTerm,
            DateOnly? startDate = null,
            DateOnly? endDate = null,
            string status = null)
        {
            var query = _context.RepairJobs
                .Include(r => r.Customer)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(r => 
                    r.ItemDescription.Contains(searchTerm) ||
                    r.Customer.CustomerName.Contains(searchTerm) ||
                    r.Customer.PhoneNumber.Contains(searchTerm));
            }

            if (startDate != null)
                query = query.Where(r => r.ReceiptDate >= startDate.Value);

            if (endDate != null)
                query = query.Where(r => r.ReceiptDate <= endDate.Value);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(r => r.Status == status);

            return await query
                .OrderByDescending(r => r.ReceiptDate)
                .ToListAsync();
        }

        public async Task<RepairJob> GetRepairJobById(int jobId)
        {
            return await _context.RepairJobs
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.RepairID == jobId);
        }

        public async Task<IEnumerable<RepairJob>> GetRepairsByDate(DateOnly startDate, DateOnly endDate)
        {
            return await _context.RepairJobs
                .Include(r => r.Customer)
                .Where(r => r.ReceiptDate >= startDate && 
                           r.ReceiptDate <= endDate)
                .OrderByDescending(r => r.ReceiptDate)
                .ToListAsync();
        }
    }
}