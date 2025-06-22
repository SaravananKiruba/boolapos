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
            DataContext = App.ServiceProvider.GetRequiredService<StockVM>();
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
