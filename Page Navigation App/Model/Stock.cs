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
        public string Location { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,3)")]
        public decimal Quantity { get; set; }

        [Required]
        public DateTime LastUpdated { get; set; }

        public DateTime? LastSold { get; set; }

        [Required]
        public DateTime AddedDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchasePrice { get; set; }

        public bool IsDeadStock { get; set; }

        public int DaysInStock 
        {
            get 
            {
                return (DateTime.Now - AddedDate).Days;
            }
        }

        public string StockStatus 
        { 
            get 
            {
                if (DaysInStock > 180 && LastSold == null)
                    return "Dead Stock";
                else if (Quantity <= 0)
                    return "Out of Stock";
                else
                    return "Available";
            }
        }

        // Navigation Properties
        public Product Product { get; set; }
    }
}