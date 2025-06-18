using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Input;
using Page_Navigation_App.Model;
using Page_Navigation_App.Utilities;
using Page_Navigation_App.Services;
using System.Collections.Generic;
using System;

namespace Page_Navigation_App.ViewModel
{
    public class CustomerVM : ViewModelBase
    {
        private readonly CustomerService _customerService;

        public ICommand AddOrUpdateCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public ObservableCollection<string> CustomerTypes { get; set; } = new ObservableCollection<string>
        {
            "Regular",
            "Gold",
            "Silver",
            "Platinum",
            "Corporate",
            "Wholesale"
        };

        public ObservableCollection<Customer> Customers { get; set; } = new ObservableCollection<Customer>();

        private Customer _selectedCustomer = new Customer();
        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                _selectedCustomer = value;
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
                // Automatically search when the term changes
                SearchCustomers();
            }
        }

        public CustomerVM(CustomerService customerService)
        {
            _customerService = customerService;
            LoadCustomers();

            // Initialize new customer with default type and registration date
            SelectedCustomer.CustomerType = CustomerTypes.First();
            SelectedCustomer.RegistrationDate = DateOnly.FromDateTime(DateTime.Now);

            AddOrUpdateCommand = new RelayCommand<object>(_ => AddOrUpdateCustomer(), _ => CanAddOrUpdateCustomer());
            ClearCommand = new RelayCommand<object>(_ => ClearForm(), _ => true);
            SearchCommand = new RelayCommand<object>(_ => SearchCustomers(), _ => true);
            EditCommand = new RelayCommand<Customer>(customer => EditCustomer(customer), _ => true);
            DeleteCommand = new RelayCommand<Customer>(customer => DeleteCustomer(customer), _ => true);
        }

        private async void LoadCustomers()
        {
            Customers.Clear();
            var customers = await _customerService.GetAllCustomers();
            foreach (var customer in customers)
            {
                Customers.Add(customer);
            }
        }

        private async void AddOrUpdateCustomer()
        {
            // If this is a new customer, set the registration date
            if (SelectedCustomer.CustomerID == 0)
            {
                SelectedCustomer.RegistrationDate = DateOnly.FromDateTime(DateTime.Now);
            }

            // Validate the customer before saving
            var validationContext = new ValidationContext(SelectedCustomer, null, null);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(SelectedCustomer, validationContext, validationResults, true);

            if (!isValid)
            {
                string errorMessage = string.Join("\n", validationResults.Select(v => v.ErrorMessage));
                System.Windows.MessageBox.Show($"Validation Errors:\n{errorMessage}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            try
            {
                if (SelectedCustomer.CustomerID > 0)
                {
                    await _customerService.UpdateCustomer(SelectedCustomer);
                }
                else
                {
                    await _customerService.AddCustomer(SelectedCustomer);
                }

                LoadCustomers();
                ClearForm();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving customer: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void EditCustomer(Customer customer)
        {
            if (customer == null) return;
            
            // Create a new Customer object and copy all properties
            SelectedCustomer = new Customer
            {
                // Basic Information
                CustomerID = customer.CustomerID,
                CustomerName = customer.CustomerName,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                PhoneNumber = customer.PhoneNumber,
                Email = customer.Email,
                WhatsAppNumber = customer.WhatsAppNumber,
                
                // Address Information
                Address = customer.Address,
                City = customer.City,
                GSTNumber = customer.GSTNumber,
                
                // Personal Details
                CustomerType = customer.CustomerType,
                DateOfBirth = customer.DateOfBirth,
                DateOfAnniversary = customer.DateOfAnniversary,
                RegistrationDate = customer.RegistrationDate,
                IsActive = customer.IsActive,
                
                // Preferences
                PreferredDesigns = customer.PreferredDesigns,
                PreferredMetalType = customer.PreferredMetalType,
                RingSize = customer.RingSize,
                BangleSize = customer.BangleSize,
                ChainLength = customer.ChainLength,
                
                // Financial Information
                LoyaltyPoints = customer.LoyaltyPoints,
                CreditLimit = customer.CreditLimit,
                TotalPurchases = customer.TotalPurchases,
                OutstandingAmount = customer.OutstandingAmount,
                IsGoldSchemeEnrolled = customer.IsGoldSchemeEnrolled,
                LastPurchaseDate = customer.LastPurchaseDate,
                
                // Additional Information
                FamilyDetails = customer.FamilyDetails
            };
        }

        private async void DeleteCustomer(Customer customer)
        {
            if (customer == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete {customer.CustomerName}?",
                "Confirm Delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    await _customerService.DeleteCustomer(customer.CustomerID);
                    Customers.Remove(customer);
                    ClearForm();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error deleting customer: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private void ClearForm()
        {
            SelectedCustomer = new Customer
            {
                CustomerType = CustomerTypes.First(), // Set default customer type when clearing form
                RegistrationDate = DateOnly.FromDateTime(DateTime.Now)   // Set current date as registration date for new customers
            };
            SearchTerm = string.Empty;
        }

        private bool CanAddOrUpdateCustomer()
        {
            // Ensure required fields are filled before allowing save
            return !string.IsNullOrEmpty(SelectedCustomer?.CustomerName) &&
                   !string.IsNullOrEmpty(SelectedCustomer?.PhoneNumber) &&
                   !string.IsNullOrEmpty(SelectedCustomer?.CustomerType);
        }

        private async void SearchCustomers()
        {
            try
            {
                if (string.IsNullOrEmpty(SearchTerm))
                {
                    LoadCustomers();
                    return;
                }

                // Search in-memory if we already have customers loaded
                if (Customers.Count > 0)
                {
                    var filteredCustomers = Customers.Where(c => 
                        c.CustomerName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        c.PhoneNumber.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        c.Email?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) == true ||
                        c.City?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) == true ||
                        c.CustomerType.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                    ).ToList();

                    Customers.Clear();
                    foreach (var customer in filteredCustomers)
                    {
                        Customers.Add(customer);
                    }
                }
                else
                {
                    // If no customers are loaded yet, use the service to filter
                    Customers.Clear();
                    var customers = await _customerService.FilterCustomers(SearchTerm);
                    foreach (var customer in customers)
                    {
                        Customers.Add(customer);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error searching customers: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}