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
        public decimal Price { get; set; }

        [ForeignKey("Category")]
        public int CategoryID { get; set; }

        [ForeignKey("Subcategory")]
        public int? SubcategoryID { get; set; } // Optional

        [ForeignKey("Supplier")]
        public int SupplierID { get; set; }

        // Navigation properties
        public Category Category { get; set; }
        public Subcategory Subcategory { get; set; }
        public ICollection<Stock> Stocks { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; }
    }
}