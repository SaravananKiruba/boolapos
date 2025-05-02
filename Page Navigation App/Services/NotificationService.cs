using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Windows;

namespace Page_Navigation_App.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly ConfigurationService _configService;
        private readonly LogService _logService;

        public NotificationService(AppDbContext context, ConfigurationService configService, LogService logService)
        {
            _context = context;
            _configService = configService;
            _logService = logService;
        }

        public async Task SendEmail(string recipient, string subject, string body)
        {
            try
            {
                var emailSettings = await _configService.GetEmailSettings();
                using (var client = new SmtpClient(emailSettings.SmtpServer, emailSettings.SmtpPort))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(emailSettings.Username, emailSettings.Password);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(emailSettings.Username, emailSettings.SenderName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(recipient);

                    await client.SendMailAsync(mailMessage);

                    // Log successful notification
                    await LogNotification(recipient, "Email", body, true);
                }
            }
            catch (Exception ex)
            {
                // Log failed notification
                await LogNotification(recipient, "Email", ex.Message, false);
                throw;
            }
        }

        public async Task SendSMS(string phoneNumber, string message)
        {
            // SMS implementation will go here
            // For now, just log it
            await LogNotification(phoneNumber, "SMS", message, true);
        }

        public async Task SendWhatsApp(string phoneNumber, string message)
        {
            // WhatsApp implementation will go here
            // For now, just log it
            await LogNotification(phoneNumber, "WhatsApp", message, true);
        }

        public async Task SendBirthdayWishes(Customer customer)
        {
            var subject = "Happy Birthday!";
            var body = $"Dear {customer.CustomerName},\n\nHappy Birthday! We wish you a wonderful day filled with joy and happiness. As a valued customer, we appreciate your continued support.\n\nBest wishes,\nYour Jewelry Store Team";
            
            if (!string.IsNullOrEmpty(customer.Email))
            {
                await SendEmail(customer.Email, subject, body);
            }
            
            if (!string.IsNullOrEmpty(customer.PhoneNumber))
            {
                await SendSMS(customer.PhoneNumber, $"Happy Birthday, {customer.CustomerName}! We wish you a wonderful day filled with joy. - Your Jewelry Store Team");
            }
        }

        public async Task SendAnniversaryWishes(Customer customer)
        {
            var subject = "Happy Wedding Anniversary!";
            var body = $"Dear {customer.CustomerName},\n\nWishing you a very happy wedding anniversary! May your love continue to grow stronger with each passing year. Thank you for being our valued customer.\n\nBest wishes,\nYour Jewelry Store Team";
            
            if (!string.IsNullOrEmpty(customer.Email))
            {
                await SendEmail(customer.Email, subject, body);
            }
            
            if (!string.IsNullOrEmpty(customer.PhoneNumber))
            {
                await SendSMS(customer.PhoneNumber, $"Happy Wedding Anniversary, {customer.CustomerName}! Wishing you both continued love and happiness. - Your Jewelry Store Team");
            }
        }

        public async Task SendNotification(string title, string message)
        {
            // Log the notification
            await _logService.LogInfo($"[{title}] {message}");

            // Show message box on UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        private async Task LogNotification(string recipient, string channel, string content, bool isSuccessful)
        {
            var log = new NotificationLog
            {
                Timestamp = DateTime.Now,
                Recipient = recipient,
                Channel = channel,
                Content = content,
                IsSuccessful = isSuccessful,
                Details = isSuccessful ? "Notification sent successfully" : "Failed to send notification"
            };

            await _context.NotificationLog.AddAsync(log);
            await _context.SaveChangesAsync();
        }
    }
}