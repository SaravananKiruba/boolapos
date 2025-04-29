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
        [StringLength(50)]
        public string CategoryName { get; set; }

        [StringLength(200)]
        public string Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal DefaultMakingCharges { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal DefaultWastage { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [StringLength(50)]
        public string DisplayOrder { get; set; }

        // Navigation Properties
        public ICollection<Product> Products { get; set; }
        public ICollection<Subcategory> Subcategories { get; set; }
    }
}