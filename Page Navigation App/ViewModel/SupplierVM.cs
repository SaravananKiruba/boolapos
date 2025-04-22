using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Page_Navigation_App.Model;
using Page_Navigation_App.Services;
using Page_Navigation_App.Utilities;
using System.Collections.Generic;

namespace Page_Navigation_App.ViewModel
{
    public class SupplierVM : ViewModelBase
    {
        private readonly SupplierService _supplierService;

        public ICommand AddOrUpdateCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand SearchCommand { get; }

        public SupplierVM(SupplierService supplierService)
        {
            _supplierService = supplierService;
            LoadSuppliers();

            AddOrUpdateCommand = new RelayCommand<object>(_ => AddOrUpdateSupplier(), _ => CanAddOrUpdateSupplier());
            ClearCommand = new RelayCommand<object>(_ => ClearForm(), _ => true);
            SearchCommand = new RelayCommand<object>(_ => SearchSuppliers(), _ => true);
        }

        public ObservableCollection<Supplier> Suppliers { get; set; } = new ObservableCollection<Supplier>();

        private Supplier _selectedSupplier = new Supplier();

        public Supplier SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                _selectedSupplier = value;
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
                AutoSelectSupplier();
            }
        }

        private void LoadSuppliers()
        {
            Suppliers.Clear();
            foreach (var supplier in _supplierService.GetAllSuppliers())
            {
                Suppliers.Add(supplier);
            }
        }

        private void AutoSelectSupplier()
        {
            var matchedSupplier = Suppliers.FirstOrDefault(s =>
                !string.IsNullOrEmpty(SearchName) && s.SupplierName.Contains(SearchName));

            if (matchedSupplier != null)
            {
                SelectedSupplier = matchedSupplier;
            }
            else
            {
                SelectedSupplier = new Supplier
                {
                    SupplierName = SearchName
                };
            }
        }

        private void AddOrUpdateSupplier()
        {
            if (SelectedSupplier.SupplierID > 0)
            {
                _supplierService.UpdateSupplier(SelectedSupplier);
            }
            else
            {
                _supplierService.AddSupplier(SelectedSupplier);
            }

            LoadSuppliers();
            ClearForm();
        }

        private void ClearForm()
        {
            SelectedSupplier = new Supplier();
            SearchName = string.Empty;
        }

        private bool CanAddOrUpdateSupplier()
        {
            return !string.IsNullOrEmpty(SelectedSupplier.SupplierName);
        }

        private void SearchSuppliers()
        {
            Suppliers.Clear();
            var filteredSuppliers = _supplierService.FilterSuppliers(SearchName);
            foreach (var supplier in filteredSuppliers)
            {
                Suppliers.Add(supplier);
            }
        }
    }
}