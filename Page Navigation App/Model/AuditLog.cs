using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class AuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [StringLength(50)]
        public string UserID { get; set; }
        
        // Property referenced in LogService
        public string UserId { get => UserID; set => UserID = value; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; }  // Create/Update/Delete/Access

        [Required]
        [StringLength(50)]
        public string EntityName { get; set; }  // Product/Customer/Order etc.

        [StringLength(50)]
        public string EntityID { get; set; }
        
        // Property referenced in LogService
        public string EntityId { get => EntityID; set => EntityID = value; }

        [StringLength(4000)]
        public string OldValues { get; set; }

        [StringLength(4000)]
        public string NewValues { get; set; }

        [StringLength(500)]
        public string Details { get; set; }

        [Required]
        [StringLength(50)]
        public string IPAddress { get; set; }

        [Required]
        public DateTime ModifiedAt { get; set; }

        [StringLength(100)]
        public string ModifiedBy { get; set; }
    }
}