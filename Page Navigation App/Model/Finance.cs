using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class Finance
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Customer")]
        public int CustomerId { get; set; }

        [Required]
        [StringLength(50)]
        public string OrderReference { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal TotalAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal RemainingAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal InterestRate { get; set; }

        [Required]
        [Range(1, 60)]
        public int NumberOfInstallments { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal InstallmentAmount { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime NextInstallmentDate { get; set; }

        public DateTime? LastPaymentDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; }  // Active/Completed/Defaulted

        [StringLength(500)]
        public string Notes { get; set; }

        // Navigation properties
        public virtual Customer Customer { get; set; }
        public virtual Order Order { get; set; }
    }
}