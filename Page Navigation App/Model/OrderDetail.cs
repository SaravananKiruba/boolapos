using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class OrderDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderDetailID { get; set; }

        [Required]
        [ForeignKey("Order")]
        public int OrderID { get; set; }

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
        public decimal UnitPrice { get; set; } // Product's price

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal TotalAmount { get; set; } // UnitPrice * Quantity

        [StringLength(500)]
        public string Notes { get; set; }

        // Keep these for backward compatibility
        [NotMapped]
        public decimal FinalAmount { get => TotalAmount; set => TotalAmount = value; }
        
        [NotMapped]
        public decimal TotalPrice { get => TotalAmount; set => TotalAmount = value; }
        
        // Backward compatibility fields
        [NotMapped]
        public decimal GrossWeight { get => Product?.GrossWeight ?? 0; set { } }
        
        [NotMapped]
        public decimal NetWeight { get => Product?.NetWeight ?? 0; set { } }
          [NotMapped]
        public decimal MetalRate { get => Product != null ? Product.BasePrice / Math.Max(Product.NetWeight, 1) : 0; set { } }
        
        [NotMapped]
        public decimal MakingCharges { get => Product?.MakingCharges ?? 0; set { } }
        
        [NotMapped]
        public decimal WastagePercentage { get => Product?.WastagePercentage ?? 0; set { } }
        
        [NotMapped]
        public decimal BaseAmount { get => UnitPrice * 0.85m; set { } }
        
        [NotMapped]
        public string HSNCode { get => "7113"; set { } }
        
        [NotMapped]
        public decimal TaxableAmount { get => TotalAmount; set { } }
        
        [NotMapped]
        public decimal CGSTAmount { get => 0; set { } }
        
        [NotMapped]
        public decimal SGSTAmount { get => 0; set { } }
        
        [NotMapped]
        public decimal IGSTAmount { get => 0; set { } }
        
        /// <summary>
        /// Calculates total amount for this order detail
        /// </summary>
        public void CalculateTotals()
        {
            if (Product != null)
            {
                UnitPrice = Product.ProductPrice;
                TotalAmount = UnitPrice * Quantity;
            }
        }

        // Navigation Properties
        public virtual Order Order { get; set; }
        public virtual Product Product { get; set; }
    }
}