using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class NotificationLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public DateTime Timestamp { get; set; }
        public string Recipient { get; set; }
        public string Channel { get; set; }
        public string Content { get; set; }
        public bool IsSuccessful { get; set; }
        public string Details { get; set; }
    }
}