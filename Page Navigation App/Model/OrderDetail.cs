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
        public decimal MetalRate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxableAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CGSTAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SGSTAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal IGSTAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal FinalAmount { get; set; }

        // Navigation Properties
        public Order Order { get; set; }
        public Product Product { get; set; }
    }
}