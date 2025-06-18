using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    /// <summary>
    /// Entity to log Hallmark Unique ID (HUID) activities like registration, transfers, etc.
    /// </summary>
    public class HUIDLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LogID { get; set; }
        
        [Required]
        [StringLength(20)]
        public string HUID { get; set; }
        
        [Required]
        public int ProductID { get; set; }
        
        [Required]
        [StringLength(50)]
        public string ActivityType { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Description { get; set; }
        
        [Required]
        public DateOnly ActivityDate { get; set; }
        
        // Navigation property
        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
    }
}