using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Page_Navigation_App.Model;
using Page_Navigation_App.Services;
using Page_Navigation_App.Utilities;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Page_Navigation_App.ViewModel
{
    public class TransactionVM : ViewModelBase
    {
        private readonly FinanceService _financeService;
        private readonly CustomerService _customerService;
        private readonly OrderService _orderService;

        public ICommand AddOrUpdateCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand SearchByTypeCommand { get; }
        public ICommand SearchByDateCommand { get; }
        public ICommand GenerateReportCommand { get; }

        public TransactionVM(FinanceService financeService, CustomerService customerService, OrderService orderService)
        {
            _financeService = financeService;
            _customerService = customerService;
            _orderService = orderService;
            
            LoadTransactions();
            LoadCustomers();
            InitializeCollections();

            AddOrUpdateCommand = new RelayCommand<object>(_ => AddOrUpdateTransaction(), _ => CanAddOrUpdateTransaction());
            ClearCommand = new RelayCommand<object>(_ => ClearForm(), _ => true);
            SearchByTypeCommand = new RelayCommand<object>(_ => SearchTransactionsByType(), _ => true);
            SearchByDateCommand = new RelayCommand<object>(_ => SearchTransactionsByDate(), _ => true);
            GenerateReportCommand = new RelayCommand<object>(_ => GenerateFinanceReport(), _ => true);
        }

        public ObservableCollection<Finance> Transactions { get; set; } = new ObservableCollection<Finance>();
        public ObservableCollection<Customer> Customers { get; set; } = new ObservableCollection<Customer>();
        public ObservableCollection<Order> Orders { get; set; } = new ObservableCollection<Order>();
        public ObservableCollection<string> TransactionTypes { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> PaymentMethods { get; set; } = new ObservableCollection<string>();

        private Finance _selectedTransaction = new Finance();
        public Finance SelectedTransaction
        {
            get => _selectedTransaction;
            set
            {
                _selectedTransaction = value;
                OnPropertyChanged();
                if (value?.CustomerId > 0)
                {
                    LoadCustomerOrders(value.CustomerId.Value);
                }
            }
        }

        private Customer _selectedCustomer;
        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                _selectedCustomer = value;
                OnPropertyChanged();
                if (value != null)
                {
                    SelectedTransaction.CustomerId = value.CustomerID;
                    LoadCustomerOrders(value.CustomerID);
                }
            }
        }

        private Order _selectedOrder;
        public Order SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                _selectedOrder = value;
                OnPropertyChanged();
                if (value != null)
                {
                    SelectedTransaction.OrderID = value.OrderID;
                    // If this is a new transaction for an order, pre-fill amount
                    if (string.IsNullOrEmpty(SelectedTransaction.FinanceID))
                    {
                        SelectedTransaction.Amount = value.GrandTotal;
                        SelectedTransaction.ReferenceNumber = value.OrderID.ToString();
                        SelectedTransaction.Description = $"Payment for Order #{value.OrderID}";
                    }
                }
            }
        }

        private string _transactionType;
        public string TransactionType
        {
            get => _transactionType;
            set
            {
                _transactionType = value;
                OnPropertyChanged();
            }
        }

        private DateTime _startDate = DateTime.Now.AddMonths(-1);
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                OnPropertyChanged();
            }
        }

        private DateTime _endDate = DateTime.Now;
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                OnPropertyChanged();
            }
        }

        private decimal _totalIncome;
        public decimal TotalIncome
        {
            get => _totalIncome;
            set
            {
                _totalIncome = value;
                OnPropertyChanged();
            }
        }

        private decimal _totalExpense;
        public decimal TotalExpense
        {
            get => _totalExpense;
            set
            {
                _totalExpense = value;
                OnPropertyChanged();
            }
        }

        private decimal _netBalance;
        public decimal NetBalance
        {
            get => _netBalance;
            set
            {
                _netBalance = value;
                OnPropertyChanged();
            }
        }

        private void InitializeCollections()
        {
            TransactionTypes.Clear();
            PaymentMethods.Clear();

            // Add transaction types
            TransactionTypes.Add("Income");
            TransactionTypes.Add("Expense");
            TransactionTypes.Add("Refund");
            TransactionTypes.Add("Deposit");
            TransactionTypes.Add("Withdrawal");
            TransactionTypes.Add("Transfer");

            // Add payment methods
            PaymentMethods.Add("Cash");
            PaymentMethods.Add("Card");
            PaymentMethods.Add("UPI");
            PaymentMethods.Add("Bank Transfer");
            PaymentMethods.Add("Credit");
            PaymentMethods.Add("EMI");
            PaymentMethods.Add("Check");
        }

        private async void LoadTransactions()
        {
            Transactions.Clear();
            var transactions = await _financeService.GetAllFinanceRecords();
            foreach (var transaction in transactions)
            {
                Transactions.Add(transaction);
            }
            
            // Calculate summary
            CalculateFinanceSummary();
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

        private async void LoadCustomerOrders(int customerId)
        {
            Orders.Clear();
            var orders = await _orderService.GetCustomerOrders(customerId);
            foreach (var order in orders)
            {
                Orders.Add(order);
            }
        }

        private void CalculateFinanceSummary()
        {
            TotalIncome = Transactions
                .Where(t => t.TransactionType == "Income" || t.TransactionType == "Deposit")
                .Sum(t => t.Amount);
                
            TotalExpense = Transactions
                .Where(t => t.TransactionType == "Expense" || t.TransactionType == "Withdrawal" || t.TransactionType == "Refund")
                .Sum(t => t.Amount);
                
            NetBalance = TotalIncome - TotalExpense;
        }

        private async void AddOrUpdateTransaction()
        {
            // Set default values if not provided
            SelectedTransaction.TransactionDate = SelectedTransaction.TransactionDate == default ? 
                DateTime.Now : SelectedTransaction.TransactionDate;
                
            SelectedTransaction.CreatedBy = string.IsNullOrEmpty(SelectedTransaction.CreatedBy) ? 
                Environment.UserName : SelectedTransaction.CreatedBy;

            // Validate
            var validationContext = new ValidationContext(SelectedTransaction, null, null);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(SelectedTransaction, validationContext, validationResults, true);

            if (!isValid)
            {
                string errorMessage = string.Join("\n", validationResults.Select(v => v.ErrorMessage));
                System.Windows.MessageBox.Show($"Validation Errors:\n{errorMessage}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            try
            {
                if (!string.IsNullOrEmpty(SelectedTransaction.FinanceID))
                {
                    await _financeService.UpdateFinanceRecord(SelectedTransaction);
                }
                else
                {
                    await _financeService.AddFinanceRecord(SelectedTransaction);
                }

                LoadTransactions();
                ClearForm();
                
                System.Windows.MessageBox.Show("Transaction saved successfully", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving transaction: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ClearForm()
        {
            SelectedTransaction = new Finance
            {
                TransactionDate = DateTime.Now,
                TransactionType = TransactionTypes.First()
            };
            SelectedCustomer = null;
            SelectedOrder = null;
            TransactionType = string.Empty;
            StartDate = DateTime.Now.AddMonths(-1);
            EndDate = DateTime.Now;
        }

        private bool CanAddOrUpdateTransaction()
        {
            return SelectedTransaction != null && 
                   SelectedTransaction.Amount > 0 && 
                   !string.IsNullOrEmpty(SelectedTransaction.TransactionType) &&
                   !string.IsNullOrEmpty(SelectedTransaction.PaymentMethod);
        }

        private async void SearchTransactionsByType()
        {
            if (string.IsNullOrEmpty(TransactionType))
            {
                LoadTransactions();
                return;
            }
            
            Transactions.Clear();
            var filteredTransactions = await _financeService.GetTransactionsByType(TransactionType);
            foreach (var transaction in filteredTransactions)
            {
                Transactions.Add(transaction);
            }
            
            CalculateFinanceSummary();
        }

        private async void SearchTransactionsByDate()
        {
            Transactions.Clear();
            var filteredTransactions = await _financeService.GetTransactionsByDateRange(StartDate, EndDate);
            foreach (var transaction in filteredTransactions)
            {
                Transactions.Add(transaction);
            }
            
            CalculateFinanceSummary();
        }
        
        private bool ValidateTransaction()
        {
            if (SelectedTransaction == null) return false;
            return SelectedTransaction.Amount > 0 && 
                   !string.IsNullOrEmpty(SelectedTransaction.TransactionType) &&
                   !string.IsNullOrEmpty(SelectedTransaction.PaymentMethod);
        }
        
        private void GenerateFinanceReport()
        {
            System.Windows.MessageBox.Show(
                $"Financial Summary:\n\n" +
                $"Period: {StartDate:d} to {EndDate:d}\n" +
                $"Total Income: {TotalIncome:C}\n" +
                $"Total Expenses: {TotalExpense:C}\n" +
                $"Net Balance: {NetBalance:C}",
                "Finance Report",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
    }
}
