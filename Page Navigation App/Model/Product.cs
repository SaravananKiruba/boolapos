using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductID { get; set; }

        [Required]
        [StringLength(100)]
        public string ProductName { get; set; } = "Traditional Gold Necklace with Ruby";

        // Add property alias for Name
        [NotMapped]
        public string Name { get => ProductName; set => ProductName = value; }

        [Required]
        [StringLength(50)]
        public string MetalType { get; set; } = "Gold";

        [Required]
        [StringLength(10)]
        public string Purity { get; set; } = "22k";

        // Add property alias for PurityLevel
        [NotMapped]
        public string PurityLevel { get => Purity; set => Purity = value; }

        [Required]
        [Column(TypeName = "decimal(10,3)")]
        [Range(0.001, 9999.999)]
        public decimal GrossWeight { get; set; } = 25.850m;

        [Required]
        [Column(TypeName = "decimal(10,3)")]
        [Range(0.001, 9999.999)]
        public decimal NetWeight { get; set; } = 23.450m;

        [Required]
        [StringLength(20)]
        public string Barcode { get; set; }  // Will be auto-generated

        // Add HUID tracking
        [StringLength(20)]
        public string HUID { get; set; }  // Hallmark Unique ID as per BIS regulations

        [StringLength(50)]
        public string TagNumber { get; set; }  // For physical tag/RFID integration

        [StringLength(500)]
        public string Description { get; set; } = "Traditional gold necklace with ruby stone work and antique finish";

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BasePrice { get; set; }  // Will be calculated based on current rate

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal FinalPrice { get; set; }  // Will be calculated including all charges

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal MakingCharges { get; set; } = 12.00m;

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal WastagePercentage { get; set; } = 3.50m;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal StoneValue { get; set; } = 15000.00m;

        [StringLength(500)]
        public string StoneDetails { get; set; } = "5 Ruby stones, total 2.4 carats";

        [Column(TypeName = "decimal(10,3)")]
        [Range(0, 9999.999)]
        public decimal StoneWeight { get; set; } = 2.400m;

        public string HallmarkNumber { get; set; }  // Will be assigned after hallmarking

        // Add IsHallmarked property
        public bool IsHallmarked { get; set; } = false;

        [StringLength(50)]
        public string Collection { get; set; } = "Traditional";

        // Add BIS compliance fields
        public bool IsBISCertified { get; set; } = false;
        
        [StringLength(50)]
        public string BISStandard { get; set; }  // BIS standard reference number

        // GST related properties
        [Column(TypeName = "decimal(18,2)")]
        public decimal GstAmount { get; set; } = 0.00m;  // Total GST charged
        
        [Column(TypeName = "decimal(5,2)")]
        public decimal GstPercentage { get; set; } = 3.00m;  // Default to 3% GST rate
        
        public bool IsGstApplicable { get; set; } = false;  // Auto-determined based on HUID

        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal ValueAdditionPercentage { get; set; } = 5.00m;

        public bool IsCustomOrder { get; set; } = false;

        [Required]
        public bool IsActive { get; set; } = true;

        [Column(TypeName = "int")]
        public int StockQuantity { get; set; } = 0;

        [Column(TypeName = "int")]
        public int ReorderLevel { get; set; } = 5;

        [StringLength(100)]
        public string Design { get; set; } = "Antique finish with temple work";

        [StringLength(50)]
        public string Size { get; set; } = "16 inches";        [Required]
        [ForeignKey("Supplier")]
        public int SupplierID { get; set; } = 1;  // Refers to "Ratanlal Jewellers"

        // Navigation properties
        public virtual Supplier Supplier { get; set; }
        public virtual ICollection<Stock> Stocks { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }

        // Properties referenced in HUIDTrackingService and RateManagementService        public string AHCCode { get; set; }
        public string JewelType { get; set; }
        public DateOnly? HUIDRegistrationDate { get; set; }
        public decimal Wastage { get => WastagePercentage; set => WastagePercentage = value; }
        public decimal MetalPrice { get; set; }
        public decimal MakingCharge { get => MakingCharges; set => MakingCharges = value; }
        public DateOnly? LastPriceUpdate { get; set; }
    }
}