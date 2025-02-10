using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class Subcategory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SubcategoryID { get; set; }

        [Required]
        [StringLength(100)]
        public string SubcategoryName { get; set; }

        [ForeignKey("Category")]
        public int CategoryID { get; set; }

        // Navigation properties
        public Category Category { get; set; }
        public ICollection<Product> Products { get; set; }
    }
}