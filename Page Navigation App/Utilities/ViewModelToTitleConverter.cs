using System;
using System.Globalization;
using System.Windows.Data;
using Page_Navigation_App.ViewModel;

namespace Page_Navigation_App.Utilities
{
    public class ViewModelToTitleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;

            // Convert ViewModel type to display name
            return value switch
            {
                HomeVM => "Home",
                CustomerVM => "Customers",
                ProductVM => "Products",
                OrderVM => "Orders",
                TransactionVM => "Transactions",
                SupplierVM => "Suppliers",
                RateMasterVM => "Rate Master",
                RepairJobVM => "Repair Jobs",
                StockVM => "Stock",
                CategoryVM => "Categories",
                UserVM => "Users",
                ReportVM => "Reports",
                SettingsVM => "Settings",
                _ => value.GetType().Name.Replace("VM", "")
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}