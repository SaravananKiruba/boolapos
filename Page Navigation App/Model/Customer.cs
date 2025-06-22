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
        public int CustomerID { get; set; }        [Required(ErrorMessage = "Customer Name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Customer Name must be between 2 and 100 characters")]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; }
        
        // Add Name alias property for UI consistency
        [NotMapped]
        public string Name { get => CustomerName; set => CustomerName = value; }
        
        // First and Last name for detailed record keeping
        [StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }
        
        [StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }        [Required(ErrorMessage = "Phone Number is required")]
        [StringLength(15)]
        [RegularExpression(@"^\+?[1-9]\d{1,14}$", ErrorMessage = "Please enter a valid phone number")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
        
        // Mobile alias property for UI consistency
        [NotMapped]
        public string Mobile { get => PhoneNumber; set => PhoneNumber = value; }        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(100)]
        [Display(Name = "Email Address")]
        public string Email { get; set; }        [StringLength(15)]
        [RegularExpression(@"^\+?[1-9]\d{1,14}$", ErrorMessage = "Please enter a valid WhatsApp number")]
        [Display(Name = "WhatsApp Number")]
        public string WhatsAppNumber { get; set; }
        #endregion

        #region Address Information        [StringLength(200)]
        [Display(Name = "Address")]
        public string Address { get; set; }        [StringLength(100)]
        [Display(Name = "City")]
        public string City { get; set; }        [StringLength(20)]
        [RegularExpression(@"^(NA|na|^\d{2}[A-Z]{5}\d{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1})$", 
            ErrorMessage = "Enter a valid GST number or 'NA'")]
        [Display(Name = "GST Number")]
        public string GSTNumber { get; set; }
        #endregion

        #region Personal Details
        [Display(Name = "Date of Birth")]
        public DateOnly? DateOfBirth { get; set; } 

        [Display(Name = "Registration Date")]
        public DateOnly RegistrationDate { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Anniversary Date")]
        public DateOnly? DateOfAnniversary { get; set; }        [Required(ErrorMessage = "Customer Type is required")]
        [StringLength(20)]
        [Display(Name = "Customer Type")]
        public string CustomerType { get; set; }
        #endregion        // Preferences, Financial Information, and Jewelry Measurements have been removed

        #region Additional Information
        [StringLength(500)]
        [Display(Name = "Family Details")]
        public string FamilyDetails { get; set; }  // Family details for occasion reminders
        #endregion

        #region Navigation Properties
        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<Finance> Payments { get; set; }
        #endregion
    }
}