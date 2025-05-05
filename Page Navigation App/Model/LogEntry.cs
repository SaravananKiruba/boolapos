using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class LogEntry
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        [StringLength(20)]
        public string Level { get; set; }  // Info/Warning/Error/Audit
        
        // Property referenced in LogService
        public string LogLevel { get => Level; set => Level = value; }

        [Required]
        public string Message { get; set; }

        [Required]
        [StringLength(100)]
        public string Source { get; set; }

        [StringLength(50)]
        public string UserID { get; set; }
        
        // Property referenced in LogService
        public string UserId { get => UserID; set => UserID = value; }
        
        // Property referenced in LogService
        public string Exception { get => ExceptionDetails; set => ExceptionDetails = value; }

        [StringLength(100)]
        public string Component { get; set; }

        [StringLength(500)]
        public string StackTrace { get; set; }

        [StringLength(500)]
        public string ExceptionDetails { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Duration { get; set; }  // For performance tracking
    }
}