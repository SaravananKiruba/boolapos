using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Page_Navigation_App.Utilities
{
    /// <summary>
    /// Converts a boolean value to a brush color for status indication
    /// </summary>
    public class BooleanToStatusBrushConverter : IValueConverter
    {
        // Static brushes for better performance (reuse instead of creating new ones each time)
        private static readonly SolidColorBrush _activeBrush = new SolidColorBrush(Color.FromRgb(62, 108, 63)); // Dark Green (#3e6c3f)
        private static readonly SolidColorBrush _inactiveBrush = new SolidColorBrush(Color.FromRgb(4, 98, 194)); // Primary Blue (#0462c2)
        private static readonly SolidColorBrush _neutralBrush = new SolidColorBrush(Color.FromRgb(106, 178, 242)); // Light Blue (#6ab2f2)

        static BooleanToStatusBrushConverter()
        {
            // Freeze brushes for better performance
            _activeBrush.Freeze();
            _inactiveBrush.Freeze();
            _neutralBrush.Freeze();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool status)
            {
                // If a parameter is provided, it might indicate a need to reverse the logic
                bool reverse = parameter is string strParam && 
                               string.Equals(strParam, "Reverse", StringComparison.OrdinalIgnoreCase);
                
                // Return appropriate frozen brush based on boolean state
                return (reverse ? !status : status) ? _activeBrush : _inactiveBrush;
            }
            
            // Return neutral gray for null or non-boolean values
            return _neutralBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("BooleanToStatusBrushConverter is a one-way converter");
        }
    }
}