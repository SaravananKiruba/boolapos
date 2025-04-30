using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class RepairJob
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RepairJobID { get; set; }

        [Required]
        [ForeignKey("Customer")]
        public int CustomerId { get; set; }

        [Required]
        [StringLength(20)]
        public string JobNumber { get; set; }

        [Required]
        [StringLength(500)]
        public string ItemDetails { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        [StringLength(20)]
        public string MetalType { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,3)")]
        [Range(0.001, 9999.999)]
        public decimal Weight { get; set; }

        [Required]
        [StringLength(50)]
        public string WorkType { get; set; }

        [Required]
        public DateTime ReceiptDate { get; set; }

        public DateTime? PromisedDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal EstimatedAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal FinalAmount { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; }  // Pending/In Process/Completed/Delivered

        [StringLength(500)]
        public string ImagePath { get; set; }

        public bool SMSNotificationSent { get; set; }
        public bool WhatsAppNotificationSent { get; set; }
        public DateTime? CompletionDate { get; set; }

        // Navigation property
        public virtual Customer Customer { get; set; }
    }
}