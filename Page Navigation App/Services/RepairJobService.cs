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
            job.ReceiptDate = DateTime.Now;
            job.Status = "Pending";
            await _context.RepairJobs.AddAsync(job);
            await _context.SaveChangesAsync();
            return job;
        }

        public async Task<RepairJob> UpdateStatus(int jobId, string newStatus)
        {
            var job = await _context.RepairJobs
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.RepairID == jobId); // Changed from RepairJobID to RepairID

            if (job == null) return null;

            job.Status = newStatus;
            
            if (newStatus == "In Process")
            {
                if (!job.SMSNotificationSent)
                {
                    await SendSMSNotification(job, "Your repair work has started.");
                    job.SMSNotificationSent = true;
                }
            }
            else if (newStatus == "Delivered")
            {
                job.CompletionDate = DateTime.Now;
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
                .OrderBy(r => r.ExpectedDeliveryDate) // Changed from PromisedDate to ExpectedDeliveryDate
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
            DateTime? startDate = null,
            DateTime? endDate = null,
            string status = null)
        {
            var query = _context.RepairJobs
                .Include(r => r.Customer)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(r => 
                    r.ItemDescription.Contains(searchTerm) || // Changed from ItemDetails to ItemDescription
                    r.Customer.CustomerName.Contains(searchTerm) ||
                    r.Customer.PhoneNumber.Contains(searchTerm));
            }

            if (startDate != null)
                query = query.Where(r => r.ReceiptDate.Date >= startDate.Value.Date);

            if (endDate != null)
                query = query.Where(r => r.ReceiptDate.Date <= endDate.Value.Date);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(r => r.Status == status);

            return await query
                .OrderByDescending(r => r.ReceiptDate)
                .ToListAsync();
        }

        private async Task SendSMSNotification(RepairJob job, string message)
        {
            // TODO: Implement actual SMS sending logic
            // This is a placeholder for SMS integration
            await Task.CompletedTask;
        }

        private async Task SendWhatsAppNotification(RepairJob job, string message)
        {
            // TODO: Implement actual WhatsApp sending logic
            // This is a placeholder for WhatsApp integration
            await Task.CompletedTask;
        }

        public async Task<bool> SendStatusUpdateNotification(int jobId)
        {
            var job = await _context.RepairJobs
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.RepairID == jobId); // Changed from RepairJobID to RepairID

            if (job == null) return false;

            string message = $"Update on your repair job: {job.ItemDescription}\nStatus: {job.Status}"; // Changed from ItemDetails to ItemDescription
            if (job.Status == "Delivered")
            {
                message += $"\nFinal Amount: {job.FinalAmount:C}";
            }

            await SendSMSNotification(job, message);
            await SendWhatsAppNotification(job, message);

            job.SMSNotificationSent = true;
            job.WhatsAppNotificationSent = true;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<RepairJob> GetRepairJobById(int jobId)
        {
            return await _context.RepairJobs
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.RepairID == jobId); // Changed from RepairJobID to RepairID
        }

        public async Task<IEnumerable<RepairJob>> GetRepairsByDate(DateTime startDate, DateTime endDate)
        {
            return await _context.RepairJobs
                .Include(r => r.Customer)
                .Where(r => r.ReceiptDate.Date >= startDate.Date && 
                           r.ReceiptDate.Date <= endDate.Date)
                .OrderByDescending(r => r.ReceiptDate)
                .ToListAsync();
        }
    }
}