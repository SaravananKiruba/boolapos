using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Page_Navigation_App.Utilities
{
    public class NegativeToRedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
            {
                return decimalValue < 0 ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Black);
            }
            
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
