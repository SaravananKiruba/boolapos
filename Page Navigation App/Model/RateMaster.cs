using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace Page_Navigation_App.Model
{
    public class RateMaster : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RateID { get; set; }        // Properties referenced in RateManagementService and RateMasterService
        public decimal RatePerGram { get => Rate; set => Rate = value; }
        public decimal MakingChargePercentage { get; set; } = 0;
        public DateTime UpdatedDate { get; set; }
        public DateTime RateDate { get => EffectiveDate; set => EffectiveDate = value; }
        public string Description { get; set; }private string _metalType;
        [Required]
        [StringLength(50)]
        public string MetalType
        {
            get => _metalType;
            set
            {
                if (_metalType != value)
                {
                    _metalType = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _purity;
        [Required]
        [StringLength(10)]
        public string Purity
        {
            get => _purity;
            set
            {
                if (_purity != value)
                {
                    _purity = value;
                    OnPropertyChanged();
                }
            }
        }

        private decimal _rate;
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal Rate
        {
            get => _rate;
            set
            {
                if (_rate != value)
                {
                    _rate = value;
                    OnPropertyChanged();
                }
            }
        }

        private DateTime _effectiveDate;
        [Required]
        public DateTime EffectiveDate
        {
            get => _effectiveDate;
            set
            {
                if (_effectiveDate != value)
                {
                    _effectiveDate = value;
                    OnPropertyChanged();
                }
            }
        }

        private DateTime? _validUntil;
        public DateTime? ValidUntil
        {
            get => _validUntil;
            set
            {
                if (_validUntil != value)
                {
                    _validUntil = value;
                    OnPropertyChanged();
                }
            }
        }        private bool _isActive;
        [Required]
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _source;
        [Required]
        [StringLength(50)]
        public string Source
        {
            get => _source;
            set
            {
                if (_source != value)
                {
                    _source = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _enteredBy;
        [Required]
        [StringLength(50)]
        public string EnteredBy
        {
            get => _enteredBy;
            set
            {
                if (_enteredBy != value)
                {
                    _enteredBy = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _updatedBy;
        public string UpdatedBy
        {
            get => _updatedBy;
            set
            {
                if (_updatedBy != value)
                {
                    _updatedBy = value;
                    OnPropertyChanged();
                }
            }
        }[NotMapped]
        public bool IsCurrentRate => IsActive && 
            EffectiveDate <= DateTime.Now && 
            (!ValidUntil.HasValue || ValidUntil.Value > DateTime.Now);
    }
}