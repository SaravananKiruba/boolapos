using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Page_Navigation_App.Utilities
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // If parameter is provided and is "Reverse", we invert the logic
                bool reverse = parameter != null && parameter.ToString().Equals("Reverse", StringComparison.OrdinalIgnoreCase);
                
                if (reverse)
                {
                    return boolValue ? Visibility.Collapsed : Visibility.Visible;
                }
                else
                {
                    return boolValue ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}