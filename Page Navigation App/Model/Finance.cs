using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class Finance
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FinanceID { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; }

        [Required]
        [StringLength(50)]
        public string TransactionType { get; set; } // e.g., Income, Expense

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [ForeignKey("Order")]
        public int? OrderID { get; set; } // Optional, if related to an order

        // Navigation property
        public Order Order { get; set; }
    }
}