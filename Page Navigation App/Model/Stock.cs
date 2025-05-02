using System;
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

        public DateTime PurchaseDate { get; set; } = DateTime.Now;
        
        [Required]
        [Column(TypeName = "decimal(10,3)")]
        [Range(0, 9999.999)]
        public decimal QuantityPurchased { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal PurchaseRate { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal TotalAmount { get; set; }
        
        [StringLength(50)]
        public string InvoiceNumber { get; set; }
        
        [StringLength(20)]
        public string PaymentStatus { get; set; } = "Pending"; // Paid, Pending, Partial
        
        [StringLength(100)]
        public string Location { get; set; } = "Main";

        [NotMapped]
        public decimal Quantity 
        { 
            get => QuantityPurchased;
            set => QuantityPurchased = value; 
        }

        [NotMapped]
        public decimal PurchasePrice
        {
            get => PurchaseRate;
            set => PurchaseRate = value;
        }

        public DateTime LastUpdated { get; set; } = DateTime.Now;

        public DateTime? LastSold { get; set; }

        public DateTime AddedDate { get; set; } = DateTime.Now;

        public bool IsDeadStock { get; set; }
        
        [StringLength(500)]
        public string Notes { get; set; }

        [NotMapped]
        public int DaysInStock
        {
            get => (DateTime.Now - AddedDate).Days;
        }

        [NotMapped]
        public string StockStatus
        {
            get
            {
                if (QuantityPurchased <= 0)
                    return "Out of Stock";
                if (QuantityPurchased <= 5)
                    return "Low Stock";
                if (DaysInStock > 180 && IsDeadStock)
                    return "Dead Stock";
                return "In Stock";
            }
        }

        // Navigation properties
        public virtual Product Product { get; set; }
        public virtual Supplier Supplier { get; set; }
    }
}