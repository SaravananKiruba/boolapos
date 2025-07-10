using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Page_Navigation_App.Utilities
{
    /// <summary>
    /// Converter that returns Visible if the count is 0, Collapsed otherwise
    /// Used to show empty state messages when collections are empty
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Visibility.Visible;

            int count = 0;
            
            if (value is int intValue)
            {
                count = intValue;
            }
            else if (value is ICollection collection)
            {
                count = collection.Count;
            }
            else if (value is IEnumerable enumerable)
            {
                count = 0;
                foreach (var item in enumerable)
                {
                    count++;
                }
            }

            // Show empty state when count is 0
            return count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("CountToVisibilityConverter does not support ConvertBack");
        }
    }
}
