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
        public string CategoryName { get; set; } = "Necklaces";

        [StringLength(200)]
        public string Description { get; set; } = "Gold and Silver necklaces collection";

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal DefaultMakingCharges { get; set; } = 12.00m; // 12% making charges

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal DefaultWastage { get; set; } = 3.50m; // 3.5% wastage

        [Required]
        public bool IsActive { get; set; } = true;

        [StringLength(50)]
        public string DisplayOrder { get; set; } = "1";

        // Navigation Properties
        public ICollection<Product> Products { get; set; }
        public ICollection<Subcategory> Subcategories { get; set; }
    }
}