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
        [StringLength(20)]
        public string MetalType { get; set; }  // Gold/Silver/Platinum

        [Required]
        [StringLength(10)]
        public string Purity { get; set; }  // 18k/22k/24k

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal Rate { get; set; }

        [Required]
        public DateTime EffectiveDate { get; set; }

        public DateTime? ValidUntil { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [StringLength(20)]
        public string Source { get; set; }  // Market/Association/Custom

        [Required]
        [StringLength(50)]
        public string EnteredBy { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }
        public decimal SaleRate { get; set; }
        public decimal PurchaseRate { get; set; }
        public string UpdatedBy { get; set; }
    }
}