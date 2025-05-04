using System;
using System.Globalization;
using System.Windows.Data;

namespace Page_Navigation_App.Utilities
{
    /// <summary>
    /// Converts a boolean value to "Active" or "Inactive" string
    /// </summary>
    public class BooleanToActiveConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? "Active" : "Inactive";
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue)
            {
                return strValue.Equals("Active", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
    }
}