﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Page_Navigation_App.Model;
using Page_Navigation_App.Services;
using Page_Navigation_App.Utilities;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Globalization;

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
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public TransactionVM(FinanceService financeService, CustomerService customerService, OrderService orderService)
        {
            _financeService = financeService;
            _customerService = customerService;
            _orderService = orderService;
            
            InitializeCollections();
            LoadTransactions();
            LoadCustomers();
            LoadAllOrdersAsync(); // Load all orders initially to make them available
            
            // Initialize with proper defaults
            SelectedTransaction = new Finance
            {
                TransactionDate = DateTime.Now,
                TransactionType = "Income",
                PaymentMethod = "Cash",
                Currency = "INR",
                Category = "Sales",
                Status = "Completed"
            };
            TransactionType = "Income"; // Set the bound property as well

            AddOrUpdateCommand = new RelayCommand<object>(_ => AddOrUpdateTransaction(), _ => CanAddOrUpdateTransaction());
            ClearCommand = new RelayCommand<object>(_ => ClearForm(), _ => true);
            SearchByTypeCommand = new RelayCommand<object>(_ => SearchTransactionsByType(), _ => true);
            SearchByDateCommand = new RelayCommand<object>(_ => SearchTransactionsByDate(), _ => true);
            GenerateReportCommand = new RelayCommand<object>(_ => GenerateFinanceReport(), _ => true);
            EditCommand = new RelayCommand<Finance>(transaction => EditTransaction(transaction), _ => true);
            DeleteCommand = new RelayCommand<Finance>(transaction => DeleteTransaction(transaction), _ => true);
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
                if (value?.CustomerID > 0)
                {
                    LoadCustomerOrders(value.CustomerID.Value);
                    // Also set the selected customer
                    SelectedCustomer = Customers.FirstOrDefault(c => c.CustomerID == value.CustomerID.Value);
                }
                if (value?.TransactionType == "Income")
                {
                    // Set the transaction type to trigger order loading
                    TransactionType = "Income";
                    // Load all orders if no customer is selected
                    if (value.CustomerID == null || value.CustomerID == 0)
                    {
                        LoadAllOrdersAsync();
                    }
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
                    SelectedTransaction.CustomerID = value.CustomerID;
                    LoadCustomerOrders(value.CustomerID);
                }
                else
                {
                    // When no customer is selected, load all orders for income transactions
                    if (SelectedTransaction?.TransactionType == "Income")
                    {
                        LoadAllOrdersAsync();
                    }
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
                
                // Load all orders when transaction type is Income to allow selection
                if (value == "Income" && (SelectedCustomer == null || Orders.Count == 0))
                {
                    LoadAllOrdersAsync();
                }
                
                // Update the selected transaction if it exists
                if (SelectedTransaction != null)
                {
                    SelectedTransaction.TransactionType = value;
                }
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

        private void LoadTransactions()
        {
            try
            {
                Transactions.Clear();
                var transactions = _financeService.GetAllFinanceRecords();
                foreach (var transaction in transactions)
                {
                    Transactions.Add(transaction);
                }
                
                // Calculate summary
                CalculateFinanceSummary();
            }
            catch (Exception ex)
            {
                ShowMessageBox($"Error loading transactions: {ex.Message}");
            }
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
            try
            {
                Orders.Clear();
                var orders = await _orderService.GetCustomerOrders(customerId);
                foreach (var order in orders)
                {
                    Orders.Add(order);
                }
            }
            catch (Exception ex)
            {
                ShowMessageBox($"Error loading customer orders: {ex.Message}");
            }
        }

        private async void LoadAllOrdersAsync()
        {
            try
            {
                Orders.Clear();
                var orders = await _orderService.GetAllOrders();
                foreach (var order in orders.OrderByDescending(o => o.OrderDate))
                {
                    Orders.Add(order);
                }
            }
            catch (Exception ex)
            {
                ShowMessageBox($"Error loading orders: {ex.Message}");
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

        private void AddOrUpdateTransaction()
        {
            try
            {
                // Ensure the transaction type is set from the UI binding
                if (!string.IsNullOrEmpty(TransactionType))
                {
                    SelectedTransaction.TransactionType = TransactionType;
                }

                // Set default values if not provided
                SelectedTransaction.TransactionDate = SelectedTransaction.TransactionDate == default ? 
                    DateTime.Now : SelectedTransaction.TransactionDate;
                    
                SelectedTransaction.CreatedBy = string.IsNullOrEmpty(SelectedTransaction.CreatedBy) ? 
                    Environment.UserName : SelectedTransaction.CreatedBy;

                // Set currency to INR if not set
                if (string.IsNullOrEmpty(SelectedTransaction.Currency))
                {
                    SelectedTransaction.Currency = "INR";
                }

                // Set category based on transaction type if not provided
                if (string.IsNullOrEmpty(SelectedTransaction.Category))
                {
                    SelectedTransaction.Category = SelectedTransaction.TransactionType switch
                    {
                        "Income" => "Sales",
                        "Expense" => "Operational",
                        "Refund" => "Customer Service",
                        "Deposit" => "Banking",
                        "Withdrawal" => "Banking",
                        "Transfer" => "Banking",
                        _ => "General"
                    };
                }

                // Set customer and order references if selected
                if (SelectedCustomer != null)
                {
                    SelectedTransaction.CustomerID = SelectedCustomer.CustomerID;
                }

                if (SelectedOrder != null)
                {
                    SelectedTransaction.OrderID = SelectedOrder.OrderID;
                    if (string.IsNullOrEmpty(SelectedTransaction.ReferenceNumber))
                    {
                        SelectedTransaction.ReferenceNumber = SelectedOrder.OrderID.ToString();
                    }
                }

                // For expense transactions, set IsPaymentReceived to false (money going out)
                if (SelectedTransaction.TransactionType == "Expense" || 
                    SelectedTransaction.TransactionType == "Withdrawal" || 
                    SelectedTransaction.TransactionType == "Refund")
                {
                    SelectedTransaction.IsPaymentReceived = false;
                }
                else
                {
                    SelectedTransaction.IsPaymentReceived = true;
                }

                // Validate basic requirements
                if (SelectedTransaction.Amount <= 0)
                {
                    System.Windows.MessageBox.Show("Amount must be greater than zero.", "Validation Error", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrEmpty(SelectedTransaction.TransactionType))
                {
                    System.Windows.MessageBox.Show("Please select a transaction type.", "Validation Error", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrEmpty(SelectedTransaction.PaymentMethod))
                {
                    System.Windows.MessageBox.Show("Please select a payment method.", "Validation Error", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                // Set description from Notes if Description is empty but Notes is provided
                if (string.IsNullOrEmpty(SelectedTransaction.Description) && !string.IsNullOrEmpty(SelectedTransaction.Notes))
                {
                    SelectedTransaction.Description = SelectedTransaction.Notes;
                }

                // Ensure we have some description for the transaction
                if (string.IsNullOrEmpty(SelectedTransaction.Description))
                {
                    SelectedTransaction.Description = $"{SelectedTransaction.TransactionType} entry - ₹{SelectedTransaction.Amount:N2}";
                }

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

                bool result;
                // Check if this is an existing record by checking if it exists in the database
                bool isExistingRecord = false;
                if (!string.IsNullOrEmpty(SelectedTransaction.FinanceID) && 
                    SelectedTransaction.FinanceID != Guid.Empty.ToString())
                {
                    // Check if the record actually exists in the database
                    isExistingRecord = _financeService.FinanceRecordExists(SelectedTransaction.FinanceID);
                }

                if (isExistingRecord)
                {
                    result = _financeService.UpdateFinanceRecord(SelectedTransaction);
                }
                else
                {
                    // For new transactions, ensure we have a valid ID
                    if (string.IsNullOrEmpty(SelectedTransaction.FinanceID) || 
                        SelectedTransaction.FinanceID == Guid.Empty.ToString())
                    {
                        SelectedTransaction.FinanceID = Guid.NewGuid().ToString();
                    }
                    result = _financeService.AddFinanceRecord(SelectedTransaction);
                }

                if (result)
                {
                    LoadTransactions();
                    ClearForm();
                    
                    string transactionTypeText = SelectedTransaction.TransactionType == "Expense" ? "Expense" : "Transaction";
                    System.Windows.MessageBox.Show($"{transactionTypeText} saved successfully with amount ₹{SelectedTransaction.Amount:N2}", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show("Failed to save transaction. Please check the logs for details.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
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
                TransactionType = "Income", // Default to Income
                PaymentMethod = "Cash",
                Currency = "INR",
                Category = "Sales",
                Status = "Completed"
            };
            SelectedCustomer = null;
            SelectedOrder = null;
            TransactionType = "Income"; // Update the bound property as well
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

        private void SearchTransactionsByType()
        {
            if (string.IsNullOrEmpty(TransactionType))
            {
                LoadTransactions();
                return;
            }
            
            Transactions.Clear();
            var filteredTransactions = _financeService.GetTransactionsByType(TransactionType);
            foreach (var transaction in filteredTransactions)
            {
                Transactions.Add(transaction);
            }
            
            CalculateFinanceSummary();
        }

        private void SearchTransactionsByDate()
        {
            Transactions.Clear();
            var filteredTransactions = _financeService.GetTransactionsByDateRange(StartDate, EndDate);
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
                $"Total Income: ₹{TotalIncome:N2}\n" +
                $"Total Expenses: ₹{TotalExpense:N2}\n" +
                $"Net Balance: ₹{NetBalance:N2}",
                "Finance Report",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        public string FormattedTotalIncome => $"₹{TotalIncome:N2}";
        public string FormattedTotalExpense => $"₹{TotalExpense:N2}";
        public string FormattedNetBalance => $"₹{NetBalance:N2}";


        private void EditTransaction(Finance transaction)
        {
            if (transaction == null) return;

            // Set the selected transaction for editing
            SelectedTransaction = new Finance
            {
                FinanceID = transaction.FinanceID,
                TransactionDate = transaction.TransactionDate,
                TransactionType = transaction.TransactionType,
                Amount = transaction.Amount,
                PaymentMethod = transaction.PaymentMethod,
                PaymentMode = transaction.PaymentMode,
                Category = transaction.Category,
                Description = transaction.Description,
                ReferenceNumber = transaction.ReferenceNumber,
                CustomerID = transaction.CustomerID,
                OrderID = transaction.OrderID,
                IsPaymentReceived = transaction.IsPaymentReceived,
                Notes = transaction.Notes,
                Status = transaction.Status,
                Currency = transaction.Currency,
                CreatedBy = transaction.CreatedBy
            };

            // Set the transaction type property to match
            TransactionType = transaction.TransactionType;

            // Set the selected customer if available
            if (transaction.CustomerID.HasValue)
            {
                SelectedCustomer = Customers.FirstOrDefault(c => c.CustomerID == transaction.CustomerID.Value);
            }

            // Set the selected order if available
            if (transaction.OrderID.HasValue)
            {
                SelectedOrder = Orders.FirstOrDefault(o => o.OrderID == transaction.OrderID.Value);
            }
        }

        private void DeleteTransaction(Finance transaction)
        {
            if (transaction == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete this transaction?\n\nAmount: ₹{transaction.Amount:N2}\nType: {transaction.TransactionType}\nDate: {transaction.TransactionDate:dd-MMM-yyyy}",
                "Confirm Delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    bool success = _financeService.DeleteFinanceRecord(transaction.FinanceID);
                    
                    if (success)
                    {
                        // If this is the currently selected transaction, clear the form
                        if (SelectedTransaction?.FinanceID == transaction.FinanceID)
                        {
                            ClearForm();
                        }
                        
                        // Refresh the transaction list
                        LoadTransactions();
                        
                        System.Windows.MessageBox.Show(
                            "Transaction deleted successfully.",
                            "Success",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(
                            "Failed to delete transaction. Please try again.",
                            "Error",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Error deleting transaction: {ex.Message}", 
                        "Error", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Error);
                }
            }
        }

        // Helper method to safely show message boxes from async methods
        private void ShowMessageBox(string message, string title = "Error", System.Windows.MessageBoxButton button = System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage icon = System.Windows.MessageBoxImage.Error)
        {
            if (System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    System.Windows.MessageBox.Show(message, title, button, icon);
                }));
            }
        }
    }
}
