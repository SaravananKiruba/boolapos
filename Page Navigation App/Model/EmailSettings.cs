using System.ComponentModel.DataAnnotations;

namespace Page_Navigation_App.Model
{
    public class EmailSettings
    {
        [Key]
        public int ID { get; set; }
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string SenderName { get; set; }
        
        // Properties referenced in ConfigurationService
        public int Port { get => SmtpPort; set => SmtpPort = value; }
        public bool UseSSL { get; set; }
        public string FromEmail { get; set; }
        public string FromName { get => SenderName; set => SenderName = value; }
    }
}