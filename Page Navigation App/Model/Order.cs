using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

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
        public DateOnly OrderDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        [StringLength(20)]
        public string InvoiceNumber { get; set; }
        
        // Add HUID tracking at invoice level
        [StringLength(500)]
        public string HUIDReferences { get; set; }  // Comma-separated list of HUIDs in this invoice
        
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

        [Required]
        public int TotalItems { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 9999999999.99)]
        public decimal TotalAmount { get; set; } // Sum of all product prices

        private decimal _discountAmount;
        
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 9999999999.99)]
        public decimal DiscountAmount
        {
            get => _discountAmount;
            set
            {
                _discountAmount = value;
                // Recalculate totals when discount changes
                CalculateOrderTotals();
            }
        }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 9999999999.99)]
        public decimal PriceBeforeTax { get; set; } // TotalAmount + DiscountAmount

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 9999999999.99)]
        public decimal GrandTotal { get; set; } // Final price including 3% tax

        [StringLength(500)]
        public string Notes { get; set; }
        
        // Backward compatibility properties for services
        [NotMapped]
        public string TagReferences { get; set; } = string.Empty;
        
        [NotMapped]
        public decimal CGST { get => Math.Round(PriceBeforeTax * 0.015m, 2); set { } }
        
        [NotMapped]
        public decimal SGST { get => Math.Round(PriceBeforeTax * 0.015m, 2); set { } }
        
        [NotMapped]
        public decimal IGST { get => 0; set { } }

        [NotMapped]
        public string HSNCode { get => "7113"; set { } }
        
        [NotMapped]
        public string GSTNumber { get => Customer?.GSTNumber ?? string.Empty; set { } }

        [NotMapped]
        public bool TaxApplicable { get => true; set { } }

        [NotMapped]
        public bool IsGSTRegisteredCustomer { get => !string.IsNullOrEmpty(Customer?.GSTNumber); set { } }

        [NotMapped]
        public bool HasMetalExchange { get => false; set { } }

        [NotMapped]
        public string ExchangeMetalType { get => string.Empty; set { } }

        [NotMapped]
        public string ExchangeMetalPurity { get => string.Empty; set { } }

        [NotMapped]
        public decimal ExchangeMetalWeight { get => 0; set { } }

        [NotMapped]
        public decimal ExchangeValue { get => 0; set { } }

        [NotMapped]
        public int? EMIMonths { get => null; set { } }

        [NotMapped]
        public decimal EMIAmount { get => 0; set { } }

        // Navigation Properties
        public virtual Customer Customer { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
        public virtual ICollection<Finance> Payments { get; set; }
        
        /// <summary>
        /// Calculates all order totals based on the simplified pricing model
        /// </summary>
        public void CalculateOrderTotals()
        {
            if (OrderDetails == null || !OrderDetails.Any())
                return;
            
            // 1. Calculate the total (sum of all product prices)
            TotalAmount = OrderDetails.Sum(od => od.TotalAmount);
            
            // 2. Apply discount to get price before tax
            PriceBeforeTax = TotalAmount + DiscountAmount; // Note: discount can be negative
            if (PriceBeforeTax < 0) PriceBeforeTax = 0; // Ensure it doesn't go negative
            
            // 3. Calculate final price with 3% tax
            GrandTotal = Math.Round(PriceBeforeTax * 1.03m, 2); // Add 3% tax for final price
            
            // 4. Set total items count
            TotalItems = OrderDetails.Sum(od => (int)od.Quantity);
        }
    }
}