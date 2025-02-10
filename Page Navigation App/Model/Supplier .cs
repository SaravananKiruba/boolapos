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
        public string SupplierName { get; set; }

        [StringLength(15)]
        public string PhoneNumber { get; set; }

        [StringLength(100)]
        public string ContactPerson { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        // Navigation property for Products (One-to-Many relationship)
        public ICollection<Product> Products { get; set; }
    }
}