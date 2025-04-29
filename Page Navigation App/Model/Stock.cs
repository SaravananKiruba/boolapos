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
        [StringLength(100)]
        public string Location { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,3)")]
        [Range(0, 9999.999)]
        public decimal Quantity { get; set; }

        [Required]
        public DateTime LastUpdated { get; set; }

        public DateTime? LastSold { get; set; }

        [Required]
        public DateTime AddedDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal PurchasePrice { get; set; }

        public bool IsDeadStock { get; set; }

        [NotMapped]
        public int DaysInStock
        {
            get
            {
                return (DateTime.Now - AddedDate).Days;
            }
        }

        [NotMapped]
        public string StockStatus
        {
            get
            {
                if (Quantity <= 0)
                    return "Out of Stock";
                if (Quantity <= 5)
                    return "Low Stock";
                if (DaysInStock > 180 && IsDeadStock)
                    return "Dead Stock";
                return "In Stock";
            }
        }

        // Navigation property
        public Product Product { get; set; }
    }
}