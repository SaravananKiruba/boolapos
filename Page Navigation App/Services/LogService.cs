using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.IO;

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
            var fullMessage = message;
            if (ex != null)
            {
                fullMessage += $"\nException: {ex.Message}";
                fullMessage += $"\nStack Trace: {ex.StackTrace}";
                if (ex.InnerException != null)
                {
                    fullMessage += $"\nInner Exception: {ex.InnerException.Message}";
                }
            }

            await LogMessage(LogLevel.Error, fullMessage, source, userId);
        }

        public async Task LogAudit(
            string action,
            string details,
            string userId = null)
        {
            var audit = new AuditLog
            {
                Timestamp = DateTime.Now,
                UserID = userId,
                Action = action,
                Details = details
            };

            await _context.AuditLogs.AddAsync(audit);
            await _context.SaveChangesAsync();

            // Also log to file system for redundancy
            await LogMessage(
                LogLevel.Audit,
                $"Audit: {action} - {details}",
                "AuditLog",
                userId);
        }

        public async Task<IEnumerable<LogEntry>> GetLogs(
            DateTime? startDate = null,
            DateTime? endDate = null,
            LogLevel? level = null,
            string source = null,
            string userId = null)
        {
            var query = _context.LogEntries.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(l => l.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(l => l.Timestamp <= endDate.Value);

            if (level.HasValue)
                query = query.Where(l => l.Level == level.Value);

            if (!string.IsNullOrEmpty(source))
                query = query.Where(l => l.Source == source);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(l => l.UserID == userId);

            return await query
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

        public async Task CleanupOldLogs(int daysToKeep = 90)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);

            // Clean up database logs
            await _context.LogEntries
                .Where(l => l.Timestamp < cutoffDate)
                .ExecuteDeleteAsync();

            // Clean up file system logs
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
                }
            }
        }

        private async Task LogMessage(
            LogLevel level,
            string message,
            string source,
            string userId)
        {
            var logEntry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message,
                Source = source,
                UserID = userId
            };

            // Log to database
            await _context.LogEntries.AddAsync(logEntry);
            await _context.SaveChangesAsync();

            // Log to file system
            var logFile = Path.Combine(
                _logDirectory,
                $"{LOG_FILE_PREFIX}{DateTime.Now:yyyy-MM-dd}.log");

            var logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                         $"[{level}] " +
                         $"[{source}] " +
                         $"[User: {userId ?? "System"}] " +
                         $"{message}";

            try
            {
                await File.AppendAllLinesAsync(logFile, new[] { logLine });
            }
            catch (Exception ex)
            {
                // If file logging fails, at least we have the database log
                var errorEntry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Level = LogLevel.Error,
                    Message = $"Failed to write to log file: {ex.Message}",
                    Source = "LogService",
                    UserID = userId
                };

                await _context.LogEntries.AddAsync(errorEntry);
                await _context.SaveChangesAsync();
            }
        }
    }

    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Audit
    }

    public class LogEntry
    {
        public int ID { get; set; }
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; }
        public string Source { get; set; }
        public string UserID { get; set; }
    }

    public class AuditLog
    {
        public int ID { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserID { get; set; }
        public string Action { get; set; }
        public string Details { get; set; }
    }
}