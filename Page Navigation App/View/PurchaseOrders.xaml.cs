using Microsoft.Extensions.DependencyInjection;
using Page_Navigation_App.ViewModel;
using Page_Navigation_App.Utilities;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Page_Navigation_App.View
{
    public partial class PurchaseOrders : UserControl
    {
        public PurchaseOrders()
        {
            InitializeComponent();
            this.DataContext = App.ServiceProvider.GetRequiredService<PurchaseOrderVM>();
        }
    }

    // Converter for DateOnly to DateTime for DatePicker
    public class DateOnlyConverter : IValueConverter
    {
        public static readonly DateOnlyConverter Instance = new DateOnlyConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateOnly dateOnly)
            {
                return dateOnly.ToDateTime(TimeOnly.MinValue);
            }
            return DateTime.Now;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                return DateOnly.FromDateTime(dateTime);
            }
            return DateOnly.FromDateTime(DateTime.Now);
        }
    }

    // Converter for INR currency formatting
    public class INRCurrencyConverter : IValueConverter
    {
        public static readonly INRCurrencyConverter Instance = new INRCurrencyConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal amount)
            {
                return CurrencyFormatting.FormatAsINR(amount);
            }
            return "₹0.00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text && decimal.TryParse(text.Replace("₹", "").Replace(",", ""), out decimal result))
            {
                return result;
            }
            return 0m;
        }
    }
}
