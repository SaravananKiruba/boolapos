using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Page_Navigation_App.Model
{
    public class Supplier
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SupplierID { get; set; }

        [Required]
        [StringLength(100)]
        public string SupplierName { get; set; }

        [Required]
        [StringLength(15)]
        public string ContactNumber { get; set; }

        [StringLength(100)]
        public string ContactPerson { get; set; }

        [Required]
        [StringLength(200)]
        public string Address { get; set; }

        [StringLength(20)]
        public string GSTNumber { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CreditLimit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentBalance { get; set; }

        public string PaymentTerms { get; set; }

        [StringLength(15)]
        public string WhatsAppNumber { get; set; }

        public string BankName { get; set; }
        
        public string AccountNumber { get; set; }
        
        public string IFSCCode { get; set; }

        public bool IsActive { get; set; }

        // Navigation Properties
        public ICollection<Product> Products { get; set; }
        public ICollection<Finance> Payments { get; set; }
    }
}