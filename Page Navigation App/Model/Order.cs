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
        public string OrderType { get; set; } // Retail/Wholesale

        [Required]
        public string PaymentType { get; set; } // Cash/Credit/EMI

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrandTotal { get; set; }

        // Metal Exchange Details
        public bool HasMetalExchange { get; set; }

        [Column(TypeName = "decimal(10,3)")]
        public decimal? ExchangeMetalWeight { get; set; }

        public string ExchangeMetalType { get; set; }
        
        public string ExchangeMetalPurity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ExchangeValue { get; set; }

        // EMI Details
        public int? EMIMonths { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? EMIAmount { get; set; }

        // GST Details
        [StringLength(20)]
        public string GSTNumber { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CGSTAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SGSTAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal IGSTAmount { get; set; }

        public string InvoiceNumber { get; set; }

        // Navigation Properties
        public Customer Customer { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; }
        public ICollection<Finance> Payments { get; set; }
    }
}