using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class PurchaseOrder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PurchaseOrderID { get; set; }

        [Required]
        [StringLength(50)]
        public string PurchaseOrderNumber { get; set; } // Auto-generated: PO-YYYYMMDD-XXXX

        [Required]
        [ForeignKey("Supplier")]
        public int SupplierID { get; set; }

        [Required]
        public DateTime PurchaseDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Completed, Cancelled

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceAmount { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentStatus { get; set; } = "Pending"; // Paid, Pending, Partial

        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Cash";

        [StringLength(200)]
        public string DeliveryAddress { get; set; }

        public DateTime? ExpectedDeliveryDate { get; set; }

        public DateTime? ActualDeliveryDate { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        // Flag to indicate if an expense entry has been created for this purchase
        public bool HasExpenseEntry { get; set; } = false;

        // Navigation properties
        public virtual Supplier Supplier { get; set; }
        public virtual ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; }
        public virtual ICollection<Stock> Stocks { get; set; }
    }
}
