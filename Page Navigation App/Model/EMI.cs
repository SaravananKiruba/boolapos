using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class EMI
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EMIID { get; set; }

        [Required]
        [ForeignKey("Order")]
        public int OrderID { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal TotalAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal InstallmentAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal InterestRate { get; set; }

        [Required]
        public int TotalInstallments { get; set; }

        [Required]
        public int RemainingInstallments { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public int PaymentDay { get; set; }  // Day of month when payment is due

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingAmount { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        public DateTime? LastPaymentDate { get; set; }

        public DateTime? NextPaymentDate { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Active";  // Active, Completed, Defaulted

        [StringLength(500)]
        public string Notes { get; set; }

        // Navigation Properties
        public virtual Order Order { get; set; }
        public virtual Customer Customer { get; set; }
    }
}