using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductID { get; set; }

        [Required]
        [StringLength(100)]
        public string ProductName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public string MetalType { get; set; } // Gold, Silver, Platinum

        [Required]
        public string Purity { get; set; } // 18k, 22k, 24k

        [Required]
        [Column(TypeName = "decimal(10,3)")]
        public decimal GrossWeight { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,3)")]
        public decimal NetWeight { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal WastagePercentage { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MakingCharges { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal StoneValue { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BasePrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal FinalPrice { get; set; }

        public string Barcode { get; set; }

        [Required]
        public string Location { get; set; }

        public bool IsDeadStock { get; set; }

        [ForeignKey("Category")]
        public int CategoryID { get; set; }

        [ForeignKey("Subcategory")]
        public int? SubcategoryID { get; set; }

        [ForeignKey("Supplier")]
        public int SupplierID { get; set; }

        // Navigation properties
        public Category Category { get; set; }
        public Subcategory Subcategory { get; set; }
        public Supplier Supplier { get; set; }
        public ICollection<Stock> Stocks { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; }
    }
}