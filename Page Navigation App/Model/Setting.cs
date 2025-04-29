using System.ComponentModel.DataAnnotations;

namespace Page_Navigation_App.Model
{
    public class Setting
    {
        [Key]
        public string Key { get; set; }
        
        [Required]
        public string Value { get; set; }
    }
}