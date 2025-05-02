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
        [ForeignKey("Customer")]
        public int CustomerID { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        [Required]
        [StringLength(20)]
        public string InvoiceNumber { get; set; }
        
        [Required]
        [StringLength(50)]
        public string OrderType { get; set; } = "Retail"; // Retail, Wholesale, Custom

        [Required]
        [StringLength(20)]
        public string PaymentType { get; set; } = "Cash"; // Cash, Card, UPI, EMI, etc.

        public int? EMIMonths { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal EMIAmount { get; set; }

        [StringLength(20)]
        public string GSTNumber { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 9999999999.99)]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 9999999999.99)]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 9999999999.99)]
        public decimal TaxAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 9999999999.99)]
        public decimal GrandTotal { get; set; }
        
        // For compatibility with existing code
        [NotMapped]
        public decimal TotalAmount { get => GrandTotal; set => GrandTotal = value; }

        // Metal Exchange Properties
        public bool HasMetalExchange { get; set; }
        public string ExchangeMetalType { get; set; }
        public string ExchangeMetalPurity { get; set; }

        [Column(TypeName = "decimal(10,3)")]
        public decimal ExchangeMetalWeight { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ExchangeValue { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        public bool IsDelivered { get; set; }
        public DateTime? DeliveryDate { get; set; }

        [StringLength(100)]
        public string DeliveryAddress { get; set; }

        // Navigation Properties
        public virtual Customer Customer { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
        public virtual ICollection<Finance> Payments { get; set; }
    }
}