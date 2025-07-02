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

        // Simplified: No GST for Purchase Orders as per requirement
        // TotalAmount = Sum of (Quantity * UnitPrice) for all items
        // GrandTotal = TotalAmount - DiscountAmount
        [Column(TypeName = "decimal(18,2)")]
        public decimal GrandTotal { get; set; }

        // Item Receipt Status
        [Required]
        [StringLength(50)]
        public string ItemReceiptStatus { get; set; } = "Pending"; // Pending, Partial, Completed

        // Payment Status Enhancement
        public bool IsItemsReceived { get; set; } = false;
        public DateTime? ItemsReceivedDate { get; set; }

        // Finance Integration for Payment Tracking
        public bool HasFinanceRecord { get; set; } = false;
        public string FinanceRecordID { get; set; }

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

        // Check if all items are received
        [NotMapped]
        public bool AreAllItemsReceived => PurchaseOrderItems?.All(x => x.IsFullyReceived) ?? false;

        // Calculate total received quantity
        [NotMapped]
        public decimal TotalReceivedQuantity => PurchaseOrderItems?.Sum(x => x.ReceivedQuantity) ?? 0;

        // Calculate pending quantity
        [NotMapped]
        public decimal PendingQuantity => PurchaseOrderItems?.Sum(x => x.RemainingQuantity) ?? 0;
    }
}
