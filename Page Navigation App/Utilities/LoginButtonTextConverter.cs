using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Page_Navigation_App.Utilities
{
    public class LoginButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isLoggingIn && parameter is string defaultText)
            {
                return isLoggingIn ? "LOGGING IN..." : defaultText;
            }
            return parameter ?? "LOGIN";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}