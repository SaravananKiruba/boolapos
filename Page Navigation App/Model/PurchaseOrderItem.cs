using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class PurchaseOrderItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PurchaseOrderItemID { get; set; }

        [Required]
        [ForeignKey("PurchaseOrder")]
        public int PurchaseOrderID { get; set; }

        [Required]
        [ForeignKey("Product")]
        public int ProductID { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,3)")]
        [Range(0.001, 9999.999)]
        public decimal Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal TotalPrice { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        // Navigation properties
        public virtual PurchaseOrder PurchaseOrder { get; set; }
        public virtual Product Product { get; set; }
    }
}
