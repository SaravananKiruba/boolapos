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
    }
}