using Microsoft.Extensions.DependencyInjection;
using Page_Navigation_App.ViewModel;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Page_Navigation_App.View
{
    public partial class Stock : UserControl
    {
        public Stock()
        {
            InitializeComponent();
            this.DataContext = App.ServiceProvider.GetRequiredService<StockVM>();
        }

        private void CreatePurchaseOrder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int productId)
            {
                // Navigate to Purchase Orders tab and pre-fill with this product
                // This could be implemented based on your navigation system
                MessageBox.Show($"Create Purchase Order for Product ID: {productId}", "Create PO", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
