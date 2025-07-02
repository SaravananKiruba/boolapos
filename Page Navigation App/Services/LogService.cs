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
                
                if (!inExistingTransaction)
                {
                    // Only save to database if we're not in an existing transaction
                    try
                    {
                        _context.LogEntries.Add(logEntry);
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception dbEx)
                    {
                        // If database logging fails, at least write to file
                        WriteToLogFile("ERROR", $"Database logging failed: {dbEx.Message}");
                    }
                }
                
                // Always log to file for redundancy
                WriteToLogFile("ERROR", message, exception);
            }
            catch (Exception ex)
            {
                // If all else fails, at least try to write to file
                WriteToLogFile("FATAL", $"Complete logging failure: {ex.Message}", ex);
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
                    Source = "BoolaPOS"
                };
                
                // Check if we're already in a transaction to avoid nested transaction errors
                bool inExistingTransaction = _context.Database.CurrentTransaction != null;
                
                if (!inExistingTransaction)
                {
                    // Only save to database if we're not in an existing transaction
                    try
                    {
                        _context.LogEntries.Add(logEntry);
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception dbEx)
                    {
                        // If database logging fails, at least write to file
                        WriteToLogFile("ERROR", $"Database logging failed: {dbEx.Message}");
                    }
                }
                
                WriteToLogFile("INFO", message);
            }
            catch (Exception ex)
            {
                WriteToLogFile("INFO", message);
                WriteToLogFile("ERROR", $"Failed to log info: {ex.Message}", ex);
            }
        }        /// <summary>
        /// Log security event (login, permissions change, etc.)
        /// </summary>
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
        }        /// <summary>
        /// Add a log entry with specified module, message, and level
        /// </summary>
        public async Task AddLogEntryAsync(string module, string message, string logLevel)
        {
            try
            {
                // Create log entry
                var logEntry = new LogEntry
                {
                    LogLevel = logLevel.ToUpper(),
                    Message = message,
                    Timestamp = DateTime.Now,
                    Source = module
                };
                
                // Add to database
                await _context.LogEntries.AddAsync(logEntry);
                await _context.SaveChangesAsync();
                
                // Also write to log file
                WriteToLogFile(logLevel.ToUpper(), $"[{module}] {message}");
            }
            catch (Exception ex)
            {
                // If we can't write to the database, at least try to write to the log file
                WriteToLogFile("ERROR", $"Failed to log message: {ex.Message}", ex);
            }
        }
    }
}