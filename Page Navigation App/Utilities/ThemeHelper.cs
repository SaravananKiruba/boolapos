using ControlzEx.Theming;
using System.Windows;

namespace Page_Navigation_App.Utilities
{
    public static class ThemeHelper
    {
        public static void ApplyTheme()
        {
            // Set MahApps.Metro theme
            ThemeManager.Current.ChangeTheme(Application.Current, "Light.Blue");
        }
        
        public static void ApplyDarkTheme()
        {
            // Set MahApps.Metro dark theme
            ThemeManager.Current.ChangeTheme(Application.Current, "Dark.Blue");
        }
        
        public static void ApplyLightTheme() 
        {
            // Set MahApps.Metro light theme
            ThemeManager.Current.ChangeTheme(Application.Current, "Light.Blue");
        }
        
        public static void ApplyAccentColor(string accentColor)
        {
            // Get current theme (light/dark)
            var currentTheme = ThemeManager.Current.DetectTheme(Application.Current);
            string baseTheme = currentTheme.BaseColorScheme;
            
            // Apply the new accent color with the existing base theme
            ThemeManager.Current.ChangeTheme(Application.Current, $"{baseTheme}.{accentColor}");
        }
    }
}
