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

        [StringLength(20)]
        public string InvoiceNumber { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Completed, Cancelled, etc.
        
        [StringLength(50)]
        public string OrderType { get; set; } = "Retail"; // Retail, Wholesale, Custom, Exchange

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Credit Card, UPI, EMI, etc.

        // Adding PaymentType as alias for PaymentMethod for compatibility
        [NotMapped]
        public string PaymentType 
        { 
            get { return PaymentMethod; } 
            set { PaymentMethod = value; } 
        }

        public int? EMIMonths { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal EMIAmount { get; set; }

        [StringLength(20)]
        public string GSTNumber { get; set; }

        [Required]
        public int TotalItems { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 9999999999.99)]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 9999999999.99)]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 9999999999.99)]
        public decimal CGST { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 9999999999.99)]
        public decimal SGST { get; set; }
        
        // Add IGST property
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 9999999999.99)]
        public decimal IGST { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 9999999999.99)]
        public decimal GrandTotal { get; set; }
        
        // Add HallmarkingCharges property
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 9999999999.99)]
        public decimal HallmarkingCharges { get; set; }
        
        // Backward compatibility with old code
        [NotMapped]
        public decimal SubTotal { get => TotalAmount; set => TotalAmount = value; }
        
        [NotMapped]
        public decimal TaxAmount { get => CGST + SGST + IGST; set { CGST = value / 3; SGST = value / 3; IGST = value / 3; } }

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