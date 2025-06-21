using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class Supplier
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SupplierID { get; set; }

        [Required]
        [StringLength(100)]
        public string SupplierName { get; set; } = "Ratanlal Jewellers";
        
        // Add Name alias property
        [NotMapped]
        public string Name { get => SupplierName; set => SupplierName = value; }

        [Required]
        [StringLength(15)]
        [RegularExpression(@"^\+?[1-9]\d{1,14}$")]
        public string PhoneNumber { get; set; } = "+919898989898";
        
        // Add property alias for compatibility with existing code
        [NotMapped]
        public string ContactNumber { get => PhoneNumber; set => PhoneNumber = value; }
        
        // Add Mobile alias property
        [NotMapped]
        public string Mobile { get => PhoneNumber; set => PhoneNumber = value; }

        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = "info@ratanlal.com";

        [StringLength(200)]
        public string Address { get; set; } = "45 Jewellers Market, Mumbai";

        [StringLength(100)]
        public string City { get; set; } = "Mumbai";
        
        [StringLength(100)]
        public string ContactPerson { get; set; } = "Rakesh Sharma";

        [StringLength(20)]
        [RegularExpression(@"^\d{2}[A-Z]{5}\d{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$")]
        public string GSTNumber { get; set; } = "27AABCS1429B1ZB";

        [Required]
        public bool IsActive { get; set; } = true;
        
        // Add RegistrationDate property
        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual ICollection<Product> Products { get; set; }
    }
}