using System.Windows.Controls;
using Page_Navigation_App.Services;
using Page_Navigation_App.ViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace Page_Navigation_App.View
{
    /// <summary>
    /// Interaction logic for Stock.xaml
    /// </summary>    
    public partial class Stock : UserControl
    {
        public Stock()
        {
            InitializeComponent();
            
            // Create composite view model with both original and integration functionality
            var stockVM = App.ServiceProvider.GetRequiredService<StockVM>();
            var stockIntegrationVM = App.ServiceProvider.GetRequiredService<StockIntegrationVM>();
            
            // Set stockVM as primary DataContext
            DataContext = stockVM;
            
            // Set integration VM for the new stock addition section
            StockAdditionSection.DataContext = stockIntegrationVM;
            RecentPurchasesSection.DataContext = stockIntegrationVM;
        }

        public void CloseModalOnBackgroundClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is StockVM vm)
            {
                vm.IsPurchaseModalOpen = false;
            }
        }

        public void CloseStockItemsModalOnClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is StockVM vm)
            {
                vm.IsStockItemsVisible = false;
            }
        }
    }


    }
