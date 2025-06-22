using System;
using System.Windows.Controls;
using System.Windows;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Page_Navigation_App.ViewModel;

namespace Page_Navigation_App.View
{
    /// <summary>
    /// Interaction logic for PurchaseOrderView.xaml
    /// </summary>
    public partial class PurchaseOrderView : UserControl
    {
    public PurchaseOrderView()
    {
        try
        {
            // Note: InitializeComponent is generated at build time from XAML
            if (System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Runtime)
            {
                var type = this.GetType();
                var method = type.GetMethod("InitializeComponent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (method != null)
                {
                    method.Invoke(this, null);
                }
            }
            
            // DataContext is set via NavigationVM
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                // Design-time data context
                this.DataContext = new PurchaseOrderVM();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing PurchaseOrderView: {ex.Message}");
        }
    }
    }
}
