using System;
using System.ComponentModel.DataAnnotations;

namespace Page_Navigation_App.Model
{
    public class BusinessInfo
    {
        [Key]
        public int ID { get; set; }
        public string BusinessName { get; set; }
        
        // Add alias property for Name
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string Name { get => BusinessName; set => BusinessName = value; }
        
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string TaxId { get; set; }
        
        // Add alias property for TaxIdentificationNumber
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string TaxIdentificationNumber { get => TaxId; set => TaxId = value; }
        
        // Add City, State, and PostalCode properties
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        
        // Properties referenced in errors
        public string Country { get; set; }
        public string AlternatePhone { get; set; }
        public string GSTNumber { get; set; }
        public string LogoPath { get; set; }
        public string InvoicePrefix { get; set; }
        public string BankDetails { get; set; }
        public string TermsAndConditions { get; set; }
        public DateTime UpdatedDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public string PhoneNumber { get => Phone; set => Phone = value; }
        public string TagLine { get; set; }
        public string LogoUrl { get => LogoPath; set => LogoPath = value; }
        public string BISRegistrationNumber { get; set; }
        public string OwnerName { get; set; }
        
        // Currency properties
        public string CurrencySymbol { get; set; } = "₹";
        public string CurrencyCode { get; set; } = "INR";
        
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string FormattedCurrencySymbol => !string.IsNullOrEmpty(CurrencySymbol) ? CurrencySymbol : "₹";
    }
}