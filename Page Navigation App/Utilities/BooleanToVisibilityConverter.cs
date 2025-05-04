using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Page_Navigation_App.Utilities
{
    /// <summary>
    /// Converts a boolean value to Visible or Collapsed based on the boolean value
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        // Cached singleton instance for better performance
        private static BooleanToVisibilityConverter _instance;
        public static BooleanToVisibilityConverter Instance => _instance ??= new BooleanToVisibilityConverter();

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
            if (value is Visibility visibility)
            {
                bool reverse = parameter != null && parameter.ToString().Equals("Reverse", StringComparison.OrdinalIgnoreCase);
                bool isVisible = visibility == Visibility.Visible;
                
                return reverse ? !isVisible : isVisible;
            }
            
            return false;
        }
    }
}