using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderID { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        [Required]
        [ForeignKey("Customer")]
        public int CustomerID { get; set; }

        [Required]
        [StringLength(20)]
        public string OrderType { get; set; } // Retail/Wholesale

        [Required]
        [StringLength(20)]
        public string PaymentType { get; set; } // Cash/Credit/EMI

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal SubTotal { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal DiscountAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal TaxAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal GrandTotal { get; set; }

        [Required]
        [StringLength(20)]
        public string OrderStatus { get; set; } // Pending/Processing/Completed/Cancelled

        [StringLength(500)]
        public string Notes { get; set; }

        public DateTime? DeliveryDate { get; set; }

        [StringLength(50)]
        public string InvoiceNumber { get; set; }

        // Navigation properties
        public Customer Customer { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; }
        public ICollection<Finance> Payments { get; set; }
    }
}