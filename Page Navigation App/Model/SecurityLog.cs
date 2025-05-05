using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class SecurityLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        [StringLength(50)]
        public string UserID { get; set; }
        
        // Property referenced in LogService
        [NotMapped]
        public string UserId { get => UserID; set => UserID = value; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; }
        
        // Property referenced in LogService
        [NotMapped]
        public string Details { get => Description; set => Description = value; }

        [Required]
        public bool IsSuccessful { get; set; }

        [Required]
        [StringLength(50)]
        public string IPAddress { get; set; }
        
        // Property referenced in LogService
        [NotMapped]
        public string IpAddress { get => IPAddress; set => IPAddress = value; }

        [Required]
        [StringLength(500)]
        public string UserAgent { get; set; }

        [StringLength(100)]
        public string Module { get; set; }

        [StringLength(100)]
        public string Severity { get; set; }  // Info/Warning/Critical

        [StringLength(500)]
        public string AdditionalData { get; set; }
    }
}