using Page_Navigation_App.Model;
using System;
using Page_Navigation_App.Utilities;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Page_Navigation_App.Services;
using System.Collections.Generic;

namespace Page_Navigation_App.ViewModel
{
    public class OrderVM : ViewModelBase
    {
        private readonly OrderService _orderService;

        public ICommand AddOrUpdateCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand SearchByDateCommand { get; }
        public ICommand SearchByCustomerCommand { get; }

        // Add a parameterless constructor to allow XAML instantiation
        public OrderVM() { }

        public OrderVM(OrderService orderService)
        {
            _orderService = orderService;
            LoadOrders();

            AddOrUpdateCommand = new RelayCommand<object>(_ => AddOrUpdateOrder(), _ => CanAddOrUpdateOrder());
            ClearCommand = new RelayCommand<object>(_ => ClearForm(), _ => true);
            SearchByDateCommand = new RelayCommand<object>(_ => SearchOrdersByDate(), _ => true);
            SearchByCustomerCommand = new RelayCommand<object>(_ => SearchOrdersByCustomer(), _ => true);
        }

        public ObservableCollection<Order> Orders { get; set; } = new ObservableCollection<Order>();

        private Order _selectedOrder = new Order();

        public Order SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                _selectedOrder = value;
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

        private int _customerId;
        public int CustomerId
        {
            get => _customerId;
            set
            {
                _customerId = value;
                OnPropertyChanged();
            }
        }

        private void LoadOrders()
        {
            Orders.Clear();
            foreach (var order in _orderService.GetAllOrders())
            {
                Orders.Add(order);
            }
        }

        private void AddOrUpdateOrder()
        {
            if (SelectedOrder.OrderID > 0)
            {
                _orderService.UpdateOrder(SelectedOrder);
            }
            else
            {
                _orderService.AddOrder(SelectedOrder);
            }

            LoadOrders();
            ClearForm();
        }

        private void ClearForm()
        {
            SelectedOrder = new Order();
            StartDate = DateTime.Now.AddMonths(-1);
            EndDate = DateTime.Now;
            CustomerId = 0;
        }

        private bool CanAddOrUpdateOrder()
        {
            return SelectedOrder.CustomerID > 0 && SelectedOrder.TotalAmount > 0;
        }

        private void SearchOrdersByDate()
        {
            Orders.Clear();
            var filteredOrders = _orderService.FilterOrdersByDate(StartDate, EndDate);
            foreach (var order in filteredOrders)
            {
                Orders.Add(order);
            }
        }

        private void SearchOrdersByCustomer()
        {
            Orders.Clear();
            var filteredOrders = _orderService.FilterOrdersByCustomer(CustomerId);
            foreach (var order in filteredOrders)
            {
                Orders.Add(order);
            }
        }
    }
}
