using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;

namespace Page_Navigation_App.Services
{
    public class SecurityService
    {
        private readonly AppDbContext _context;
        private readonly LogService _logService;
        private readonly ConfigurationService _configService;

        public SecurityService(
            AppDbContext context,
            LogService logService,
            ConfigurationService configService)
        {
            _context = context;
            _logService = logService;
            _configService = configService;
        }

        public async Task<bool> CheckPermission(string userId, string action)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.UserID.ToString() == userId);

            if (user == null || !user.IsActive)
                return false;

            // Admin role has all permissions
            if (user.Roles.Any(r => r.RoleName == "Admin"))
                return true;

            var permissionMatrix = GetPermissionMatrix();
            foreach (var role in user.Roles)
            {
                if (permissionMatrix.TryGetValue(role.RoleName, out var permissions) &&
                    (permissions.Contains(action) || permissions.Contains("*")))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task LogAction(
            string userId,
            string action,
            string description,
            bool isSuccessful)
        {
            var securityLog = new SecurityLog
            {
                Timestamp = DateTime.Now,
                UserID = userId,
                Action = action,
                Description = description,
                IsSuccessful = isSuccessful,
                IPAddress = GetCurrentIPAddress(),
                UserAgent = GetCurrentUserAgent()
            };

            await _context.SecurityLogs.AddAsync(securityLog);
            await _context.SaveChangesAsync();

            if (!isSuccessful)
            {
                await _logService.LogWarning(
                    $"Security event: {action} - {description}",
                    "SecurityService",
                    userId);
            }
        }

        public async Task<IEnumerable<SecurityLog>> GetSecurityLogs(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string userId = null,
            string action = null,
            bool? isSuccessful = null)
        {
            var query = _context.SecurityLogs.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(l => l.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(l => l.Timestamp <= endDate.Value);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(l => l.UserID == userId);

            if (!string.IsNullOrEmpty(action))
                query = query.Where(l => l.Action == action);

            if (isSuccessful.HasValue)
                query = query.Where(l => l.IsSuccessful == isSuccessful.Value);

            return await query
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        public Dictionary<string, List<string>> GetPermissionMatrix()
        {
            // This could be loaded from configuration or database
            return new Dictionary<string, List<string>>
            {
                {
                    "Admin",
                    new List<string> { "*" } // All permissions
                },
                {
                    "Manager",
                    new List<string>
                    {
                        "ViewReports",
                        "ManageUsers",
                        "ManageProducts",
                        "ManageStock",
                        "ManageOrders",
                        "ManageCustomers",
                        "ManageRepairs",
                        "ViewRates",
                        "UpdateRates",
                        "ProcessPayments",
                        "ManageSuppliers",
                        "ViewAuditLogs"
                    }
                },
                {
                    "Sales",
                    new List<string>
                    {
                        "ViewProducts",
                        "CreateOrders",
                        "ViewCustomers",
                        "CreateCustomers",
                        "ViewRates",
                        "ProcessPayments",
                        "CreateRepairs",
                        "UpdateRepairs"
                    }
                }
            };
        }

        public async Task<Dictionary<string, int>> GetSecurityStats(
            DateTime startDate,
            DateTime endDate)
        {
            var logs = await _context.SecurityLogs
                .Where(l => l.Timestamp >= startDate && 
                           l.Timestamp <= endDate)
                .ToListAsync();

            return new Dictionary<string, int>
            {
                { "TotalEvents", logs.Count },
                { "FailedEvents", logs.Count(l => !l.IsSuccessful) },
                { "UniqueUsers", logs.Select(l => l.UserID).Distinct().Count() },
                { "LoginAttempts", logs.Count(l => l.Action == "LoginAttempt") },
                { "FailedLogins", logs.Count(l => 
                    l.Action == "LoginAttempt" && !l.IsSuccessful) }
            };
        }

        private string GetCurrentIPAddress()
        {
            // Implementation would depend on your web framework
            return "127.0.0.1"; // Placeholder
        }

        private string GetCurrentUserAgent()
        {
            // Implementation would depend on your web framework
            return "Unknown"; // Placeholder
        }
    }
}