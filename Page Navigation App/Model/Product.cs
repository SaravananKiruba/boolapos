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
        public decimal ProductPrice { get; set; }  // Will be calculated including all charges
        
        // Added BasePrice for backward compatibility
        [NotMapped]
        public decimal BasePrice { get => ProductPrice * 0.85m; set => ProductPrice = value / 0.85m; }
        
        // Added FinalPrice for backward compatibility
        [NotMapped]
        public decimal FinalPrice { get => ProductPrice; set => ProductPrice = value; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999.99)]
        public decimal MakingCharges { get; set; } = 1200.00m; // Changed to amount instead of percentage

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
        public string BISStandard { get; set; }  // BIS standard reference number        // Method to calculate product price based on requirements
        public void CalculateProductPrice(decimal ratePerGram)
        {
            // Formula: ((Product weight + Wastage %) * Gold rate) + making charges
            // Calculate effective weight including wastage
            decimal effectiveWeight = CalculateEffectiveWeight();
            
            // Calculate the metal value
            decimal metalValue = effectiveWeight * ratePerGram;
            
            // Store the current metal price for reference
            MetalPrice = metalValue;
            
            // Calculate final product price
            ProductPrice = metalValue + MakingCharges;
            
            // Update the last price update date
            LastPriceUpdate = DateOnly.FromDateTime(DateTime.Now);
        }
        
        // Calculate effective weight including wastage
        public decimal CalculateEffectiveWeight()
        {
            // Add wastage percentage to the net weight
            decimal wastageAmount = NetWeight * (WastagePercentage / 100);
            return NetWeight + wastageAmount;
        }

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

        // Properties referenced in HUIDTrackingService and RateManagementService
        public string AHCCode { get; set; }
        public string JewelType { get; set; }
        public DateOnly? HUIDRegistrationDate { get; set; }
        public decimal Wastage { get => WastagePercentage; set => WastagePercentage = value; }
        public decimal MetalPrice { get; set; }
        public decimal MakingCharge { get => MakingCharges; set => MakingCharges = value; }
        public DateOnly? LastPriceUpdate { get; set; }
        
        // Method to get the current product value based on provided rate
        public decimal GetCurrentValue(decimal currentRatePerGram)
        {
            decimal effectiveWeight = CalculateEffectiveWeight();
            return (effectiveWeight * currentRatePerGram) + MakingCharges;
        }
        
        // Method to display the price breakdown
        public string GetPriceBreakdown(decimal ratePerGram)
        {
            decimal effectiveWeight = CalculateEffectiveWeight();
            decimal wastageWeight = NetWeight * (WastagePercentage / 100);
            decimal metalValue = effectiveWeight * ratePerGram;
            
            return $"Net Weight: {NetWeight}g\n" +
                   $"Wastage ({WastagePercentage}%): {wastageWeight}g\n" +
                   $"Effective Weight: {effectiveWeight}g\n" +
                   $"Rate: ₹{ratePerGram}/g\n" +
                   $"Metal Value: ₹{metalValue}\n" +
                   $"Making Charges: ₹{MakingCharges}\n" +
                   $"Total Price: ₹{metalValue + MakingCharges}";
        }
    }
}