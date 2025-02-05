using Page_Navigation_App.Data; // For AppDbContext
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

        private readonly AppDbContext _dbContext;

        public NavigationVM(AppDbContext dbContext, HomeVM homeVM, Customers customers, ProductVM productVM, OrderVM orderVM, TransactionVM transactionVM)
        {
            _dbContext = dbContext;

            HomeCommand = new RelayCommand<object>(_ => NavigateTo(homeVM));
            CustomersCommand = new RelayCommand<object>(_ => NavigateTo(customers));
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
