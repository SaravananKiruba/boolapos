using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class ReportData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ReportID { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        // Properties referenced in ReportService
        public DateTime ReportDate { get => Date; set => Date = value; }
        public object ReportContent { get; set; }
        
        [Required]
        [StringLength(50)]
        public string ReportType { get; set; } // Daily, Weekly, Monthly, Custom
        
        // Sales metrics
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalSales { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal GoldSales { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal SilverSales { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal PlatinumSales { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiamondSales { get; set; }
        
        public int TotalTransactions { get; set; }
        
        public int NewCustomers { get; set; }
        
        // Inventory metrics
        [Column(TypeName = "decimal(10,3)")]
        public decimal GoldInventoryWeight { get; set; }
        
        [Column(TypeName = "decimal(10,3)")]
        public decimal SilverInventoryWeight { get; set; }
        
        [Column(TypeName = "decimal(10,3)")]
        public decimal PlatinumInventoryWeight { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalInventoryValue { get; set; }
        
        // Repair metrics
        public int PendingRepairs { get; set; }
        
        public int CompletedRepairs { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal RepairRevenue { get; set; }
        
        // GST metrics
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCGST { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalSGST { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalIGST { get; set; }
        
        // Financial metrics
        [Column(TypeName = "decimal(18,2)")]
        public decimal CashPayments { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal CardPayments { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal UPIPayments { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal BankTransferPayments { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalExpenses { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal NetProfit { get; set; }
        
        // Serialized JSON data for detailed report
        public string DetailedReportData { get; set; }
        
        // Report generation info
        public DateTime GeneratedOn { get; set; } = DateTime.Now;
        
        [StringLength(100)]
        public string GeneratedBy { get; set; }
    }
}