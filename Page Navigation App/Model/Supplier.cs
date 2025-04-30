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
        [RegularExpression(@"^\+?[1-9]\d{1,14}$")]
        public string ContactNumber { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        [StringLength(100)]
        public string City { get; set; }

        [StringLength(20)]
        [RegularExpression(@"^\d{2}[A-Z]{5}\d{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$")]
        public string GSTNumber { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(100)]
        public string ContactPerson { get; set; }

        [StringLength(15)]
        [RegularExpression(@"^\+?[1-9]\d{1,14}$")]
        public string WhatsAppNumber { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 9999999999.99)]
        public decimal CreditLimit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentBalance { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        [StringLength(100)]
        public string BankName { get; set; }

        [StringLength(50)]
        public string AccountNumber { get; set; }

        [StringLength(20)]
        public string IFSCCode { get; set; }

        [StringLength(50)]
        public string PaymentTerms { get; set; }

        public DateTime? LastPurchaseDate { get; set; }

        // Navigation Properties
        public virtual ICollection<Product> Products { get; set; }
        public virtual ICollection<Finance> Payments { get; set; }
    }
}