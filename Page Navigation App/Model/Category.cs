using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class Category
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CategoryID { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; }

        // Navigation property for Products (One-to-Many relationship)
        public ICollection<Product> Products { get; set; }

        // Navigation property for Subcategories (One-to-Many relationship)
        public ICollection<Subcategory> Subcategories { get; set; }
    }
}