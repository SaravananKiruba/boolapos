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

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        [StringLength(100)]
        public string Recipient { get; set; }

        [Required]
        [StringLength(20)]
        public string Channel { get; set; }  // SMS/Email/WhatsApp

        [Required]
        public string Content { get; set; }

        [Required]
        public bool IsSuccessful { get; set; }

        [StringLength(500)]
        public string Details { get; set; }

        [StringLength(100)]
        public string SentBy { get; set; }  // System/UserID

        [StringLength(100)]
        public string ErrorMessage { get; set; }

        public DateTime? DeliveredAt { get; set; }
    }
}