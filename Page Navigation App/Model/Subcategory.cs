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
        [StringLength(50)]
        public string SubcategoryName { get; set; }

        [StringLength(200)]
        public string Description { get; set; }

        [Required]
        [ForeignKey("Category")]
        public int CategoryID { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal? SpecialMakingCharges { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal? SpecialWastage { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [StringLength(50)]
        public string DisplayOrder { get; set; }

        // Navigation Properties
        public Category Category { get; set; }
        public ICollection<Product> Products { get; set; }
    }
}