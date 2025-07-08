using System;
using System.Globalization;
using System.Windows.Data;

namespace Page_Navigation_App.Utilities
{
    /// <summary>
    /// Converts a boolean value to a text string based on provided parameters
    /// </summary>
    public class BoolToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                string paramString = parameter as string;
                if (!string.IsNullOrEmpty(paramString))
                {
                    string[] textOptions = paramString.Split('|');
                    if (textOptions.Length >= 2)
                    {
                        return boolValue ? textOptions[0] : textOptions[1];
                    }
                }
                
                // Default values if no parameter provided
                return boolValue ? "Yes" : "No";
            }
            
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
