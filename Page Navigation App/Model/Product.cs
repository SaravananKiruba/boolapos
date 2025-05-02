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
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal BasePrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal FinalPrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal WastagePercentage { get; set; }

        [Required]
        public string MetalType { get; set; }

        [Required]
        public string Purity { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,3)")]
        [Range(0.001, 9999.999)]
        public decimal GrossWeight { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,3)")]
        [Range(0.001, 9999.999)]
        public decimal NetWeight { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal MakingCharges { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal StoneValue { get; set; }

        [StringLength(50)]
        [RegularExpression(@"^[A-Za-z0-9\-]+$")]
        public string Barcode { get; set; }

        [Required]
        [StringLength(100)]
        public string Location { get; set; }

        public bool IsDeadStock { get; set; }

        public bool IsActive { get; set; } = true;  // Added IsActive property with default value

        [StringLength(100)]
        public string Design { get; set; }  // Design number/name

        [StringLength(100)]
        public string Size { get; set; }  // Ring size, bangle size, etc.

        [Column(TypeName = "decimal(10,3)")]
        [Range(0, 9999.999)]
        public decimal StoneWeight { get; set; }  // Weight of stones in carats

        [StringLength(500)]
        public string StoneDetails { get; set; }  // Details of stones used

        [StringLength(100)]
        public string HallmarkNumber { get; set; }  // Hallmark certification number

        [StringLength(50)]
        public string Collection { get; set; }  // Collection or series name

        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal ValueAdditionPercentage { get; set; }  // Additional charges percentage

        public bool IsCustomOrder { get; set; }  // Whether this is a custom order piece

        [Required]
        [ForeignKey("Category")]
        public int CategoryID { get; set; }

        [ForeignKey("Subcategory")]
        public int? SubcategoryID { get; set; }

        [Required]
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