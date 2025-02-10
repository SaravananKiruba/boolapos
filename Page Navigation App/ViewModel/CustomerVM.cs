using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Page_Navigation_App.Model;
using Page_Navigation_App.Utilities;
using Page_Navigation_App.Services;

namespace Page_Navigation_App.ViewModel
{
    public class CustomerVM : ViewModelBase
    {
        private readonly CustomerService _customerService;

        public ICommand AddOrUpdateCommand { get; }

        public CustomerVM(CustomerService customerService)
        {
            _customerService = customerService;
            LoadCustomers();

            AddOrUpdateCommand = new RelayCommand<object>(_ => AddOrUpdateCustomer(), _ => true);
        }

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

        private void LoadCustomers()
        {
            Customers.Clear();
            foreach (var customer in _customerService.GetAllCustomers())
            {
                Customers.Add(customer);
            }
        }

        private void AutoSelectCustomer()
        {
            var matchedCustomer = Customers.FirstOrDefault(c => c.CustomerName == SearchName || c.PhoneNumber == SearchPhone);
            if (matchedCustomer != null)
            {
                SelectedCustomer = matchedCustomer;
            }
            else
            {
                SelectedCustomer = new Customer { CustomerName = SearchName, PhoneNumber = SearchPhone };
            }
        }

        private void AddOrUpdateCustomer()
        {
            if (SelectedCustomer.CustomerID > 0)
            {
                _customerService.UpdateCustomer(SelectedCustomer);
            }
            else
            {
                _customerService.AddCustomer(SelectedCustomer);
            }
            LoadCustomers();
        }
    }
}
