using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Page_Navigation_App.Utilities
{
    /// <summary>
    /// Converts a boolean value to a color brush based on provided parameters
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                string paramString = parameter as string;
                if (!string.IsNullOrEmpty(paramString))
                {
                    string[] colors = paramString.Split('|');
                    if (colors.Length >= 2)
                    {
                        string colorValue = boolValue ? colors[0] : colors[1];
                        
                        // If returning a Brush
                        if (targetType == typeof(Brush))
                        {
                            return new BrushConverter().ConvertFromString(colorValue);
                        }
                        
                        // If returning a string (color value)
                        return colorValue;
                    }
                }
                
                // Default values if no parameter provided
                if (targetType == typeof(Brush))
                {
                    return new BrushConverter().ConvertFromString(boolValue ? "#4CAF50" : "#F44336");
                }
                return boolValue ? "#4CAF50" : "#F44336";
            }
            
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
