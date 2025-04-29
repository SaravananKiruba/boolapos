using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class Customer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerID { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string CustomerName { get; set; }

        [Required]
        [StringLength(15)]
        [RegularExpression(@"^\+?[1-9]\d{1,14}$")]
        public string PhoneNumber { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        [StringLength(100)]
        public string City { get; set; }

        [StringLength(20)]
        [RegularExpression(@"^\d{2}[A-Z]{5}\d{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$")]
        public string GSTNumber { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public DateTime? Anniversary { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 9999999999.99)]
        public decimal CreditLimit { get; set; }

        [Required]
        [StringLength(20)]
        public string CustomerType { get; set; } // Retail, Wholesale

        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(15)]
        [RegularExpression(@"^\+?[1-9]\d{1,14}$")]
        public string WhatsAppNumber { get; set; }

        // Navigation properties
        public ICollection<Order> Orders { get; set; }
        public ICollection<RepairJob> RepairJobs { get; set; }
        public ICollection<Finance> Payments { get; set; }
    }
}