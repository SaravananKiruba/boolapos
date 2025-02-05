using System.ComponentModel.DataAnnotations;

namespace Page_Navigation_App.Model
{
    public class Customer
    {
        [Key]
        public int CustomerID { get; set; }
        public string CustomerName { get; set; }
        public string PhoneNumber { get; set; }
        public string ContactPerson { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public string WhatsAppNumber { get; set; }
    }
}
