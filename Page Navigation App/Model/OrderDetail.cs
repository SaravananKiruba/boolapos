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
        public decimal GrossWeight { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,3)")]
        [Range(0.001, 9999.999)]
        public decimal NetWeight { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal MetalRate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal BaseAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal MakingCharges { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal StoneValue { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal TaxableAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CGSTAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SGSTAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal IGSTAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal FinalAmount { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal WastagePercentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal WastageAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal HallmarkingCharge { get; set; }

        [Column(TypeName = "decimal(10,3)")]
        public decimal StoneWeight { get; set; }  // Weight of stones in carats

        [StringLength(500)]
        public string StoneDetails { get; set; }  // Details of stones used

        [StringLength(100)]
        public string HallmarkNumber { get; set; }  // Hallmark certification number

        [Column(TypeName = "decimal(5,2)")]
        public decimal ValueAdditionPercentage { get; set; }  // Additional charges percentage

        [Column(TypeName = "decimal(18,2)")]
        public decimal ValueAdditionAmount { get; set; }  // Additional charges amount

        [StringLength(50)]
        public string Size { get; set; }  // Size if applicable (ring/bangle)

        // Navigation properties
        public Order Order { get; set; }
        public Product Product { get; set; }
    }
}