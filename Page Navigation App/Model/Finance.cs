using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class Finance
    {
        [Key]
        public string FinanceID { get; set; } = Guid.NewGuid().ToString();

        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; } // "Income", "Expense", "EMI", "Metal_Purchase", "Gold_Scheme"
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string ReferenceNumber { get; set; }

        // Customer and Order references
        public int? CustomerId { get; set; }
        public virtual Customer Customer { get; set; }
        public int? OrderReference { get; set; }

        // EMI specific properties
        public decimal? TotalAmount { get; set; }
        public decimal? RemainingAmount { get; set; }
        public decimal? InterestRate { get; set; }
        public int? NumberOfInstallments { get; set; }
        public decimal? InstallmentAmount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public DateTime? NextInstallmentDate { get; set; }
        public string Status { get; set; } // "Active", "Completed", "Defaulted"

        // GST components
        public decimal? CGSTAmount { get; set; }
        public decimal? SGSTAmount { get; set; }
        public decimal? IGSTAmount { get; set; }
        public string GSTNumber { get; set; }

        // Metal trade specific properties
        public string MetalType { get; set; }
        public string MetalPurity { get; set; }
        public decimal? MetalWeight { get; set; }
        public decimal? MetalRate { get; set; }
        public string SupplierName { get; set; }
        public decimal? WastagePercentage { get; set; }
        public decimal? MakingCharges { get; set; }
        public decimal? StoneValue { get; set; }
        public string HallmarkNumber { get; set; }
        public decimal? ExchangeMetalWeight { get; set; }
        public string ExchangeMetalType { get; set; }
        public string ExchangeMetalPurity { get; set; }
        public decimal? ExchangeValue { get; set; }

        // Gold scheme specific properties
        public decimal? SchemeTargetGrams { get; set; }
        public decimal? SchemeCollectedGrams { get; set; }
        public decimal? SchemeMonthlyAmount { get; set; }
        public int? SchemeTenureMonths { get; set; }
        public DateTime? SchemeMaturityDate { get; set; }
        public string SchemeStatus { get; set; } // "Active", "Matured", "Cancelled"
        public decimal? SchemeMaturityBonus { get; set; }

        // Calculated properties
        [NotMapped]
        public decimal RemainingInstallments => 
            NumberOfInstallments.HasValue && InstallmentAmount.HasValue && RemainingAmount.HasValue
                ? Math.Ceiling(RemainingAmount.Value / InstallmentAmount.Value)
                : 0;

        [NotMapped]
        public decimal SchemeCompletionPercentage =>
            SchemeTargetGrams.HasValue && SchemeCollectedGrams.HasValue
                ? (SchemeCollectedGrams.Value / SchemeTargetGrams.Value) * 100
                : 0;

        [NotMapped]
        public decimal TotalGSTAmount =>
            (CGSTAmount ?? 0) + (SGSTAmount ?? 0) + (IGSTAmount ?? 0);

        [NotMapped]
        public decimal NetAmount => Amount - TotalGSTAmount;

        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }
}