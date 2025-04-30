using System;
using System.ComponentModel.DataAnnotations;

namespace Page_Navigation_App.Model
{
    public class NotificationSettings
    {
        [Key]
        public int ID { get; set; }
        public bool EnableSMS { get; set; }
        public bool EnableWhatsApp { get; set; }
        public bool EnableEmailNotifications { get; set; }
        public bool SendBirthdayWishes { get; set; }
        public bool SendAnniversaryWishes { get; set; }
        public bool SendOrderConfirmations { get; set; }
        public bool SendPaymentReminders { get; set; }
        public bool SendRepairUpdates { get; set; }
    }
}