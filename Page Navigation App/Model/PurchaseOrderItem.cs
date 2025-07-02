using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class PurchaseOrderItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PurchaseOrderItemID { get; set; }

        [Required]
        [ForeignKey("PurchaseOrder")]
        public int PurchaseOrderID { get; set; }

        [Required]
        [ForeignKey("Product")]
        public int ProductID { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,3)")]
        [Range(0.001, 9999.999)]
        public decimal Quantity { get; set; } = 1;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal UnitCost { get; set; } // Purchase cost per unit

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal TotalAmount { get; set; } // UnitCost * Quantity (Simplified calculation)

        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercentage { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        // Item Receipt Tracking
        [Column(TypeName = "decimal(10,3)")]
        public decimal ReceivedQuantity { get; set; } = 0;

        public DateTime? ReceivedDate { get; set; }

        [StringLength(100)]
        public string ReceivedBy { get; set; }

        // Stock Integration - Track if items added to stock
        public bool IsAddedToStock { get; set; } = false;
        public DateTime? StockAddedDate { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Delivered, Cancelled

        // Navigation Properties
        public virtual PurchaseOrder PurchaseOrder { get; set; }
        public virtual Product Product { get; set; }

        // Calculate remaining quantity
        [NotMapped]
        public decimal RemainingQuantity => Quantity - ReceivedQuantity;

        // Check if fully received
        [NotMapped]
        public bool IsFullyReceived => ReceivedQuantity >= Quantity;

        // Calculate net amount after discount
        [NotMapped]
        public decimal NetAmount => TotalAmount - DiscountAmount;
    }
}
