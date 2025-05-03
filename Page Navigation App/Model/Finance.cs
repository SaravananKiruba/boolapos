using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class Finance
    {
        [Key]
        public string FinanceID { get; set; } = Guid.NewGuid().ToString();

        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        public string TransactionType { get; set; } = "Income"; // Income, Expense, EMI, Metal_Purchase, Gold_Scheme
        
        // Add Type alias property
        [NotMapped]
        public string Type { get => TransactionType; set => TransactionType = value; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMode { get; set; } = "Cash"; // Cash, Card, UPI, Bank_Transfer, etc.
        
        // Adding PaymentMethod as alias for PaymentMode for compatibility
        [NotMapped]
        public string PaymentMethod 
        { 
            get { return PaymentMode; } 
            set { PaymentMode = value; } 
        }

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = "Sales"; // Sales, Purchase, Operational, etc.

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(50)]
        public string ReferenceNumber { get; set; }  // Invoice/Order number reference
        
        // Add ReferenceID property alias
        [NotMapped]
        public string ReferenceID { get => ReferenceNumber; set => ReferenceNumber = value; }
        
        // Add ReferenceType property
        [StringLength(50)]
        public string ReferenceType { get; set; }

        // Order reference
        public int? OrderReference { get; set; }
        
        // Adding OrderID as alias for OrderReference for compatibility
        [NotMapped]
        public int? OrderID 
        { 
            get { return OrderReference; } 
            set { OrderReference = value; } 
        }

        // Customer reference
        public int? CustomerId { get; set; }
        
        // EMI transaction status
        [StringLength(50)]
        public string Status { get; set; } = "Completed"; // Completed, Pending, Failed, In Progress
        
        // EMI payment dates
        public DateTime? StartDate { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public DateTime? NextInstallmentDate { get; set; }
        
        // EMI specific payment amount
        [Column(TypeName = "decimal(18,2)")]
        public decimal? InstallmentAmount { get; set; }
        
        // GST amounts
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CGSTAmount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? SGSTAmount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? IGSTAmount { get; set; }
        
        // Metal purchase details
        [StringLength(50)]
        public string MetalType { get; set; }
        
        [StringLength(10)]
        public string MetalPurity { get; set; }
        
        [Column(TypeName = "decimal(10,3)")]
        public decimal? MetalWeight { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MetalRate { get; set; }

        [StringLength(100)]
        public string SupplierName { get; set; }

        // EMI specific details
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalAmount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? RemainingAmount { get; set; }
        
        [Column(TypeName = "decimal(5,2)")]
        public decimal? InterestRate { get; set; }
        
        public int? NumberOfInstallments { get; set; }
        
        public int? InstallmentNumber { get; set; }
        
        // Adding CreatedBy property
        [StringLength(100)]
        public string CreatedBy { get; set; }
        
        // Add RecordedBy property alias
        [NotMapped]
        public string RecordedBy { get => CreatedBy; set => CreatedBy = value; }

        // Navigation property
        public virtual Customer Customer { get; set; }
    }
}