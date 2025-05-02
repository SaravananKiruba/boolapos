using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Input;
using Page_Navigation_App.Model;
using Page_Navigation_App.Utilities;
using Page_Navigation_App.Services;
using System.Collections.Generic;

namespace Page_Navigation_App.ViewModel
{
    public class CustomerVM : ViewModelBase
    {
        private readonly CustomerService _customerService;

        public ICommand AddOrUpdateCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand SearchCommand { get; }

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

        private string _searchName;
        public string SearchName
        {
            get => _searchName;
            set
            {
                _searchName = value;
                OnPropertyChanged();
                AutoSelectCustomer();
            }
        }

        private string _searchPhone;
        public string SearchPhone
        {
            get => _searchPhone;
            set
            {
                _searchPhone = value;
                OnPropertyChanged();
                AutoSelectCustomer();
            }
        }

        public CustomerVM(CustomerService customerService)
        {
            _customerService = customerService;
            LoadCustomers();

            // Initialize new customer with default type
            SelectedCustomer.CustomerType = CustomerTypes.First();

            AddOrUpdateCommand = new RelayCommand<object>(_ => AddOrUpdateCustomer(), _ => CanAddOrUpdateCustomer());
            ClearCommand = new RelayCommand<object>(_ => ClearForm(), _ => true);
            SearchCommand = new RelayCommand<object>(_ => SearchCustomers(), _ => true);
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

        private void AutoSelectCustomer()
        {
            var matchedCustomer = Customers.FirstOrDefault(c =>
                (!string.IsNullOrEmpty(SearchName) && c.CustomerName.Contains(SearchName)) ||
                (!string.IsNullOrEmpty(SearchPhone) && c.PhoneNumber.Contains(SearchPhone))
            );

            if (matchedCustomer != null)
            {
                SelectedCustomer = matchedCustomer;
            }
            else
            {
                SelectedCustomer = new Customer
                {
                    CustomerName = SearchName,
                    PhoneNumber = SearchPhone,
                    CustomerType = CustomerTypes.First() // Set default customer type for new customers
                };
            }
        }

        private async void AddOrUpdateCustomer()
        {
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

        private void ClearForm()
        {
            SelectedCustomer = new Customer
            {
                CustomerType = CustomerTypes.First() // Set default customer type when clearing form
            };
            SearchName = string.Empty;
            SearchPhone = string.Empty;
        }

        private bool CanAddOrUpdateCustomer()
        {
            // Ensure required fields are filled before allowing save
            return !string.IsNullOrEmpty(SelectedCustomer.CustomerName) &&
                   !string.IsNullOrEmpty(SelectedCustomer.PhoneNumber) &&
                   !string.IsNullOrEmpty(SelectedCustomer.CustomerType);
        }

        private async void SearchCustomers()
        {
            Customers.Clear();
            var customers = await _customerService.FilterCustomers(SearchName);
            foreach (var customer in customers)
            {
                Customers.Add(customer);
            }
        }
    }
}