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
        public string ProductName { get; set; } = "Traditional Gold Necklace with Ruby";

        [Required]
        [StringLength(50)]
        public string MetalType { get; set; } = "Gold";

        [Required]
        [StringLength(10)]
        public string Purity { get; set; } = "22k";

        [Required]
        [Column(TypeName = "decimal(10,3)")]
        [Range(0.001, 9999.999)]
        public decimal GrossWeight { get; set; } = 25.850m;

        [Required]
        [Column(TypeName = "decimal(10,3)")]
        [Range(0.001, 9999.999)]
        public decimal NetWeight { get; set; } = 23.450m;

        [Required]
        [StringLength(20)]
        public string Barcode { get; set; }  // Will be auto-generated

        [StringLength(500)]
        public string Description { get; set; } = "Traditional gold necklace with ruby stone work and antique finish";

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BasePrice { get; set; }  // Will be calculated based on current rate

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal FinalPrice { get; set; }  // Will be calculated including all charges

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal MakingCharges { get; set; } = 12.00m;

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal WastagePercentage { get; set; } = 3.50m;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal StoneValue { get; set; } = 15000.00m;

        [StringLength(500)]
        public string StoneDetails { get; set; } = "5 Ruby stones, total 2.4 carats";

        [Column(TypeName = "decimal(10,3)")]
        [Range(0, 9999.999)]
        public decimal StoneWeight { get; set; } = 2.400m;

        public string HallmarkNumber { get; set; }  // Will be assigned after hallmarking

        [StringLength(50)]
        public string Collection { get; set; } = "Traditional";

        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal ValueAdditionPercentage { get; set; } = 5.00m;

        public bool IsCustomOrder { get; set; } = false;

        [Required]
        public bool IsActive { get; set; } = true;

        [Column(TypeName = "int")]
        public int StockQuantity { get; set; } = 0;

        [Column(TypeName = "int")]
        public int ReorderLevel { get; set; } = 5;

        [StringLength(100)]
        public string Design { get; set; } = "Antique finish with temple work";

        [StringLength(50)]
        public string Size { get; set; } = "16 inches";

        [Required]
        [ForeignKey("Category")]
        public int CategoryID { get; set; } = 1;  // Refers to our "Necklaces" category

        [ForeignKey("Subcategory")]
        public int? SubcategoryID { get; set; }

        [Required]
        [ForeignKey("Supplier")]
        public int SupplierID { get; set; } = 1;  // Refers to "Ratanlal Jewellers"

        // Navigation properties
        public virtual Category Category { get; set; }
        public virtual Subcategory Subcategory { get; set; }
        public virtual Supplier Supplier { get; set; }
        public virtual ICollection<Stock> Stocks { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    }
}