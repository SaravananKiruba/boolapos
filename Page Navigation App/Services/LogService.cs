using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;

namespace Page_Navigation_App.Services
{
    /// <summary>
    /// Service for handling application logging including errors, security events, and audit trails
    /// </summary>
    public class LogService
    {
        private readonly AppDbContext _context;
        private readonly string _logFilePath;
        private readonly object _fileLock = new object();

        public LogService(AppDbContext context)
        {
            _context = context;
            
            // Create logs folder if it doesn't exist
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
            
            // Set log file path with date in filename
            _logFilePath = Path.Combine(logDirectory, $"BoolaPOS_{DateTime.Now:yyyy-MM-dd}.log");
        }        /// <summary>
        /// Log error message to database and file
        /// </summary>
        public async Task LogErrorAsync(string message, string userId = null, Exception exception = null)
        {
            try
            {
                // Create log entry
                var logEntry = new LogEntry
                {
                    LogLevel = "ERROR",
                    Message = message,
                    Timestamp = DateTime.Now,
                    UserId = userId,
                    Exception = exception?.ToString(),
                    Source = exception?.Source ?? "BoolaPOS",
                    StackTrace = exception?.StackTrace
                };
                
                // Check if we're already in a transaction to avoid nested transaction errors
                bool inExistingTransaction = _context.Database.CurrentTransaction != null;
                
                // Save to database
                _context.LogEntries.Add(logEntry);
                
                // Only call SaveChangesAsync if we're not in an existing transaction
                // This avoids the "connection is already in a transaction" error
                if (!inExistingTransaction) {
                    await _context.SaveChangesAsync();
                }
                
                // Always log to file for redundancy
                WriteToLogFile("ERROR", message, exception);
            }
            catch (Exception ex)
            {
                // If database logging fails, at least try to write to file
                WriteToLogFile("ERROR", message, exception);
                WriteToLogFile("FATAL", $"Failed to log to database: {ex.Message}", ex);
            }
        }        /// <summary>
        /// Log warning message
        /// </summary>
        public async Task LogWarningAsync(string message, string userId = null)
        {
            try
            {
                var logEntry = new LogEntry
                {
                    LogLevel = "WARNING",
                    Message = message,
                    Timestamp = DateTime.Now,
                    UserId = userId,
                    Source = "BoolaPOS" // Adding the required Source property
                };
                
                // Check if we're already in a transaction to avoid nested transaction errors
                bool inExistingTransaction = _context.Database.CurrentTransaction != null;
                
                _context.LogEntries.Add(logEntry);
                
                // Only call SaveChangesAsync if we're not in an existing transaction
                if (!inExistingTransaction) {
                    await _context.SaveChangesAsync();
                }
                
                WriteToLogFile("WARNING", message);
            }
            catch (Exception ex)
            {
                WriteToLogFile("WARNING", message);
                WriteToLogFile("ERROR", $"Failed to log warning to database: {ex.Message}", ex);
            }
        }        /// <summary>
        /// Log information message
        /// </summary>
        public async Task LogInformationAsync(string message, string userId = null)
        {
            try
            {
                var logEntry = new LogEntry
                {
                    LogLevel = "INFO",
                    Message = message,
                    Timestamp = DateTime.Now,
                    UserId = userId,
                    Source = "BoolaPOS" // Adding the required Source property
                };
                
                // Check if we're already in a transaction to avoid nested transaction errors
                bool inExistingTransaction = _context.Database.CurrentTransaction != null;
                
                _context.LogEntries.Add(logEntry);
                
                // Only call SaveChangesAsync if we're not in an existing transaction
                if (!inExistingTransaction) {
                    await _context.SaveChangesAsync();
                }
                
                WriteToLogFile("INFO", message);
            }
            catch (Exception ex)
            {
                WriteToLogFile("INFO", message);
                WriteToLogFile("ERROR", $"Failed to log info to database: {ex.Message}", ex);
            }
        }        /// <summary>
        /// Log security event (login, permissions change, etc.)
        /// </summary>
        public async Task LogSecurityEventAsync(string action, string userId, string details, bool isSuccessful)
        {
            try
            {
                var securityLog = new SecurityLog
                {
                    Action = action,
                    UserId = userId,
                    Details = details,
                    Timestamp = DateTime.Now,
                    IsSuccessful = isSuccessful,
                    IpAddress = GetClientIpAddress()
                };
                
                // Check if we're already in a transaction to avoid nested transaction errors
                bool inExistingTransaction = _context.Database.CurrentTransaction != null;
                
                _context.SecurityLogs.Add(securityLog);
                
                // Only call SaveChangesAsync if we're not in an existing transaction
                if (!inExistingTransaction) {
                    await _context.SaveChangesAsync();
                }
                
                string logMessage = $"SECURITY: {action} - User: {userId} - Success: {isSuccessful} - {details}";
                WriteToLogFile("SECURITY", logMessage);
            }
            catch (Exception ex)
            {
                string logMessage = $"SECURITY: {action} - User: {userId} - Success: {isSuccessful} - {details}";
                WriteToLogFile("SECURITY", logMessage);
                WriteToLogFile("ERROR", $"Failed to log security event to database: {ex.Message}", ex);
            }
        }        /// <summary>
        /// Log audit information for data changes
        /// </summary>
        public async Task LogAuditAsync(string entityName, int entityId, string action, string userId, string oldValues, string newValues)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    EntityName = entityName,
                    EntityId = entityId.ToString(),
                    Action = action,
                    UserId = userId,
                    Timestamp = DateTime.Now,
                    OldValues = oldValues,
                    NewValues = newValues
                };
                
