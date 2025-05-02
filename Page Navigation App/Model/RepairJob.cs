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

        [Column(TypeName = "decimal(10,3)")]
        [Range(0, 9999.999)]
        public decimal AdditionalMetalWeight { get; set; }  // Weight of additional metal used

        [Column(TypeName = "decimal(10,3)")]
        [Range(0, 9999.999)]
        public decimal StoneWeight { get; set; }  // Weight of stones if any

        [StringLength(500)]
        public string StoneDetails { get; set; }  // Details of stones used/replaced

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MetalRate { get; set; }  // Current metal rate when repair started

        [Column(TypeName = "decimal(18,2)")]
        public decimal MakingCharges { get; set; }  // Making charges for repair work

        [StringLength(50)]
        public string Size { get; set; }  // New size if resizing

        [StringLength(100)]
        public string OldHallmark { get; set; }  // Old hallmark number if any

        [StringLength(100)]
        public string NewHallmark { get; set; }  // New hallmark number if required

        [StringLength(50)]
        public string Purity { get; set; }  // Metal purity

        [StringLength(500)]
        public string QualityChecks { get; set; }  // Quality check notes

        public bool IsHallmarkRequired { get; set; }  // Whether new hallmark is needed

        [StringLength(500)]
        public string WorkmanRemarks { get; set; }  // Remarks from the workman

        [StringLength(50)]
        public string AssignedTo { get; set; }  // Workman assigned to the job

        // Navigation property
        public virtual Customer Customer { get; set; }
    }
}