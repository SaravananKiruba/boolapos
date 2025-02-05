using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using Page_Navigation_App.Utilities;

namespace Page_Navigation_App.ViewModel
{
    public class CustomerVM : ViewModelBase
    {
        private readonly AppDbContext _dbContext;

        public ICommand AddCommand { get; }
        public ICommand UpdateCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand FilterCommand { get; }

        public CustomerVM(AppDbContext dbContext)
        {
            _dbContext = dbContext;
            LoadCustomers();

            AddCommand = new RelayCommand<object>(_ => AddCustomer(), _ => SelectedCustomer != null);
            UpdateCommand = new RelayCommand<object>(_ => UpdateCustomer(), _ => SelectedCustomer != null);
            DeleteCommand = new RelayCommand<object>(_ => DeleteCustomer(), _ => SelectedCustomer != null);
            FilterCommand = new RelayCommand<object>(_ => FilterCustomer(), _ => true);
        }

        public ObservableCollection<Customer> Customers { get; set; }

        private Customer _selectedCustomer;
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
            Customers = new ObservableCollection<Customer>(_dbContext.Customers.ToList());
        }

        private void AddCustomer()
        {
            if (SelectedCustomer != null)
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

                _dbContext.Customers.Add(newCustomer);
                _dbContext.SaveChanges();
                Customers.Add(newCustomer);
            }
        }

        private void UpdateCustomer()
        {
            if (SelectedCustomer != null)
            {
                var customerInDb = _dbContext.Customers.FirstOrDefault(c => c.CustomerID == SelectedCustomer.CustomerID);
                if (customerInDb != null)
                {
                    customerInDb.CustomerName = SelectedCustomer.CustomerName;
                    customerInDb.PhoneNumber = SelectedCustomer.PhoneNumber;
                    customerInDb.ContactPerson = SelectedCustomer.ContactPerson;
                    customerInDb.Address = SelectedCustomer.Address;
                    customerInDb.Email = SelectedCustomer.Email;
                    customerInDb.WhatsAppNumber = SelectedCustomer.WhatsAppNumber;

                    _dbContext.SaveChanges();
                    LoadCustomers();
                }
            }
        }

        private void DeleteCustomer()
        {
            if (SelectedCustomer != null)
            {
                _dbContext.Customers.Remove(SelectedCustomer);
                _dbContext.SaveChanges();
                Customers.Remove(SelectedCustomer);
            }
        }

        private void FilterCustomer()
        {
            if (SelectedCustomer != null && !string.IsNullOrWhiteSpace(SelectedCustomer.CustomerName))
            {
                var filteredList = _dbContext.Customers
                    .Where(c => EF.Functions.Like(c.CustomerName, $"%{SelectedCustomer.CustomerName}%"))
                    .ToList();

                Customers.Clear();
                foreach (var customer in filteredList)
                    Customers.Add(customer);
            }
            else
            {
                LoadCustomers();
            }
        }
    }
}
