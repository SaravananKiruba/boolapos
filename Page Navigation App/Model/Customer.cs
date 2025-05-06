using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    /// <summary>
    /// Represents a customer in the jewelry business
    /// </summary>
    public class Customer
    {
        #region Basic Information
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerID { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = "Rajesh Kumar";
        
        // Add Name alias property for UI consistency
        [NotMapped]
        public string Name { get => CustomerName; set => CustomerName = value; }
        
        // First and Last name for detailed record keeping
        [StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }
        
        [StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [StringLength(15)]
        [RegularExpression(@"^\+?[1-9]\d{1,14}$", ErrorMessage = "Please enter a valid phone number")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = "+919876543210";
        
        // Mobile alias property for UI consistency
        [NotMapped]
        public string Mobile { get => PhoneNumber; set => PhoneNumber = value; }

        [EmailAddress]
        [StringLength(100)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = "rajesh.kumar@email.com";

        [StringLength(15)]
        [RegularExpression(@"^\+?[1-9]\d{1,14}$", ErrorMessage = "Please enter a valid WhatsApp number")]
        [Display(Name = "WhatsApp Number")]
        public string WhatsAppNumber { get; set; } = "+919876543210";
        #endregion

        #region Address Information
        [StringLength(200)]
        [Display(Name = "Address")]
        public string Address { get; set; } = "123 Main Street, Bangalore";

        [StringLength(100)]
        [Display(Name = "City")]
        public string City { get; set; } = "Bangalore";

        [StringLength(20)]
        [RegularExpression(@"^(NA|na|^\d{2}[A-Z]{5}\d{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1})$", 
            ErrorMessage = "Enter a valid GST number or 'NA'")]
        [Display(Name = "GST Number")]
        public string GSTNumber { get; set; } = "29ABCDE1234F1Z5";
        #endregion

        #region Personal Details
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; } = new DateTime(1985, 6, 15);

        [Display(Name = "Registration Date")]
        public DateTime RegistrationDate { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Anniversary Date")]
        public DateTime? DateOfAnniversary { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Customer Type")]
        public string CustomerType { get; set; } = "Gold";
        #endregion

        #region Preferences
        [StringLength(500)]
        [Display(Name = "Preferred Designs")]
        public string PreferredDesigns { get; set; } = "Traditional designs";

        [StringLength(100)]
        [Display(Name = "Preferred Metal Type")]
        public string PreferredMetalType { get; set; } = "Gold";

        [StringLength(100)]
        [Display(Name = "Ring Size")]
        public string RingSize { get; set; }  // Customer's ring size for future reference

        [StringLength(100)]
        [Display(Name = "Bangle Size")]
        public string BangleSize { get; set; }  // Customer's bangle size

        [StringLength(100)]
        [Display(Name = "Chain Length")]
        public string ChainLength { get; set; }  // Preferred chain length
        #endregion

        #region Financial Information
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total Purchases")]
        public decimal TotalPurchases { get; set; }  // Total purchase amount

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Outstanding Amount")]
        public decimal OutstandingAmount { get; set; }  // Current outstanding amount

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Credit Limit")]
        public decimal CreditLimit { get; set; } = 10000;  // Credit limit for the customer

        [Display(Name = "Loyalty Points")]
        public int LoyaltyPoints { get; set; } = 0;  // Loyalty points accumulated by customer

        [Display(Name = "Gold Scheme Enrolled")]
        public bool IsGoldSchemeEnrolled { get; set; }  // Whether enrolled in gold savings scheme

        [Display(Name = "Last Purchase Date")]
        public DateTime? LastPurchaseDate { get; set; }  // Date of last purchase
        #endregion

        #region Additional Information
        [StringLength(500)]
        [Display(Name = "Family Details")]
        public string FamilyDetails { get; set; }  // Family details for occasion reminders
        #endregion

        #region Navigation Properties
        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<RepairJob> RepairJobs { get; set; }
        public virtual ICollection<Finance> Payments { get; set; }
        #endregion
    }
}