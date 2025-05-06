using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Page_Navigation_App.Model;

namespace Page_Navigation_App.Services
{
    /// <summary>
    /// Service for handling application logging including errors, security events, and audit trails
    /// </summary>
    public class LogService
    {
        private readonly string _logFilePath;
        private readonly object _lockObj = new object();

        public LogService()
        {
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            
            // Create logs directory if it doesn't exist
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
            
            // Set log file path with date in filename
            _logFilePath = Path.Combine(logDirectory, $"BoolaPOS_{DateTime.Now:yyyyMMdd}.log");
        }

        /// <summary>
        /// Log information message
        /// </summary>
        public void LogInfo(string message)
        {
            WriteLog("INFO", message);
        }

        /// <summary>
        /// Log warning message
        /// </summary>
        public void LogWarning(string message)
        {
            WriteLog("WARNING", message);
        }

        /// <summary>
        /// Log error message
        /// </summary>
        public void LogError(string message)
        {
            WriteLog("ERROR", message);
        }

        /// <summary>
        /// Log information message asynchronously
        /// </summary>
        public async Task LogInfoAsync(string message)
        {
            await WriteLogAsync("INFO", message);
        }

        /// <summary>
        /// Log information message asynchronously (alias for LogInfoAsync)
        /// </summary>
        public async Task LogInformationAsync(string message)
        {
            await LogInfoAsync(message);
        }

        /// <summary>
        /// Log warning message asynchronously
        /// </summary>
        public async Task LogWarningAsync(string message)
        {
            await WriteLogAsync("WARNING", message);
        }

        /// <summary>
        /// Log error message asynchronously
        /// </summary>
        public async Task LogErrorAsync(string message)
        {
            await WriteLogAsync("ERROR", message);
        }

        /// <summary>
        /// Log error message asynchronously with exception
        /// </summary>
        public async Task LogErrorAsync(string message, Exception exception)
        {
            await WriteLogAsync("ERROR", $"{message}. Exception: {exception.Message}");
        }

        /// <summary>
        /// Log information message (synchronous version for compatibility)
        /// </summary>
        public void LogInformation(string message)
        {
            WriteLog("INFO", message);
        }

        /// <summary>
        /// Log warning message with additional context
        /// </summary>
        public void LogWarning(string message, string context, string additionalInfo = null)
        {
            string fullMessage = $"{message} | Context: {context}";
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                fullMessage += $" | {additionalInfo}";
            }
            WriteLog("WARNING", fullMessage);
        }

        // Methods for retrieving logs
        public List<LogEntry> GetSystemLogs(DateTime? startDate = null, DateTime? endDate = null)
        {
            // Implementation to retrieve system logs from the log file
            var logs = new List<LogEntry>();
            
            try
            {
                if (File.Exists(_logFilePath))
                {
                    var logLines = File.ReadAllLines(_logFilePath);
                    
                    foreach (var line in logLines)
                    {
                        try
                        {
                            // Parse log line (format: "2025-05-06 14:25:30 [INFO] Log message")
                            var timestampString = line.Substring(0, 19);
                            var levelStart = line.IndexOf('[') + 1;
                            var levelEnd = line.IndexOf(']', levelStart);
                            var level = line.Substring(levelStart, levelEnd - levelStart);
                            var message = line.Substring(levelEnd + 2);
                            
                            DateTime timestamp;
                            if (DateTime.TryParse(timestampString, out timestamp))
                            {
                                if ((!startDate.HasValue || timestamp >= startDate.Value) && 
                                    (!endDate.HasValue || timestamp <= endDate.Value))
                                {
                                    logs.Add(new LogEntry
                                    {
                                        Timestamp = timestamp,
                                        Level = level,
                                        Message = message,
                                        Source = "Application"
                                    });
                                }
                            }
                        }
                        catch
                        {
                            // Skip malformed log lines
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving system logs: {ex.Message}");
            }
            
            return logs.OrderByDescending(l => l.Timestamp).ToList();
        }

        public List<AuditLog> GetAuditLogs(DateTime? startDate = null, DateTime? endDate = null)
        {
            // Implementation to retrieve audit logs from the database
            // This would typically query from a database using DbContext
            // For now, returning an empty list as a placeholder
            return new List<AuditLog>();
        }

        /// <summary>
        /// Write log message to file
        /// </summary>
        private void WriteLog(string level, string message)
        {
            try
            {
                lock (_lockObj)
                {
                    string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                    
                    // Also write to console for debugging
                    Console.WriteLine(logEntry);
                }
            }
            catch (Exception ex)
            {
                // If logging fails, write to console
                Console.WriteLine($"Error writing to log file: {ex.Message}");
                Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}");
            }
        }

        /// <summary>
        /// Write log message to file asynchronously
        /// </summary>
        private async Task WriteLogAsync(string level, string message)
        {
            try
            {
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
                await File.AppendAllTextAsync(_logFilePath, logEntry + Environment.NewLine);
                
                // Also write to console for debugging
                Console.WriteLine(logEntry);
            }
            catch (Exception ex)
            {
                // If logging fails, write to console
                Console.WriteLine($"Error writing to log file: {ex.Message}");
                Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}");
            }
        }
    }
}