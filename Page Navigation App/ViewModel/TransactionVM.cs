using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Page_Navigation_App.Model;
using Page_Navigation_App.Services;
using Page_Navigation_App.Utilities;

namespace Page_Navigation_App.ViewModel
{
    public class TransactionVM : ViewModelBase
    {
        private readonly FinanceService _financeService;

        public ICommand AddOrUpdateCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand SearchByTypeCommand { get; }
        public ICommand SearchByDateCommand { get; }

        public TransactionVM(FinanceService financeService)
        {
            _financeService = financeService;
            LoadTransactions();

            AddOrUpdateCommand = new RelayCommand<object>(_ => AddOrUpdateTransaction(), _ => CanAddOrUpdateTransaction());
            ClearCommand = new RelayCommand<object>(_ => ClearForm(), _ => true);
            SearchByTypeCommand = new RelayCommand<object>(_ => SearchTransactionsByType(), _ => true);
            SearchByDateCommand = new RelayCommand<object>(_ => SearchTransactionsByDate(), _ => true);
        }

        public ObservableCollection<Finance> Transactions { get; set; } = new ObservableCollection<Finance>();

        private Finance _selectedTransaction = new Finance();

        public Finance SelectedTransaction
        {
            get => _selectedTransaction;
            set
            {
                _selectedTransaction = value;
                OnPropertyChanged();
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

        private void LoadTransactions()
        {
            Transactions.Clear();
            foreach (var transaction in _financeService.GetAllTransactions())
            {
                Transactions.Add(transaction);
            }
        }

        private void AddOrUpdateTransaction()
        {
            if (SelectedTransaction.FinanceID > 0)
            {
                _financeService.UpdateTransaction(SelectedTransaction);
            }
            else
            {
                _financeService.AddTransaction(SelectedTransaction);
            }

            LoadTransactions();
            ClearForm();
        }

        private void ClearForm()
        {
            SelectedTransaction = new Finance();
            TransactionType = string.Empty;
            StartDate = DateTime.Now.AddMonths(-1);
            EndDate = DateTime.Now;
        }

        private bool CanAddOrUpdateTransaction()
        {
            return SelectedTransaction.Amount > 0 && !string.IsNullOrEmpty(SelectedTransaction.TransactionType);
        }

        private void SearchTransactionsByType()
        {
            Transactions.Clear();
            var filteredTransactions = _financeService.FilterTransactionsByType(TransactionType);
            foreach (var transaction in filteredTransactions)
            {
                Transactions.Add(transaction);
            }
        }

        private void SearchTransactionsByDate()
        {
            Transactions.Clear();
            var filteredTransactions = _financeService.FilterTransactionsByDate(StartDate, EndDate);
            foreach (var transaction in filteredTransactions)
            {
                Transactions.Add(transaction);
            }
        }
    }
}
