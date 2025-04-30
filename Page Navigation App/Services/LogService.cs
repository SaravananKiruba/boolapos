using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Page_Navigation_App.Services
{
    public class LogService
    {
        private readonly AppDbContext _context;
        private readonly string _logDirectory;
        private const string LOG_FILE_PREFIX = "app_log_";

        public LogService(AppDbContext context)
        {
            _context = context;
            _logDirectory = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Logs");
            
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public async Task LogInfo(
            string message,
            string source = "",
            string userId = null)
        {
            await LogMessage(LogLevel.Info, message, source, userId);
        }

        public async Task LogWarning(
            string message,
            string source = "",
            string userId = null)
        {
            await LogMessage(LogLevel.Warning, message, source, userId);
        }

        public async Task LogError(
            string message,
            Exception ex = null,
            string source = "",
            string userId = null)
        {
            var details = ex != null
                ? $"{message}\nException: {ex.Message}\nStack Trace: {ex.StackTrace}"
                : message;

            await LogMessage(LogLevel.Error, details, source, userId);
        }

        private async Task LogMessage(
            LogLevel level,
            string message,
            string source,
            string userId)
        {
            var logEntry = new LogEntry
            {
                Level = level.ToString(),
                Message = message,
                Source = source,
                UserID = userId,
                Timestamp = DateTime.Now
            };

            await _context.LogEntries.AddAsync(logEntry);
            await _context.SaveChangesAsync();

            // Also write to file for backup
            var logFile = Path.Combine(
                _logDirectory,
                $"{LOG_FILE_PREFIX}{DateTime.Now:yyyy-MM-dd}.log");

            await File.AppendAllTextAsync(
                logFile,
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}|{level}|{source}|{userId ?? "N/A"}|{message}\n");
        }

        public async Task LogAudit(
            string action,
            string details,
            string userId = null)
        {
            var auditLog = new AuditLog
            {
                Action = action,
                Details = details,
                UserID = userId,
                Timestamp = DateTime.Now,
                IPAddress = "127.0.0.1" // TODO: Implement proper IP capture
            };

            await _context.AuditLogs.AddAsync(auditLog);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<LogEntry>> GetLogs(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string level = null,
            string source = null)
        {
            var query = _context.LogEntries.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(l => l.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(l => l.Timestamp <= endDate.Value);

            if (!string.IsNullOrEmpty(level))
                query = query.Where(l => l.Level == level);

            if (!string.IsNullOrEmpty(source))
                query = query.Where(l => l.Source == source);

            return await query
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<LogEntry>> GetSystemLogs(DateTime startDate, DateTime endDate)
        {
            return await _context.LogEntries
                .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogs(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string userId = null,
            string action = null)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(l => l.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(l => l.Timestamp <= endDate.Value);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(l => l.UserID == userId);

            if (!string.IsNullOrEmpty(action))
                query = query.Where(l => l.Action == action);

            return await query
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<NotificationLog>> GetNotificationLogs(
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _context.NotificationLog.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(l => l.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(l => l.Timestamp <= endDate.Value);

            return await query
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        public async Task<bool> ClearOldLogs(int daysToKeep)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
            
            // Delete old log entries in batches to avoid timeouts
            var oldLogs = await _context.LogEntries
                .Where(l => l.Timestamp < cutoffDate)
                .ToListAsync();
                
            _context.LogEntries.RemoveRange(oldLogs);
            await _context.SaveChangesAsync();

            // Also clean up log files
            var oldFiles = Directory.GetFiles(_logDirectory, $"{LOG_FILE_PREFIX}*.log")
                .Where(f => File.GetCreationTime(f) < cutoffDate);

            foreach (var file in oldFiles)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    await LogError(
                        $"Failed to delete old log file: {file}",
                        ex,
                        "LogService");
                    return false;
                }
            }

            return true;
        }
    }

    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Debug
    }
}