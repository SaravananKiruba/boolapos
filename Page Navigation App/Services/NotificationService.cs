using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Page_Navigation_App.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly NotificationSettings _settings;
        private readonly HttpClient _httpClient;

        public NotificationService(AppDbContext context)
        {
            _context = context;
            _httpClient = new HttpClient();
            
            // Load notification settings from database
            _settings = _context.NotificationSettings.FirstOrDefaultAsync().Result 
                ?? new NotificationSettings 
                {
                    EnableSMS = false,
                    EnableWhatsApp = false,
                    EnableEmail = false
                };
        }

        public async Task<bool> SendSMS(string phoneNumber, string message)
        {
            if (!_settings.EnableSMS) return false;

            try
            {
                // Log notification attempt
                var notification = new NotificationLog
                {
                    NotificationType = "SMS",
                    NotificationContent = message,
                    RecipientContact = phoneNumber,
                    SentDate = DateTime.Now,
                    Status = "Processing"
                };
                
                await _context.NotificationLogs.AddAsync(notification);
                await _context.SaveChangesAsync();
                
                // In a real implementation, this would call an SMS gateway API
                // For this implementation, we'll simulate success
                bool success = true;
                
                // Update notification status
                notification.Status = success ? "Sent" : "Failed";
                notification.ResponseDetails = success ? "Message delivered" : "Failed to deliver message";
                await _context.SaveChangesAsync();
                
                return success;
            }
            catch (Exception ex)
            {
                // Log the error
                await _context.LogEntries.AddAsync(new LogEntry
                {
                    LogDate = DateTime.Now,
                    LogLevel = "Error",
                    Source = "NotificationService.SendSMS",
                    Message = ex.Message,
                    StackTrace = ex.StackTrace
                });
                await _context.SaveChangesAsync();
                
                return false;
            }
        }

        public async Task<bool> SendWhatsApp(string phoneNumber, string message)
        {
            if (!_settings.EnableWhatsApp) return false;
            
            try
            {
                // Log notification attempt
                var notification = new NotificationLog
                {
                    NotificationType = "WhatsApp",
                    NotificationContent = message,
                    RecipientContact = phoneNumber,
                    SentDate = DateTime.Now,
                    Status = "Processing"
                };
                
                await _context.NotificationLogs.AddAsync(notification);
                await _context.SaveChangesAsync();
                
                // In a real implementation, this would call WhatsApp Business API
                // For this implementation, we'll simulate success
                bool success = true;
                
                // Update notification status
                notification.Status = success ? "Sent" : "Failed";
                notification.ResponseDetails = success ? "Message delivered" : "Failed to deliver message";
                await _context.SaveChangesAsync();
                
                return success;
            }
            catch (Exception ex)
            {
                // Log the error
                await _context.LogEntries.AddAsync(new LogEntry
                {
                    LogDate = DateTime.Now,
                    LogLevel = "Error",
                    Source = "NotificationService.SendWhatsApp",
                    Message = ex.Message,
                    StackTrace = ex.StackTrace
                });
                await _context.SaveChangesAsync();
                
                return false;
            }
        }

        public async Task<bool> SendEmail(string emailAddress, string subject, string message)
        {
            if (!_settings.EnableEmail) return false;
            
            try
            {
                // Log notification attempt
                var notification = new NotificationLog
                {
                    NotificationType = "Email",
                    NotificationContent = $"Subject: {subject}\n\n{message}",
                    RecipientContact = emailAddress,
                    SentDate = DateTime.Now,
                    Status = "Processing"
                };
                
                await _context.NotificationLogs.AddAsync(notification);
                await _context.SaveChangesAsync();
                
                // In a real implementation, this would use SMTP client
                // For this implementation, we'll simulate success
                bool success = true;
                
                // Update notification status
                notification.Status = success ? "Sent" : "Failed";
                notification.ResponseDetails = success ? "Email delivered" : "Failed to deliver email";
                await _context.SaveChangesAsync();
                
                return success;
            }
            catch (Exception ex)
            {
                // Log the error
                await _context.LogEntries.AddAsync(new LogEntry
                {
                    LogDate = DateTime.Now,
                    LogLevel = "Error",
                    Source = "NotificationService.SendEmail",
                    Message = ex.Message,
                    StackTrace = ex.StackTrace
                });
                await _context.SaveChangesAsync();
                
                return false;
            }
        }

        public async Task<bool> SendRepairNotification(int customerId, int repairJobId, string message)
        {
            try
            {
                // Get customer contact information
                var customer = await _context.Customers.FindAsync(customerId);
                if (customer == null) return false;
                
                // Create notification log entry
                var notification = new NotificationLog
                {
                    CustomerID = customerId,
                    ReferenceID = repairJobId.ToString(),
                    ReferenceType = "RepairJob",
                    NotificationContent = message,
                    RecipientContact = customer.Mobile,
                    SentDate = DateTime.Now,
                    Status = "Processing"
                };
                
                await _context.NotificationLogs.AddAsync(notification);
                await _context.SaveChangesAsync();
                
                bool success = false;
                
                // Try to send WhatsApp first if enabled
                if (_settings.EnableWhatsApp && !string.IsNullOrEmpty(customer.Mobile))
                {
                    notification.NotificationType = "WhatsApp";
                    success = await SendWhatsApp(customer.Mobile, message);
                }
                
                // If WhatsApp failed or not enabled, try SMS
                if (!success && _settings.EnableSMS && !string.IsNullOrEmpty(customer.Mobile))
                {
                    notification.NotificationType = "SMS";
                    success = await SendSMS(customer.Mobile, message);
                }
                
                // If both WhatsApp and SMS failed or not enabled, try email
                if (!success && _settings.EnableEmail && !string.IsNullOrEmpty(customer.Email))
                {
                    notification.NotificationType = "Email";
                    success = await SendEmail(
                        customer.Email, 
                        "Update on your repair job", 
                        message);
                }
                
                // Update notification status
                notification.Status = success ? "Sent" : "Failed";
                await _context.SaveChangesAsync();
                
                return success;
            }
            catch (Exception ex)
            {
                // Log the error
                await _context.LogEntries.AddAsync(new LogEntry
                {
                    LogDate = DateTime.Now,
                    LogLevel = "Error",
                    Source = "NotificationService.SendRepairNotification",
                    Message = ex.Message,
                    StackTrace = ex.StackTrace
                });
                await _context.SaveChangesAsync();
                
                return false;
            }
        }
    }
}