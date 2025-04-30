using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Page_Navigation_App.Services;
using Page_Navigation_App.ViewModel;

namespace Page_Navigation_App.View
{
    public partial class Settings : UserControl
    {
        public Settings()
        {
            InitializeComponent();
            DataContext = App.ServiceProvider.GetRequiredService<SettingsVM>();
        }
    }
}