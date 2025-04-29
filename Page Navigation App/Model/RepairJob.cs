using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class RepairJob
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string ItemDetails { get; set; }

        [Required]
        public string MetalType { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,3)")]
        public decimal Weight { get; set; }

        [Required]
        public string WorkType { get; set; }

        public string Description { get; set; }

        [Required]
        public DateTime ReceiptDate { get; set; }

        public DateTime? PromisedDate { get; set; }

        public DateTime? CompletionDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal EstimatedAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal FinalAmount { get; set; }

        [Required]
        public string Status { get; set; } // Pending, In Process, Delivered

        public string ImagePath { get; set; }

        [Required]
        [ForeignKey("Customer")]
        public int CustomerId { get; set; }

        // Navigation property
        public Customer Customer { get; set; }

        public bool SMSNotificationSent { get; set; }
        public bool WhatsAppNotificationSent { get; set; }
    }
}