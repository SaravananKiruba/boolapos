using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace Page_Navigation_App.Utilities
{
    public static class CurrencyFormatting
    {
        private static readonly NumberFormatInfo indianNumberFormat = new NumberFormatInfo
        {
            CurrencySymbol = "₹",
            CurrencyDecimalDigits = 2,
            CurrencyDecimalSeparator = ".",
            CurrencyGroupSeparator = ",",
            CurrencyGroupSizes = new int[] { 3, 2 } // Indian number format (e.g., ₹1,00,000.00)
        };

        public static string FormatAsINR(decimal amount)
        {
            return string.Format(indianNumberFormat, "{0:C}", amount);
        }

        public static string FormatAsINRWithoutSymbol(decimal amount)
        {
            return string.Format(indianNumberFormat, "{0:N}", amount);
        }

        public static string GetCurrencySymbol()
        {
            return "₹";
        }
    }
}
