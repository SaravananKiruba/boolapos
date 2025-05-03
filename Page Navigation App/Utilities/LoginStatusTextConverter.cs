using System;
using System.Globalization;
using System.Windows.Data;

namespace Page_Navigation_App.Utilities
{
    public class LoginStatusTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isLoggingIn)
            {
                string defaultText = parameter as string ?? "Please enter your credentials";
                return isLoggingIn ? "Logging in... please wait" : defaultText;
            }
            return parameter ?? "Please enter your credentials";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}