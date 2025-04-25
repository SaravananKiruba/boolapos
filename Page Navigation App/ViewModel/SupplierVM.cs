using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Page_Navigation_App.Model;
using Page_Navigation_App.Services;
using Page_Navigation_App.Utilities;
using System.Collections.Generic;

namespace Page_Navigation_App.ViewModel
{
    public class VendorVM : ViewModelBase
    {
        private readonly VendorService _vendorService;

        public ICommand AddOrUpdateCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand SearchCommand { get; }

        public VendorVM(VendorService vendorService)
        {
            _vendorService = vendorService;
            LoadVendors();

            AddOrUpdateCommand = new RelayCommand<object>(_ => AddOrUpdateVendor(), _ => CanAddOrUpdateVendor());
            ClearCommand = new RelayCommand<object>(_ => ClearForm(), _ => true);
            SearchCommand = new RelayCommand<object>(_ => SearchVendors(), _ => true);
        }

        public ObservableCollection<Vendor> Vendors { get; set; } = new ObservableCollection<Vendor>();

        private Vendor _selectedVendor = new Vendor();

        public Vendor SelectedVendor
        {
            get => _selectedVendor;
            set
            {
                _selectedVendor = value;
                OnPropertyChanged();
            }
        }

        private string _searchName;
        public string SearchName
        {
            get => _searchName;
            set
            {
                _searchName = value;
                OnPropertyChanged();
                AutoSelectVendor();
            }
        }

        private void LoadVendors()
        {
            Vendors.Clear();
            foreach (var vendor in _vendorService.GetAllVendors())
            {
                Vendors.Add(vendor);
            }
        }

        private void AutoSelectVendor()
        {
            var matchedVendor = Vendors.FirstOrDefault(v =>
                !string.IsNullOrEmpty(SearchName) && v.VendorName.Contains(SearchName));

            if (matchedVendor != null)
            {
                SelectedVendor = matchedVendor;
            }
            else
            {
                SelectedVendor = new Vendor
                {
                    VendorName = SearchName
                };
            }
        }

        private void AddOrUpdateVendor()
        {
            if (SelectedVendor.VendorID > 0)
            {
                _vendorService.UpdateVendor(SelectedVendor);
            }
            else
            {
                _vendorService.AddVendor(SelectedVendor);
            }

            LoadVendors();
            ClearForm();
        }

        private void ClearForm()
        {
            SelectedVendor = new Vendor();
            SearchName = string.Empty;
        }

        private bool CanAddOrUpdateVendor()
        {
            return !string.IsNullOrEmpty(SelectedVendor.VendorName);
        }

        private void SearchVendors()
        {
            Vendors.Clear();
            var filteredVendors = _vendorService.SearchVendors(SearchName);
            foreach (var vendor in filteredVendors)
            {
                Vendors.Add(vendor);
            }
        }
    }
}