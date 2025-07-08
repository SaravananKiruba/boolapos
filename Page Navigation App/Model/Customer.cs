using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Page_Navigation_App.Model
{
    /// <summary>
    /// Represents a customer in the jewelry business
    /// </summary>
    public class Customer : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            ValidateProperty(propertyName);
        }
        #endregion
        
        #region INotifyDataErrorInfo Implementation
        private readonly Dictionary<string, List<string>> _errors = new Dictionary<string, List<string>>();
        
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        
        public bool HasErrors => _errors.Count > 0;
        
        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName))
            {
                return null;
            }
            
            return _errors[propertyName];
        }
        
        private void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
        
        private void ValidateProperty(string propertyName)
        {
            // Clear existing errors for this property
            if (_errors.ContainsKey(propertyName))
            {
                _errors.Remove(propertyName);
                OnErrorsChanged(propertyName);
            }
            
            // Get validation attributes for the property
            var propertyInfo = GetType().GetProperty(propertyName);
            if (propertyInfo == null) return;
            
            var validationContext = new ValidationContext(this) { MemberName = propertyName };
            var validationResults = new List<ValidationResult>();
            
            var value = propertyInfo.GetValue(this);
            bool isValid = Validator.TryValidateProperty(value, validationContext, validationResults);
            
            if (!isValid)
            {
                _errors[propertyName] = validationResults.Select(r => r.ErrorMessage).ToList();
                OnErrorsChanged(propertyName);
            }
        }
        
        // Helper to validate all properties
        public void ValidateAllProperties()
        {
            foreach (var property in GetType().GetProperties())
            {
                ValidateProperty(property.Name);
            }
        }
        #endregion
        #region Basic Information
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerID { get; set; }
        
        private string _customerName;
        [Required(ErrorMessage = "Customer Name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Customer Name must be between 2 and 100 characters")]
        [Display(Name = "Customer Name")]
        public string CustomerName 
        { 
            get => _customerName; 
            set
            {
                if (_customerName != value)
                {
                    _customerName = value;
                    OnPropertyChanged();
                }
            }
        }
        
        // Add Name alias property for UI consistency
        [NotMapped]
        public string Name { get => CustomerName; set => CustomerName = value; }
        
        // First and Last name for detailed record keeping
        [StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }
        
        [StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }        private string _phoneNumber;
        [Required(ErrorMessage = "Phone Number is required")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Phone Number must be exactly 10 digits")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Phone Number must contain only digits (0-9)")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber 
        { 
            get => _phoneNumber; 
            set
            {
                if (_phoneNumber != value)
                {
                    _phoneNumber = value;
                    OnPropertyChanged();
                }
            }
        }
        
        // Mobile alias property for UI consistency
        [NotMapped]
        public string Mobile { get => PhoneNumber; set => PhoneNumber = value; }        private string _email;
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(100)]
        [Display(Name = "Email Address")]
        public string Email 
        { 
            get => _email; 
            set
            {
                if (_email != value)
                {
                    _email = value;
                    OnPropertyChanged();
                }
            }
        }        [StringLength(10, MinimumLength = 10, ErrorMessage = "WhatsApp Number must be exactly 10 digits")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "WhatsApp Number must contain only digits (0-9)")]
        [Display(Name = "WhatsApp Number")]
        public string WhatsAppNumber { get; set; }
        #endregion

        #region Address Information        [StringLength(200)]
        [Display(Name = "Address")]
        public string Address { get; set; }        [StringLength(100)]
        [Display(Name = "City")]
        public string City { get; set; }        [StringLength(20)]
        [RegularExpression(@"^(NA|na|^\d{2}[A-Z]{5}\d{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1})$", 
            ErrorMessage = "Enter a valid GST number or 'NA'")]
        [Display(Name = "GST Number")]
        public string GSTNumber { get; set; }
        #endregion

        #region Personal Details
        [Display(Name = "Date of Birth")]
        public DateOnly? DateOfBirth { get; set; } 

        [Display(Name = "Registration Date")]
        public DateOnly RegistrationDate { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Anniversary Date")]
        public DateOnly? DateOfAnniversary { get; set; }        private string _customerType;
        [Required(ErrorMessage = "Customer Type is required")]
        [StringLength(20)]
        [Display(Name = "Customer Type")]
        public string CustomerType 
        { 
            get => _customerType; 
            set
            {
                if (_customerType != value)
                {
                    _customerType = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion        // Preferences, Financial Information, and Jewelry Measurements have been removed

        #region Additional Information
        [StringLength(500)]
        [Display(Name = "Family Details")]
        public string FamilyDetails { get; set; }  // Family details for occasion reminders
        #endregion

        #region Navigation Properties
        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<Finance> Payments { get; set; }
        #endregion
    }
}