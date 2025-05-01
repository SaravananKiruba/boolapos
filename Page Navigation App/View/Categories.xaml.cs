using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Page_Navigation_App.ViewModel;

namespace Page_Navigation_App.View
{
    public partial class Categories : UserControl
    {
        public Categories()
        {
            InitializeComponent();
            DataContext = App.ServiceProvider.GetRequiredService<CategoryVM>();
        }
    }
}