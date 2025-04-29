using System.Windows.Input;
using Page_Navigation_App.Utilities;
using Page_Navigation_App.Model;
using Page_Navigation_App.Services;

namespace Page_Navigation_App.ViewModel
{
    public class NavigationVM : ViewModelBase
    {
        private readonly UserService _userService;
        private object _currentView;
        
        public object CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand HomeCommand { get; set; }
        public ICommand CustomersCommand { get; set; }
        public ICommand ProductsCommand { get; set; }
        public ICommand OrdersCommand { get; set; }
        public ICommand TransactionsCommand { get; set; }
        public ICommand SuppliersCommand { get; set; }
        public ICommand RateMasterCommand { get; set; }
        public ICommand RepairJobsCommand { get; set; }
        public ICommand StockCommand { get; set; }
        public ICommand CategoryCommand { get; set; }
        public ICommand UserCommand { get; set; }
        public ICommand ReportCommand { get; set; }

        public NavigationVM(
            HomeVM homeVM,
            CustomerVM customerVM,
            ProductVM productVM,
            OrderVM orderVM,
            TransactionVM transactionVM,
            SupplierVM supplierVM,
            RateMasterVM rateMasterVM,
            RepairJobVM repairJobVM,
            StockVM stockVM,
            CategoryVM categoryVM,
            UserVM userVM,
            ReportVM reportVM,
            UserService userService)
        {
            _userService = userService;

            // Initialize commands
            HomeCommand = new RelayCommand<object>(_ => NavigateTo(homeVM));
            CustomersCommand = new RelayCommand<object>(_ => NavigateTo(customerVM));
            ProductsCommand = new RelayCommand<object>(_ => NavigateTo(productVM));
            OrdersCommand = new RelayCommand<object>(_ => NavigateTo(orderVM));
            TransactionsCommand = new RelayCommand<object>(_ => NavigateTo(transactionVM));
            SuppliersCommand = new RelayCommand<object>(_ => NavigateTo(supplierVM));
            RateMasterCommand = new RelayCommand<object>(_ => NavigateTo(rateMasterVM));
            RepairJobsCommand = new RelayCommand<object>(_ => NavigateTo(repairJobVM));
            StockCommand = new RelayCommand<object>(_ => NavigateTo(stockVM));
            CategoryCommand = new RelayCommand<object>(_ => NavigateTo(categoryVM));
            UserCommand = new RelayCommand<object>(_ => NavigateTo(userVM), _ => CanAccessUserManagement());
            ReportCommand = new RelayCommand<object>(_ => NavigateTo(reportVM));

            // Set default view
            CurrentView = homeVM;
        }

        private void NavigateTo(object view)
        {
            CurrentView = view;
        }

        private bool CanAccessUserManagement()
        {
            // TODO: Implement role-based access control
            return true;
        }
    }
}
