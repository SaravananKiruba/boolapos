using System.ComponentModel.DataAnnotations;

namespace Page_Navigation_App.Model
{
    public class BusinessInfo
    {
        [Key]
        public int ID { get; set; }
        public string BusinessName { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string TaxId { get; set; }
    }
}