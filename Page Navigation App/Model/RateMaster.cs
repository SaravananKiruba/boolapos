using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class RateMaster
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RateID { get; set; }

        [Required]
        public DateTime EffectiveDate { get; set; }

        [Required]
        public string MetalType { get; set; } // Gold/Silver/Platinum

        [Required]
        public string Purity { get; set; } // 18k/22k/24k

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Rate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchaseRate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SaleRate { get; set; }

        public string UpdatedBy { get; set; }

        public bool IsActive { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }
    }
}