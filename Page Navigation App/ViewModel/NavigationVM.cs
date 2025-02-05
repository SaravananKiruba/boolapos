using Page_Navigation_App.Services; // For CustomerService and other services
using Page_Navigation_App.Utilities; // For RelayCommand
using Page_Navigation_App.View; // For other view models like HomeVM, ProductVM
using System.Windows.Input;

namespace Page_Navigation_App.ViewModel
{
    public class NavigationVM : ViewModelBase
    {
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

        public ICommand HomeCommand { get; }
        public ICommand CustomersCommand { get; }
        public ICommand ProductsCommand { get; }
        public ICommand OrdersCommand { get; }
        public ICommand TransactionsCommand { get; }

        private readonly CustomerService _customerService;

        public NavigationVM(HomeVM homeVM, CustomerVM customerVM, ProductVM productVM, OrderVM orderVM, TransactionVM transactionVM,
                             CustomerService customerService)
        {
            _customerService = customerService;

            HomeCommand = new RelayCommand<object>(_ => NavigateTo(homeVM));
            CustomersCommand = new RelayCommand<object>(_ => NavigateTo(customerVM));
            ProductsCommand = new RelayCommand<object>(_ => NavigateTo(productVM));
            OrdersCommand = new RelayCommand<object>(_ => NavigateTo(orderVM));
            TransactionsCommand = new RelayCommand<object>(_ => NavigateTo(transactionVM));

            // Startup Page
            CurrentView = homeVM;
        }

        private void NavigateTo(object viewModel)
        {
            CurrentView = viewModel;
        }
    }
}
