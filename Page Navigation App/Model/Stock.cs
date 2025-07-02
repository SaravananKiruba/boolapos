using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class Stock
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int StockID { get; set; }

        [Required]
        [ForeignKey("Product")]
        public int ProductID { get; set; }

        [Required]
        [ForeignKey("Supplier")]
        public int SupplierID { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,3)")]
        [Range(0.001, 9999.999)]
        public decimal Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal UnitCost { get; set; } // Purchase cost per unit

        [Required]
        [StringLength(50)]
        public string Location { get; set; } = "Main Store";

        [Required]
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Available"; // Available, Reserved, Sold, Defective

        [StringLength(100)]
        public string Batch { get; set; } // Batch or lot number

        // Enhanced tracking for individual items
        public int AvailableCount { get; set; } = 0; // Count of available individual items
        public int ReservedCount { get; set; } = 0;  // Count of reserved individual items
        public int SoldCount { get; set; } = 0;      // Count of sold individual items

        [StringLength(500)]
        public string Notes { get; set; }

        // For PurchaseOrder reference
        [ForeignKey("PurchaseOrder")]
        public int? PurchaseOrderID { get; set; }

        // Track when items were added from Purchase Order
        public DateTime? PurchaseOrderReceivedDate { get; set; }

        // Navigation Properties
        public virtual Product Product { get; set; }
        public virtual Supplier Supplier { get; set; }
        public virtual PurchaseOrder PurchaseOrder { get; set; }
        public virtual ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();

        // Calculate total value
        [NotMapped]
        public decimal TotalValue => Quantity * UnitCost;

        // Calculate total available items
        [NotMapped]
        public int TotalAvailableItems => AvailableCount;

        // Calculate total items in stock
        [NotMapped]
        public int TotalItemsInStock => AvailableCount + ReservedCount;
    }
}
