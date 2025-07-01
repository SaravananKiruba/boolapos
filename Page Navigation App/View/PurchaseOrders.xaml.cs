using Microsoft.Extensions.DependencyInjection;
using Page_Navigation_App.ViewModel;
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
}
