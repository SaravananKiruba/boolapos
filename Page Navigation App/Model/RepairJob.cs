using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class RepairJob
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Customer")]
        public string CustomerId { get; set; }

        [Required]
        [StringLength(20)]
        public string JobNumber { get; set; }

        [Required]
        [StringLength(500)]
        public string ItemDetails { get; set; }

        [Required]
        [StringLength(50)]
        public string WorkType { get; set; }  // Repair/Resize/Polish/etc

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal EstimatedAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal FinalAmount { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EstimatedEndDate { get; set; }

        public DateTime? CompletionDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; }  // Pending/InProgress/Completed/Delivered

        [Column(TypeName = "decimal(10,3)")]
        [Range(0, 9999.999)]
        public decimal? ItemWeight { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        [Required]
        [StringLength(20)]
        public string Priority { get; set; }  // Normal/High/Urgent

        // Navigation property
        public virtual Customer Customer { get; set; }
    }
}