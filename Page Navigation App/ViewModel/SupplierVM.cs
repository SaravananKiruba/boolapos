using System;
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
        public ICommand RecordPurchaseCommand { get; }
        public ICommand RecordPaymentCommand { get; }

        public SupplierVM(SupplierService supplierService)
        {
            _supplierService = supplierService;
            LoadSuppliers();

            AddOrUpdateCommand = new RelayCommand<object>(_ => AddOrUpdateSupplier(), _ => CanAddOrUpdateSupplier());
            ClearCommand = new RelayCommand<object>(_ => ClearForm(), _ => true);
            SearchCommand = new RelayCommand<object>(_ => SearchSuppliers(), _ => true);
            RecordPurchaseCommand = new RelayCommand<object>(_ => RecordPurchase(), _ => CanRecordTransaction());
            RecordPaymentCommand = new RelayCommand<object>(_ => RecordPayment(), _ => CanRecordTransaction());
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

        private string _searchTerm;
        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                _searchTerm = value;
                OnPropertyChanged();
                AutoSelectSupplier();
            }
        }

        private decimal _transactionAmount;
        public decimal TransactionAmount
        {
            get => _transactionAmount;
            set
            {
                _transactionAmount = value;
                OnPropertyChanged();
            }
        }

        private string _referenceNumber;
        public string ReferenceNumber
        {
            get => _referenceNumber;
            set
            {
                _referenceNumber = value;
                OnPropertyChanged();
            }
        }

        private async void LoadSuppliers()
        {
            Suppliers.Clear();
            var suppliers = await _supplierService.GetAllSuppliers();
            foreach (var supplier in suppliers)
            {
                Suppliers.Add(supplier);
            }
        }

        private void AutoSelectSupplier()
        {
            var matchedSupplier = Suppliers.FirstOrDefault(s =>
                !string.IsNullOrEmpty(SearchTerm) && 
                (s.SupplierName.Contains(SearchTerm) || 
                s.ContactNumber.Contains(SearchTerm) ||
                s.Email.Contains(SearchTerm)));

            if (matchedSupplier != null)
            {
                SelectedSupplier = matchedSupplier;
            }
            else
            {
                SelectedSupplier = new Supplier
                {
                    SupplierName = SearchTerm,
                    IsActive = true
                };
            }
        }        private async void AddOrUpdateSupplier()
        {
            try
            {
                // Validate the supplier object before saving
                if (string.IsNullOrEmpty(SelectedSupplier.SupplierName))
                {
                    System.Windows.MessageBox.Show("Supplier name cannot be empty.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrEmpty(SelectedSupplier.ContactNumber))
                {
                    System.Windows.MessageBox.Show("Contact number cannot be empty.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                // Save the supplier based on whether it's an update or a new record
                bool success;
                if (SelectedSupplier.SupplierID > 0)
                {
                    // Update existing supplier
                    success = await _supplierService.UpdateSupplier(SelectedSupplier);
                    if (success)
                    {
                        System.Windows.MessageBox.Show($"Supplier '{SelectedSupplier.SupplierName}' updated successfully.", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show($"Failed to update supplier '{SelectedSupplier.SupplierName}'.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    // Add new supplier
                    var result = await _supplierService.AddSupplier(SelectedSupplier);
                    if (result != null)
                    {
                        System.Windows.MessageBox.Show($"Supplier '{SelectedSupplier.SupplierName}' added successfully.", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show($"Failed to add supplier '{SelectedSupplier.SupplierName}'.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        return;
                    }
                }

                // Refresh the suppliers list and clear the form
                LoadSuppliers();
                ClearForm();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"An error occurred: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ClearForm()
        {
            SelectedSupplier = new Supplier { IsActive = true };
            SearchTerm = string.Empty;
            TransactionAmount = 0;
            ReferenceNumber = string.Empty;
        }

        private bool CanAddOrUpdateSupplier()
        {
            return !string.IsNullOrEmpty(SelectedSupplier.SupplierName) &&
                   !string.IsNullOrEmpty(SelectedSupplier.ContactNumber);
        }

        private async void SearchSuppliers()
        {
            Suppliers.Clear();
            var suppliers = await _supplierService.SearchSuppliers(SearchTerm);
            foreach (var supplier in suppliers)
            {
                Suppliers.Add(supplier);
            }
        }

        private async void RecordPurchase()
        {
            if (SelectedSupplier?.SupplierID > 0 && TransactionAmount > 0)
            {
                await _supplierService.RecordPurchase(
                    SelectedSupplier.SupplierID,
                    TransactionAmount,
                    "Purchase",
                    "Direct",
                    ReferenceNumber);

                LoadSuppliers();
                TransactionAmount = 0;
                ReferenceNumber = string.Empty;
            }
        }

        private async void RecordPayment()
        {
            if (SelectedSupplier?.SupplierID > 0 && TransactionAmount > 0)
            {
                await _supplierService.RecordPayment(
                    SelectedSupplier.SupplierID,
                    TransactionAmount,
                    "Direct",
                    ReferenceNumber);

                LoadSuppliers();
                TransactionAmount = 0;
                ReferenceNumber = string.Empty;
            }
        }

        private bool CanRecordTransaction()
        {
            return SelectedSupplier?.SupplierID > 0 && TransactionAmount > 0;
        }
    }
}