using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Mail;
using System.Net;

namespace Page_Navigation_App.Services
{
    public class NotificationService
    {
        private readonly AppDbContext _context;
        private readonly ConfigurationService _configService;
        private readonly LogService _logService;

        public NotificationService(
            AppDbContext context,
            ConfigurationService configService,
            LogService logService)
        {
            _context = context;
            _configService = configService;
            _logService = logService;
        }

        public async Task<bool> SendSMS(string phoneNumber, string message)
        {
            try
            {
                // Implementation would integrate with an SMS gateway service
                // This is a placeholder for the actual SMS sending logic
                await LogNotification(
                    phoneNumber,
                    "SMS",
                    message,
                    true,
                    "Simulated SMS sent successfully");

                return true;
            }
            catch (Exception ex)
            {
                await LogNotification(
                    phoneNumber,
                    "SMS",
                    message,
                    false,
                    ex.Message);
                return false;
            }
        }

        public async Task<bool> SendEmail(
            string email,
            string subject,
            string body,
            bool isHtml = false)
        {
            try
            {
                var emailSettings = await _configService.GetTypedValue<EmailSettings>(
                    "EmailSettings",
                    new EmailSettings
                    {
                        SmtpServer = "smtp.gmail.com",
                        SmtpPort = 587,
                        Username = "your-email@gmail.com",
                        Password = "your-app-specific-password",
                        SenderName = "Jewelry Shop"
                    });

                using var smtp = new SmtpClient(emailSettings.SmtpServer, emailSettings.SmtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(
                        emailSettings.Username,
                        emailSettings.Password)
                };

                using var mail = new MailMessage
                {
                    From = new MailAddress(
                        emailSettings.Username,
                        emailSettings.SenderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };

                mail.To.Add(email);
                await smtp.SendMailAsync(mail);

                await LogNotification(
                    email,
                    "Email",
                    subject,
                    true,
                    "Email sent successfully");

                return true;
            }
            catch (Exception ex)
            {
                await LogNotification(
                    email,
                    "Email",
                    subject,
                    false,
                    ex.Message);
                return false;
            }
        }

        public async Task<bool> SendWhatsApp(string phoneNumber, string message)
        {
            try
            {
                // Implementation would integrate with WhatsApp Business API
                // This is a placeholder for the actual WhatsApp sending logic
                await LogNotification(
                    phoneNumber,
                    "WhatsApp",
                    message,
                    true,
                    "Simulated WhatsApp message sent successfully");

                return true;
            }
            catch (Exception ex)
            {
                await LogNotification(
                    phoneNumber,
                    "WhatsApp",
                    message,
                    false,
                    ex.Message);
                return false;
            }
        }

        public async Task SendBirthdayWishes(Customer customer)
        {
            var settings = await _configService.GetNotificationSettings();
            if (!settings.SendBirthdayWishes)
                return;

            var message = await GenerateBirthdayMessage(customer);

            if (settings.EnableSMS && !string.IsNullOrEmpty(customer.PhoneNumber))
            {
                await SendSMS(customer.PhoneNumber, message);
            }

            if (settings.EnableWhatsApp && !string.IsNullOrEmpty(customer.PhoneNumber))
            {
                await SendWhatsApp(customer.PhoneNumber, message);
            }

            if (settings.EnableEmailNotifications && !string.IsNullOrEmpty(customer.Email))
            {
                await SendEmail(
                    customer.Email,
                    "Happy Birthday from Jewelry Shop!",
                    message,
                    true);
            }
        }

        public async Task SendAnniversaryWishes(Customer customer)
        {
            var settings = await _configService.GetNotificationSettings();
            if (!settings.SendAnniversaryWishes)
                return;

            var message = await GenerateAnniversaryMessage(customer);

            if (settings.EnableSMS && !string.IsNullOrEmpty(customer.PhoneNumber))
            {
                await SendSMS(customer.PhoneNumber, message);
            }

            if (settings.EnableWhatsApp && !string.IsNullOrEmpty(customer.PhoneNumber))
            {
                await SendWhatsApp(customer.PhoneNumber, message);
            }

            if (settings.EnableEmailNotifications && !string.IsNullOrEmpty(customer.Email))
            {
                await SendEmail(
                    customer.Email,
                    "Happy Anniversary from Jewelry Shop!",
                    message,
                    true);
            }
        }

        public async Task SendOrderConfirmation(Order order)
        {
            var settings = await _configService.GetNotificationSettings();
            if (!settings.SendOrderConfirmations)
                return;

            var customer = await _context.Customers.FindAsync(order.CustomerID);
            if (customer == null)
                return;

            var message = await GenerateOrderConfirmation(order);

            if (settings.EnableSMS && !string.IsNullOrEmpty(customer.PhoneNumber))
            {
                await SendSMS(customer.PhoneNumber, message);
            }

            if (settings.EnableWhatsApp && !string.IsNullOrEmpty(customer.PhoneNumber))
            {
                await SendWhatsApp(customer.PhoneNumber, message);
            }

            if (settings.EnableEmailNotifications && !string.IsNullOrEmpty(customer.Email))
            {
                var htmlMessage = await GenerateOrderConfirmationHtml(order);
                await SendEmail(
                    customer.Email,
                    $"Order Confirmation #{order.OrderID}",
                    htmlMessage,
                    true);
            }
        }

        public async Task SendPaymentReminder(Customer customer, decimal amount)
        {
            var settings = await _configService.GetNotificationSettings();
            if (!settings.SendPaymentReminders)
                return;

            var message = await GeneratePaymentReminder(customer, amount);

            if (settings.EnableSMS && !string.IsNullOrEmpty(customer.PhoneNumber))
            {
                await SendSMS(customer.PhoneNumber, message);
            }

            if (settings.EnableWhatsApp && !string.IsNullOrEmpty(customer.PhoneNumber))
            {
                await SendWhatsApp(customer.PhoneNumber, message);
            }

            if (settings.EnableEmailNotifications && !string.IsNullOrEmpty(customer.Email))
            {
                await SendEmail(
                    customer.Email,
                    "Payment Reminder",
                    message,
                    true);
            }
        }

        public async Task SendRepairStatusUpdate(RepairJob repair)
        {
            var settings = await _configService.GetNotificationSettings();
            if (!settings.SendRepairUpdates)
                return;

            var customer = await _context.Customers.FindAsync(repair.CustomerId);
            if (customer == null)
                return;

            var message = await GenerateRepairStatusUpdate(repair);

            if (settings.EnableSMS && !string.IsNullOrEmpty(customer.PhoneNumber))
            {
                await SendSMS(customer.PhoneNumber, message);
            }

            if (settings.EnableWhatsApp && !string.IsNullOrEmpty(customer.PhoneNumber))
            {
                await SendWhatsApp(customer.PhoneNumber, message);
            }

            if (settings.EnableEmailNotifications && !string.IsNullOrEmpty(customer.Email))
            {
                await SendEmail(
                    customer.Email,
                    $"Repair Status Update - #",
                    message,
                    true);
            }
        }

        private async Task<string> GenerateBirthdayMessage(Customer customer)
        {
            var businessInfo = await _configService.GetBusinessInfo();
            return $"Dear {customer.CustomerName},\n\n" +
                   $"Happy Birthday! 🎉\n\n" +
                   $"As a valued customer of {businessInfo.Name}, " +
                   $"we wish you a wonderful day filled with joy and happiness.\n\n" +
                   $"Visit us today to receive a special birthday discount!\n\n" +
                   $"Best wishes,\n{businessInfo.Name}";
        }

        private async Task<string> GenerateAnniversaryMessage(Customer customer)
        {
            var businessInfo = await _configService.GetBusinessInfo();
            return $"Dear {customer.CustomerName},\n\n" +
                   $"Happy Anniversary! 💑\n\n" +
                   $"Thank you for choosing {businessInfo.Name} for your special moments. " +
                   $"May your love continue to shine bright like our finest diamonds.\n\n" +
                   $"Visit us to celebrate with special anniversary offers!\n\n" +
                   $"Best wishes,\n{businessInfo.Name}";
        }

        private async Task<string> GenerateOrderConfirmation(Order order)
        {
            var businessInfo = await _configService.GetBusinessInfo();
            return $"Order #{order.OrderID} Confirmation\n\n" +
                   $"Thank you for your purchase at {businessInfo.Name}!\n" +
                   $"Order Total: ₹{order.GrandTotal:N2}\n" +
                   $"Date: {order.OrderDate:d}\n\n" +
                   $"Your order will be ready for collection as per the discussed timeline.\n\n" +
                   $"For any queries, contact us at {businessInfo.Phone}";
        }

        private async Task<string> GenerateOrderConfirmationHtml(Order order)
        {
            var businessInfo = await _configService.GetBusinessInfo();
            return $@"
                <html>
                <body>
                    <h2>Order Confirmation</h2>
                    <p>Thank you for your purchase at {businessInfo.Name}!</p>
                    <div style='margin: 20px 0; padding: 20px; background-color: #f8f9fa;'>
                        <p><strong>Order Number:</strong> {order.OrderID}</p>
                        <p><strong>Date:</strong> {order.OrderDate:d}</p>
                        <p><strong>Total Amount:</strong> ₹{order.GrandTotal:N2}</p>
                    </div>
                    <p>Your order will be ready for collection as per the discussed timeline.</p>
                    <p>For any queries, please contact us:</p>
                    <ul>
                        <li>Phone: {businessInfo.Phone}</li>
                        <li>Email: {businessInfo.Email}</li>
                    </ul>
                    <hr>
                    <p style='font-size: small; color: #666;'>
                        {businessInfo.Name}<br>
                        {businessInfo.Address}
                    </p>
                </body>
                </html>";
        }

        private async Task<string> GeneratePaymentReminder(Customer customer, decimal amount)
        {
            var businessInfo = await _configService.GetBusinessInfo();
            return $"Dear {customer.CustomerName},\n\n" +
                   $"This is a friendly reminder that you have a pending payment " +
                   $"of ₹{amount:N2} at {businessInfo.Name}.\n\n" +
                   $"Please clear your dues at your earliest convenience.\n\n" +
                   $"For any queries, contact us at {businessInfo.Phone}\n\n" +
                   $"Thank you for your business,\n{businessInfo.Name}";
        }

        private async Task<string> GenerateRepairStatusUpdate(RepairJob repair)
        {
            var businessInfo = await _configService.GetBusinessInfo();
            return $"Repair Job #{repair.Id} Status Update\n\n" +
                   $"Current Status: {repair.Status}\n" +
                   $"Expected Completion: {repair.CompletionDate:d}\n\n" +
                   $"For any queries, contact us at {businessInfo.Phone}";
        }

        private async Task LogNotification(
            string recipient,
            string channel,
            string content,
            bool success,
            string details)
        {
            var notification = new NotificationLog
            {
                Timestamp = DateTime.Now,
                Recipient = recipient,
                Channel = channel,
                Content = content,
                IsSuccessful = success,
                Details = details
            };

            await _context.NotificationLog.AddAsync(notification);
            await _context.SaveChangesAsync();

            if (!success)
            {
                await _logService.LogInfo(
                    $"Notification failed: {channel} to {recipient} - {details}",
                    "NotificationService");
            }
        }
    }

    public class NotificationLog
    {
        public int ID { get; set; }
        public DateTime Timestamp { get; set; }
        public string Recipient { get; set; }
        public string Channel { get; set; }
        public string Content { get; set; }
        public bool IsSuccessful { get; set; }
        public string Details { get; set; }
    }

    public class EmailSettings
    {
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string SenderName { get; set; }
    }
}