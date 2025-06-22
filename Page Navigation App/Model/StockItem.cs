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
        [StringLength(50)]
        public string StockItemCode { get; set; } // Format: ProductID-KAMJEWL-XXXX (4-digit number)

        [Required]
        [ForeignKey("Product")]
        public int ProductID { get; set; }

        [Required]
        [ForeignKey("Stock")]
        public int StockID { get; set; } // Reference to the purchase entry

        public DateTime AddedDate { get; set; } = DateTime.Now;

        [StringLength(20)]
        public string Status { get; set; } = "Available"; // Available, Sold, Reserved, Damaged

        public DateTime? SoldDate { get; set; }

        [ForeignKey("Order")]
        public int? OrderID { get; set; } // Only filled when item is sold

        [StringLength(500)]
        public string Notes { get; set; }

        // Navigation properties
        public virtual Product Product { get; set; }
        public virtual Stock Stock { get; set; }
        public virtual Order Order { get; set; }

        // Generate a new stock item code
        public static string GenerateStockItemCode(int productId, int currentCount)
        {
            // Format: ProductID-KAMJEWL-XXXX (4-digit number)
            string sequenceNumber = currentCount.ToString().PadLeft(4, '0');
            return $"{productId}-KAMJEWL-{sequenceNumber}";
        }
    }
}
