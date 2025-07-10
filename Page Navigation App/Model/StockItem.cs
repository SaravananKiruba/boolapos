using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class StockItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int StockItemID { get; set; }

        [Required]
        [ForeignKey("Product")]
        public int ProductID { get; set; }

        [Required]
        [StringLength(50)]
        public string UniqueTagID { get; set; } // Unique identifier for each individual item

        [Required]
        [StringLength(50)]
        public string Barcode { get; set; } // Individual barcode for this specific item

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal PurchaseCost { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal SellingPrice { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Available"; // Available, Reserved, Sold, Returned

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string Location { get; set; } = "Main Store";

        // Purchase details
        [ForeignKey("PurchaseOrder")]
        public int? PurchaseOrderID { get; set; }

        [ForeignKey("PurchaseOrderItem")]
        public int? PurchaseOrderItemID { get; set; } // Link to specific purchase order item

        public DateTime? PurchaseDate { get; set; }

        // Sale details
        [ForeignKey("Order")]
        public int? OrderID { get; set; }

        // CRITICAL FIX: Add link to specific order detail for precise tracking
        [ForeignKey("OrderDetail")]
        public int? OrderDetailID { get; set; }

        public DateTime? SaleDate { get; set; }

        [ForeignKey("Customer")]
        public int? CustomerID { get; set; }

        // HUID tracking for jewelry
        [StringLength(20)]
        public string HUID { get; set; }

        [StringLength(100)]
        public string HallmarkDetails { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        // Navigation Properties
        public virtual Product Product { get; set; }
        public virtual PurchaseOrder PurchaseOrder { get; set; }
        public virtual PurchaseOrderItem PurchaseOrderItem { get; set; }
        public virtual Order Order { get; set; }
        public virtual OrderDetail OrderDetail { get; set; } // CRITICAL FIX: Add navigation to order detail
        public virtual Customer Customer { get; set; }

        // Calculate profit
        [NotMapped]
        public decimal Profit => Status == "Sold" ? SellingPrice - PurchaseCost : 0;
    }
}
