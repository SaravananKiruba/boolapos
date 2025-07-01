using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Page_Navigation_App.Model
{
    public class PurchaseOrder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PurchaseOrderID { get; set; }

        [Required]
        [StringLength(50)]
        public string PurchaseOrderNumber { get; set; }

        [Required]
        [ForeignKey("Supplier")]
        public int SupplierID { get; set; }

        [Required]
        public DateOnly OrderDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        public DateOnly? ExpectedDeliveryDate { get; set; }

        public DateOnly? ActualDeliveryDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Delivered, Cancelled

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Cash";

        [StringLength(50)]
        public string PaymentStatus { get; set; } = "Pending"; // Pending, Partial, Paid

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrandTotal { get; set; }

        public int TotalItems { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        [StringLength(100)]
        public string ReferenceNumber { get; set; }

        [Required]
        [StringLength(100)]
        public string CreatedBy { get; set; } = "System";

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? LastModified { get; set; }

        // Navigation Properties
        public virtual Supplier Supplier { get; set; }
        public virtual ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
        public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();

        // Calculate remaining balance
        [NotMapped]
        public decimal RemainingBalance => GrandTotal - PaidAmount;

        // Check if fully paid
        [NotMapped]
        public bool IsFullyPaid => PaidAmount >= GrandTotal;
    }
}
