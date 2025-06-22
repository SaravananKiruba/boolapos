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

        public ObservableCollection<Customer> Customers { get; set; } = new ObservableCollection<Customer>();        private Customer _selectedCustomer = new Customer();        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                _selectedCustomer = value;
                OnPropertyChanged();
                
                // Also notify changes for key properties to ensure CanAddOrUpdateCustomer is reevaluated
                OnPropertyChanged(nameof(SelectedCustomer.CustomerName));
                OnPropertyChanged(nameof(SelectedCustomer.PhoneNumber));
                OnPropertyChanged(nameof(SelectedCustomer.CustomerType));
                
                // Force command reevaluation
                CommandManager.InvalidateRequerySuggested();
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
        }        public CustomerVM(CustomerService customerService)
        {
            _customerService = customerService;
            LoadCustomers();

            // Initialize a new customer with empty required fields to force user input
            SelectedCustomer = new Customer
            {
                CustomerName = "", // Empty to make it mandatory
                PhoneNumber = "", // Empty to make it mandatory
                CustomerType = CustomerTypes.First(), // Default value but still mandatory
                RegistrationDate = DateOnly.FromDateTime(DateTime.Now)
            };

            AddOrUpdateCommand = new RelayCommand<object>(_ => AddOrUpdateCustomer(), _ => CanAddOrUpdateCustomer());
            ClearCommand = new RelayCommand<object>(_ => ClearForm(), _ => true);
            SearchCommand = new RelayCommand<object>(_ => SearchCustomers(), _ => true);
            EditCommand = new RelayCommand<Customer>(customer => EditCustomer(customer), _ => true);
            DeleteCommand = new RelayCommand<Customer>(customer => DeleteCustomer(customer), _ => true);
            
            // Use PropertyChanged event to update command state when properties change
            PropertyChanged += (sender, args) =>
            {
                // Invalidate commands whenever any property changes that might affect validation
                if (args.PropertyName == nameof(SelectedCustomer) ||
                    args.PropertyName == nameof(SelectedCustomer.CustomerName) ||
                    args.PropertyName == nameof(SelectedCustomer.PhoneNumber) ||
                    args.PropertyName == nameof(SelectedCustomer.CustomerType))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            };
        }

        private async void LoadCustomers()
        {
            Customers.Clear();
            var customers = await _customerService.GetAllCustomers();
            foreach (var customer in customers)
            {
                Customers.Add(customer);
            }
        }        private async void AddOrUpdateCustomer()
        {
            // If this is a new customer, set the registration date
            if (SelectedCustomer.CustomerID == 0)
            {
                SelectedCustomer.RegistrationDate = DateOnly.FromDateTime(DateTime.Now);
            }

            // Manual validation for required fields
            List<string> errors = new List<string>();
            
            if (string.IsNullOrWhiteSpace(SelectedCustomer.CustomerName))
            {
                errors.Add("Customer Name is required.");
            }

            if (string.IsNullOrWhiteSpace(SelectedCustomer.PhoneNumber))
            {
                errors.Add("Phone Number is required.");
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(SelectedCustomer.PhoneNumber, @"^\+?[1-9]\d{1,14}$"))
            {
                errors.Add("Please enter a valid phone number.");
            }

            if (string.IsNullOrWhiteSpace(SelectedCustomer.CustomerType))
            {
                errors.Add("Customer Type is required.");
            }

            // If we have manual validation errors, show them and return
            if (errors.Count > 0)
            {
                string errorMessage = string.Join("\n", errors);
                System.Windows.MessageBox.Show($"Validation Errors:\n{errorMessage}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            // Also run the standard validation
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
        }        private void EditCustomer(Customer customer)
        {
            if (customer == null) return;

            // Assign customer to SelectedCustomer - this will trigger PropertyChanged notifications
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
                // Preferences, Financial Information, and Jewelry Measurements sections have been removed
                
                // Additional Information
                FamilyDetails = customer.FamilyDetails
            };
            
            // Manually trigger command evaluation to enable save button
            CommandManager.InvalidateRequerySuggested();
        }        private async void DeleteCustomer(Customer customer)
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
                    bool success = await _customerService.DeleteCustomer(customer.CustomerID);
                    
                    if (success)
                    {
                        // If this is the currently selected customer, clear the form
                        if (SelectedCustomer?.CustomerID == customer.CustomerID)
                        {
                            ClearForm();
                        }
                        
                        // Refresh the customer list instead of just removing the item
                        // This ensures we get updated IsActive status for soft-deleted customers
                        LoadCustomers();
                        
                        System.Windows.MessageBox.Show(
                            "Customer has been successfully deleted or marked as inactive.",
                            "Success",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(
                            "Failed to delete customer. Please try again.",
                            "Error",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Error deleting customer: {ex.Message}", 
                        "Error", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Error);
                }
            }
        }        private void ClearForm()
        {
            SelectedCustomer = new Customer
            {
                CustomerName = "", // Clear name explicitly to make it mandatory
                PhoneNumber = "", // Clear phone number explicitly to make it mandatory
                CustomerType = CustomerTypes.First(), // Set default customer type when clearing form
                RegistrationDate = DateOnly.FromDateTime(DateTime.Now)   // Set current date as registration date for new customers
            };
            SearchTerm = string.Empty;
        }private bool CanAddOrUpdateCustomer()
        {
            // Check if SelectedCustomer is null
            if (SelectedCustomer == null)
                return false;
                
            // Ensure required fields are filled before allowing save
            bool hasRequiredFields = !string.IsNullOrWhiteSpace(SelectedCustomer.CustomerName) &&
                                    !string.IsNullOrWhiteSpace(SelectedCustomer.PhoneNumber) &&
                                    !string.IsNullOrWhiteSpace(SelectedCustomer.CustomerType);
                                    
            // Basic phone validation
            bool validPhone = !string.IsNullOrWhiteSpace(SelectedCustomer.PhoneNumber) && 
                             System.Text.RegularExpressions.Regex.IsMatch(
                                 SelectedCustomer.PhoneNumber, 
                                 @"^\+?[1-9]\d{1,14}$");
                                 
            return hasRequiredFields && validPhone;
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