using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class Customer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerID { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string CustomerName { get; set; }

        [Required]
        [StringLength(15)]
        [RegularExpression(@"^\+?[1-9]\d{1,14}$")]
        public string PhoneNumber { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        [StringLength(100)]
        public string City { get; set; }

        [StringLength(20)]
        [RegularExpression(@"^\d{2}[A-Z]{5}\d{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$")]
        public string GSTNumber { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public DateTime RegistrationDate { get; set; }
        public int LoyaltyPoints { get; set; }
        public bool IsActive { get; set; }
        public DateTime? DateOfAnniversary { get; set; }
        public bool NotifyRateChanges { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 9999999999.99)]
        public decimal CreditLimit { get; set; }

        [Required]
        [StringLength(20)]
        public string? CustomerType { get; set; } // Retail, Wholesale

        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(15)]
        [RegularExpression(@"^\+?[1-9]\d{1,14}$")]
        public string WhatsAppNumber { get; set; }

        [StringLength(500)]
        public string PreferredDesigns { get; set; }  // Customer's design preferences

        [StringLength(100)]
        public string PreferredMetalType { get; set; }  // Preferred metal type

        [StringLength(100)]
        public string RingSize { get; set; }  // Customer's ring size for future reference

        [StringLength(100)]
        public string BangleSize { get; set; }  // Customer's bangle size

        [StringLength(100)]
        public string ChainLength { get; set; }  // Preferred chain length

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPurchases { get; set; }  // Total purchase amount

        [Column(TypeName = "decimal(18,2)")]
        public decimal OutstandingAmount { get; set; }  // Current outstanding amount

        public bool IsGoldSchemeEnrolled { get; set; }  // Whether enrolled in gold savings scheme

        public DateTime? LastPurchaseDate { get; set; }  // Date of last purchase

        [StringLength(500)]
        public string FamilyDetails { get; set; }  // Family details for occasion reminders

        // Navigation properties
        public ICollection<Order> Orders { get; set; }
        public ICollection<RepairJob> RepairJobs { get; set; }
        public ICollection<Finance> Payments { get; set; }
    }
}