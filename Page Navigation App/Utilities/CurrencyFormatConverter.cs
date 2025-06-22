using System;
using System.Globalization;
using System.Windows.Data;

namespace Page_Navigation_App.Utilities
{
    public class CurrencyFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "₹0.00";

            if (value is decimal decimalValue)
            {
                return CurrencyFormatting.FormatAsINR(decimalValue);
            }
            else if (decimal.TryParse(value.ToString(), out decimal parsedValue))
            {
                return CurrencyFormatting.FormatAsINR(parsedValue);
            }

            return "₹0.00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !(value is string stringValue))
                return 0m;

            // Remove currency symbol and any formatting characters
            stringValue = stringValue.Replace("₹", "").Replace(",", "").Trim();

            if (decimal.TryParse(stringValue, out decimal result))
                return result;

            return 0m;
        }
    }
}
