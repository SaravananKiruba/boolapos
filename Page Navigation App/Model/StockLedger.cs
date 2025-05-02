using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class StockLedger
    {
        [Key]
        public int StockLedgerID { get; set; }
        
        [Required]
        public int ProductID { get; set; }
        
        [Required]
        public DateTime TransactionDate { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(10,3)")]
        public decimal Quantity { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        
        [Required]
        [StringLength(50)]
        public string TransactionType { get; set; } // "Sale", "Purchase", "Return", "Exchange", etc.
        
        [StringLength(50)]
        public string ReferenceID { get; set; } // OrderID, PurchaseID, etc.
        
        [StringLength(100)]
        public string ReferenceNumber { get; set; } // Invoice Number, etc.
        
        [StringLength(500)]
        public string Notes { get; set; }
        
        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
    }
}