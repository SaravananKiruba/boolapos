using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class Expense
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ExpenseID { get; set; }

        [Required]
        [StringLength(100)]
        public string Description { get; set; }

        [Required]
        public DateTime ExpenseDate { get; set; } = DateTime.Now;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = "Purchase"; // Purchase, Rent, Salary, Utility, Other

        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Bank Transfer, Credit Card, Other

        [ForeignKey("PurchaseOrder")]
        public int? PurchaseOrderID { get; set; } // Optional link to a purchase order

        [StringLength(200)]
        public string Recipient { get; set; }

        [StringLength(100)]
        public string ReferenceNumber { get; set; } // Invoice number, receipt number, etc.

        [StringLength(500)]
        public string Notes { get; set; }

        // Navigation properties
        public virtual PurchaseOrder PurchaseOrder { get; set; }
    }
}
