using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Page_Navigation_App.Services
{
    public class RepairService
    {
        private readonly AppDbContext _context;

        public RepairService(AppDbContext context)
        {
            _context = context;
        }

        // Create a new repair job
        public async Task<RepairJob> CreateRepairJob(RepairJob repairJob)
        {
            try
            {
                repairJob.ReceiptDate = DateTime.Now;
                repairJob.Status = "Pending";
                
                await _context.RepairJobs.AddAsync(repairJob);
                await _context.SaveChangesAsync();
                
                return repairJob;
            }
            catch
            {
                return null;
            }
        }

        // Get a repair job by ID
        public async Task<RepairJob> GetRepairJobById(int repairJobId)
        {
            return await _context.RepairJobs
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.RepairJobID == repairJobId);
        }

        // Get all repair jobs with filtering options
        public async Task<IEnumerable<RepairJob>> GetRepairJobs(
            string status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var query = _context.RepairJobs
                .Include(r => r.Customer)
                .AsQueryable();
                
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }
            
            if (fromDate.HasValue)
            {
                query = query.Where(r => r.ReceiptDate >= fromDate.Value);
            }
            
            if (toDate.HasValue)
            {
                query = query.Where(r => r.ReceiptDate <= toDate.Value);
            }
            
            return await query
                .OrderByDescending(r => r.ReceiptDate)
                .ToListAsync();
        }

        // Get repair jobs for a specific customer
        public async Task<IEnumerable<RepairJob>> GetCustomerRepairJobs(int customerId)
        {
            return await _context.RepairJobs
                .Where(r => r.CustomerID == customerId)
                .OrderByDescending(r => r.ReceiptDate)
                .ToListAsync();
        }

        // Update repair job status
        public async Task<bool> UpdateRepairJobStatus(
            int repairJobId, 
            string newStatus, 
            string notes = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var repairJob = await _context.RepairJobs
                    .Include(r => r.Customer)
                    .FirstOrDefaultAsync(r => r.RepairJobID == repairJobId);
                    
                if (repairJob == null) return false;
                
                var oldStatus = repairJob.Status;
                repairJob.Status = newStatus;
                
                // Add notes with timestamp
                if (!string.IsNullOrEmpty(notes))
                {
                    repairJob.Notes = (string.IsNullOrEmpty(repairJob.Notes) ? "" : repairJob.Notes + "\n") +
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm}] {newStatus}: {notes}";
                }
                
                // Set appropriate dates based on status
                if (newStatus == "In Process" && oldStatus == "Pending")
                {
                    repairJob.WorkStartDate = DateTime.Now;
                }
                else if (newStatus == "Completed" && (oldStatus == "Pending" || oldStatus == "In Process"))
                {
                    repairJob.CompletionDate = DateTime.Now;
                }
                else if (newStatus == "Delivered")
                {
                    repairJob.DeliveryDate = DateTime.Now;
                }
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        // Update repair details
        public async Task<bool> UpdateRepairJobDetails(RepairJob repairJob)
        {
            try
            {
                var existingJob = await _context.RepairJobs.FindAsync(repairJob.RepairJobID);
                if (existingJob == null) return false;
                
                // Only update certain fields to preserve history
                existingJob.ItemDescription = repairJob.ItemDescription;
                existingJob.WorkType = repairJob.WorkType;
                existingJob.EstimatedCost = repairJob.EstimatedCost;
                existingJob.EstimatedDeliveryDate = repairJob.EstimatedDeliveryDate;
                existingJob.ItemPhotoUrl = repairJob.ItemPhotoUrl;
                
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Complete a repair job with payment details
        public async Task<bool> CompleteRepairJob(
            int repairJobId, 
            decimal finalCost, 
            string paymentMethod,
            string notes = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var repairJob = await _context.RepairJobs.FindAsync(repairJobId);
                if (repairJob == null) return false;
                
                repairJob.Status = "Delivered";
                repairJob.DeliveryDate = DateTime.Now;
                repairJob.FinalCost = finalCost;
                repairJob.PaymentMethod = paymentMethod;
                
                if (!string.IsNullOrEmpty(notes))
                {
                    repairJob.Notes = (string.IsNullOrEmpty(repairJob.Notes) ? "" : repairJob.Notes + "\n") +
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm}] Delivered: {notes}";
                }
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        // Get repair statistics
        public async Task<Dictionary<string, object>> GetRepairStatistics(
            DateTime fromDate,
            DateTime toDate)
        {
            var repairJobs = await _context.RepairJobs
                .Where(r => r.ReceiptDate >= fromDate && r.ReceiptDate <= toDate)
                .ToListAsync();
                
            return new Dictionary<string, object>
            {
                ["TotalRepairs"] = repairJobs.Count,
                ["PendingRepairs"] = repairJobs.Count(r => r.Status == "Pending"),
                ["InProcessRepairs"] = repairJobs.Count(r => r.Status == "In Process"),
                ["CompletedRepairs"] = repairJobs.Count(r => r.Status == "Completed" || r.Status == "Delivered"),
                ["AverageCompletionTime"] = repairJobs
                    .Where(r => r.CompletionDate.HasValue && r.ReceiptDate.HasValue)
                    .Select(r => (r.CompletionDate.Value - r.ReceiptDate.Value).TotalDays)
                    .DefaultIfEmpty(0)
                    .Average(),
                ["TotalRevenue"] = repairJobs
                    .Where(r => r.Status == "Delivered" && r.FinalCost.HasValue)
                    .Sum(r => r.FinalCost.Value)
            };
        }
    }
}