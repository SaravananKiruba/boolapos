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
    }
}