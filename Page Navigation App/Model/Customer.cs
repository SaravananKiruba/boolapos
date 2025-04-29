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
        [StringLength(100)]
        public string CustomerName { get; set; }

        [Required]
        [StringLength(15)]
        public string PhoneNumber { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        [StringLength(100)]
        public string City { get; set; }

        [StringLength(20)]
        public string GSTNumber { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public DateTime? Anniversary { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CreditLimit { get; set; }

        [Required]
        public string CustomerType { get; set; } // Retail, Wholesale

        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(15)]
        public string WhatsAppNumber { get; set; }

        // Navigation properties
        public ICollection<Order> Orders { get; set; }
        public ICollection<RepairJob> RepairJobs { get; set; }
        public ICollection<Finance> Payments { get; set; }
    }
}