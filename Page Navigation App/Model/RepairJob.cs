using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class RepairJob
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RepairID { get; set; }
        
        // Add alias property for compatibility with existing code
        [NotMapped]
        public int RepairJobID { get => RepairID; set => RepairID = value; }

        [Required]
        [ForeignKey("Customer")]
        public int CustomerId { get; set; }

        [Required]
        [StringLength(100)]
        public string ItemDescription { get; set; }
        
        // Add alias property for compatibility
        [NotMapped]
        public string ItemDetails { get => ItemDescription; set => ItemDescription = value; }

        [Required]
        public DateTime ReceiptDate { get; set; } = DateTime.Now;

        public DateTime? CompletionDate { get; set; }

        public DateTime? DeliveryDate { get; set; }
        
        // Add expected delivery date alias
        [NotMapped]
        public DateTime? PromisedDate { get => ExpectedDeliveryDate; set => ExpectedDeliveryDate = value; }

        [Required]
        [Column(TypeName = "decimal(10,3)")]
        [Range(0.001, 9999.999)]
        public decimal ItemWeight { get; set; }
        
        // Add alias for Weight
        [NotMapped]
        public decimal Weight { get => ItemWeight; set => ItemWeight = value; }

        [StringLength(50)]
        public string MetalType { get; set; }

        [StringLength(10)]
        public string Purity { get; set; }
        
        [Required]
        [StringLength(50)]
        public string WorkType { get; set; } = "Repair"; // Repair, Resize, Polish, Stone Setting, etc.

        [Required]
        [StringLength(500)]
        public string WorkDescription { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal EstimatedCost { get; set; }
        
        // Add alias for EstimatedAmount
        [NotMapped]
        public decimal EstimatedAmount { get => EstimatedCost; set => EstimatedCost = value; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal FinalAmount { get; set; }

        [Column(TypeName = "decimal(10,3)")]
        public decimal? AdditionalMetalWeight { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? AdditionalMetalCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? StoneCharge { get; set; }

        [StringLength(500)]
        public string StoneDetails { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Received";  // Received, In Progress, Ready, Delivered

        [Required]
        [StringLength(50)]
        public string Priority { get; set; } = "Normal";  // Normal, High, Urgent

        public DateTime? ExpectedDeliveryDate { get; set; }

        [StringLength(50)]
        public string AssignedTo { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? AdvanceAmount { get; set; }

        public bool IsStoneProvided { get; set; }

        [StringLength(500)]
        public string CustomerComments { get; set; }
        
        [StringLength(255)]
        public string ImagePath { get; set; }

        // Navigation Property
        public virtual Customer Customer { get; set; }
    }
}