                // Check if we're already in a transaction to avoid nested transaction errors
                bool inExistingTransaction = _context.Database.CurrentTransaction != null;
                
                _context.AuditLogs.Add(auditLog);
                
                // Only call SaveChangesAsync if we're not in an existing transaction
                if (!inExistingTransaction) {
                    await _context.SaveChangesAsync();
                }
                
                string logMessage = $"AUDIT: {action} on {entityName} #{entityId} by {userId}";
                WriteToLogFile("AUDIT", logMessage);
            }
            catch (Exception ex)
            {
                string logMessage = $"AUDIT: {action} on {entityName} #{entityId} by {userId}";
                WriteToLogFile("AUDIT", logMessage);
                WriteToLogFile("ERROR", $"Failed to log audit event to database: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get recent error logs
        /// </summary>
        public async Task<LogEntry[]> GetRecentErrorLogsAsync(int count = 100)
        {
            try
            {
                return await _context.LogEntries
                    .Where(l => l.LogLevel == "ERROR")
                    .OrderByDescending(l => l.Timestamp)
                    .Take(count)
                    .ToArrayAsync();
            }
            catch (Exception ex)
            {
                WriteToLogFile("ERROR", $"Failed to get recent error logs: {ex.Message}", ex);
                return Array.Empty<LogEntry>();
            }
        }

        /// <summary>
        /// Get security logs for a user
        /// </summary>
        public async Task<SecurityLog[]> GetUserSecurityLogsAsync(string userId, int count = 100)
        {
            try
            {
                return await _context.SecurityLogs
                    .Where(l => l.UserId == userId)
                    .OrderByDescending(l => l.Timestamp)
                    .Take(count)
                    .ToArrayAsync();
            }
            catch (Exception ex)
            {
                WriteToLogFile("ERROR", $"Failed to get security logs for user {userId}: {ex.Message}", ex);
                return Array.Empty<SecurityLog>();
            }
        }

        /// <summary>
        /// Get audit logs for an entity
        /// </summary>
        public async Task<AuditLog[]> GetEntityAuditLogsAsync(string entityName, int entityId, int count = 100)
        {
            try
            {
                return await _context.AuditLogs
                    .Where(l => l.EntityName == entityName && l.EntityId == entityId.ToString())
                    .OrderByDescending(l => l.Timestamp)
                    .Take(count)
                    .ToArrayAsync();
            }
            catch (Exception ex)
            {
                WriteToLogFile("ERROR", $"Failed to get audit logs for {entityName} #{entityId}: {ex.Message}", ex);
                return Array.Empty<AuditLog>();
            }
        }

        /// <summary>
        /// Get system logs
        /// </summary>
        public List<LogEntry> GetSystemLogs(int count = 100)
        {
            try
            {
                return _context.LogEntries
                    .OrderByDescending(l => l.Timestamp)
                    .Take(count)
                    .ToList();
            }
            catch (Exception ex)
            {
                WriteToLogFile("ERROR", $"Failed to get system logs: {ex.Message}", ex);
                return new List<LogEntry>();
            }
        }

        /// <summary>
        /// Get audit logs
        /// </summary>
        public List<AuditLog> GetAuditLogs(int count = 100)
        {
            try
            {
                return _context.AuditLogs
                    .OrderByDescending(l => l.Timestamp)
                    .Take(count)
                    .ToList();
            }
            catch (Exception ex)
            {
                WriteToLogFile("ERROR", $"Failed to get audit logs: {ex.Message}", ex);
                return new List<AuditLog>();
            }
        }

        /// <summary>
        /// Write log message to file
        /// </summary>
        private void WriteToLogFile(string level, string message, Exception exception = null)
        {
            try
            {
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
                
                if (exception != null)
                {
                    logEntry += $"\nException: {exception.Message}";
                    logEntry += $"\nSource: {exception.Source}";
                    logEntry += $"\nStack Trace: {exception.StackTrace}";
                    
                    if (exception.InnerException != null)
                    {
                        logEntry += $"\nInner Exception: {exception.InnerException.Message}";
                    }
                }
                
                lock (_fileLock)
                {
                    File.AppendAllText(_logFilePath, logEntry + "\n\n");
                }
            }
            catch
            {
                // Last resort fallback if file logging fails
                Console.Error.WriteLine($"CRITICAL: Failed to write to log file: {level} - {message}");
            }
        }

        /// <summary>
        /// Get client IP address (mock implementation)
        /// </summary>
        private string GetClientIpAddress()
        {
            // In a real application, this would get the client's IP address
            // For a desktop application, this might be the local machine's IP
            return "127.0.0.1";
        }

        /// <summary>
        /// Clean up old logs (logs older than specified days)
        /// </summary>
        public async Task CleanupOldLogsAsync(int olderThanDays = 90)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-olderThanDays);
                
                // Clean up database logs
                var oldLogs = await _context.LogEntries
                    .Where(l => l.Timestamp < cutoffDate)
                    .ToListAsync();
                
                _context.LogEntries.RemoveRange(oldLogs);
                
                var oldSecurityLogs = await _context.SecurityLogs
                    .Where(l => l.Timestamp < cutoffDate)
                    .ToListAsync();
                
                _context.SecurityLogs.RemoveRange(oldSecurityLogs);
                
                // Don't delete audit logs automatically as they're important for compliance
                
                await _context.SaveChangesAsync();
                  // Clean up log files
                string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (Directory.Exists(logDirectory))
                {
                    foreach (var file in Directory.GetFiles(logDirectory, "BoolaPOS_*.log"))
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        if (fileInfo.CreationTime < cutoffDate)
                        {
                            fileInfo.Delete();
                        }
                    }
                }
                
                // Write directly to log file instead of using LogInformationAsync to avoid potential recursion
                WriteToLogFile("INFO", $"Cleaned up logs older than {olderThanDays} days");
            }
            catch (Exception ex)
            {
                WriteToLogFile("ERROR", $"Failed to clean up old logs: {ex.Message}", ex);
            }
        }        /// <summary>
        /// Log warning message (synchronous version for compatibility)
        /// </summary>
        public void LogWarning(string message, string userId = null)
        {
            try
            {
                var logEntry = new LogEntry
                {
                    LogLevel = "WARNING",
                    Message = message,
                    Timestamp = DateTime.Now,
                    UserId = userId,
                    Source = "BoolaPOS" // Adding the required Source property
                };
                
                // Check if we're already in a transaction to avoid nested transaction errors
                bool inExistingTransaction = _context.Database.CurrentTransaction != null;
                
                _context.LogEntries.Add(logEntry);
                
                // Only call SaveChanges if we're not in an existing transaction
                if (!inExistingTransaction) {
                    _context.SaveChanges(); // Synchronous version
                }
                
                WriteToLogFile("WARNING", message);
            }
            catch (Exception ex)
            {
                WriteToLogFile("WARNING", message);
                WriteToLogFile("ERROR", $"Failed to log warning to database: {ex.Message}", ex);
            }
        }        /// <summary>
        /// Log warning message with source (for SecurityService compatibility)
        /// </summary>
        public void LogWarning(string message, string source, string userId = null)
        {
            try
            {
                var logEntry = new LogEntry
                {
                    LogLevel = "WARNING",
                    Message = message,
                    Timestamp = DateTime.Now,
                    UserId = userId,
                    Source = source ?? "BoolaPOS" // Using the provided source value with fallback
                };
                
                // Check if we're already in a transaction to avoid nested transaction errors
                bool inExistingTransaction = _context.Database.CurrentTransaction != null;
                
                _context.LogEntries.Add(logEntry);
                
                // Only call SaveChanges if we're not in an existing transaction
                if (!inExistingTransaction) {
                    _context.SaveChanges(); // Synchronous version
                }
                
                WriteToLogFile("WARNING", message);
            }
            catch (Exception ex)
            {
                WriteToLogFile("WARNING", message);
                WriteToLogFile("ERROR", $"Failed to log warning to database: {ex.Message}", ex);
            }
        }        /// <summary>
        /// Log error message (synchronous version for compatibility)
        /// </summary>
        public void LogError(string message, string userId = null, Exception exception = null)
        {
            try
            {
                // Create log entry
                var logEntry = new LogEntry
                {
                    LogLevel = "ERROR",
                    Message = message,
                    Timestamp = DateTime.Now,
                    UserId = userId,
                    Exception = exception?.ToString(),
                    Source = exception?.Source ?? "BoolaPOS",
                    StackTrace = exception?.StackTrace
                };
                
                // Check if we're already in a transaction to avoid nested transaction errors
                bool inExistingTransaction = _context.Database.CurrentTransaction != null;
                
                // Save to database
                _context.LogEntries.Add(logEntry);
                
                // Only call SaveChanges if we're not in an existing transaction
                if (!inExistingTransaction) {
                    _context.SaveChanges(); // Synchronous version
                }
                
                // Also log to file for redundancy
                WriteToLogFile("ERROR", message, exception);
            }
            catch (Exception ex)
            {
                // If database logging fails, at least try to write to file
                WriteToLogFile("ERROR", message, exception);
                WriteToLogFile("FATAL", $"Failed to log to database: {ex.Message}", ex);
            }
        }        /// <summary>
        /// Log information message (synchronous version for compatibility)
        /// </summary>
        public void LogInformation(string message, string userId = null)
        {
            try
            {
                var logEntry = new LogEntry
                {
                    LogLevel = "INFO",
                    Message = message,
                    Timestamp = DateTime.Now,
                    UserId = userId,
                    Source = "BoolaPOS" // Adding the required Source property
                };
                
                // Check if we're already in a transaction to avoid nested transaction errors
                bool inExistingTransaction = _context.Database.CurrentTransaction != null;
                
                _context.LogEntries.Add(logEntry);
                
                // Only call SaveChanges if we're not in an existing transaction
                if (!inExistingTransaction) {
                    _context.SaveChanges(); // Synchronous version
                }
                
                WriteToLogFile("INFO", message);
            }
            catch (Exception ex)
            {
                WriteToLogFile("INFO", message);
                WriteToLogFile("ERROR", $"Failed to log info to database: {ex.Message}", ex);
            }
        }
    }
}