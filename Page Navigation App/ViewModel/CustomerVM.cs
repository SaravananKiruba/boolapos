// CustomerVM.cs
using System.Collections.ObjectModel;
using System.Windows.Input;
using Page_Navigation_App.Model;
using Page_Navigation_App.Utilities;
using Page_Navigation_App.Services;

namespace Page_Navigation_App.ViewModel
{
    public class CustomerVM : ViewModelBase
    {
        private readonly CustomerService _customerService;

        public ICommand AddCommand { get; }
        public ICommand UpdateCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand FilterCommand { get; }

        public CustomerVM(CustomerService customerService)
        {
            _customerService = customerService;
            LoadCustomers();

            AddCommand = new RelayCommand<object>(_ => AddCustomer(), _ => true);
            UpdateCommand = new RelayCommand<object>(_ => UpdateCustomer(), _ => SelectedCustomer != null);
            DeleteCommand = new RelayCommand<object>(_ => DeleteCustomer(), _ => SelectedCustomer != null);
            FilterCommand = new RelayCommand<object>(_ => FilterCustomer(), _ => true);
        }

        public ObservableCollection<Customer> Customers { get; set; } = new ObservableCollection<Customer>();

        private Customer _selectedCustomer = new()
        {
             CustomerName = "John Doe",
        };

        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                _selectedCustomer = value;
                OnPropertyChanged();
            }
        }

        private void LoadCustomers()
        {
            Customers.Clear();
            foreach (var customer in _customerService.GetAllCustomers())
            {
                Customers.Add(customer);
            }
        }

        private void AddCustomer()
        {
            if (SelectedCustomer == null)
            {
                var newCustomer = new Customer
                {
                    CustomerName = SelectedCustomer.CustomerName,
                    PhoneNumber = SelectedCustomer.PhoneNumber,
                    ContactPerson = SelectedCustomer.ContactPerson,
                    Address = SelectedCustomer.Address,
                    Email = SelectedCustomer.Email,
                    WhatsAppNumber = SelectedCustomer.WhatsAppNumber
                };

                

                _customerService.AddCustomer(newCustomer);
                Customers.Add(newCustomer);
            }
        }

        private void UpdateCustomer()
        {
            if (SelectedCustomer != null)
            {
                _customerService.UpdateCustomer(SelectedCustomer);
                LoadCustomers();
            }
        }

        private void DeleteCustomer()
        {
            if (SelectedCustomer != null)
            {
                _customerService.DeleteCustomer(SelectedCustomer);
                Customers.Remove(SelectedCustomer);
            }
        }

        private void FilterCustomer()
        {
            if (SelectedCustomer != null && !string.IsNullOrWhiteSpace(SelectedCustomer.CustomerName))
            {
                var filteredCustomers = _customerService.FilterCustomers(SelectedCustomer.CustomerName);
                Customers.Clear();
                foreach (var customer in filteredCustomers)
                {
                    Customers.Add(customer);
                }
            }
            else
            {
                LoadCustomers();
            }
        }
    }
}
