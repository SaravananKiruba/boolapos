using System.Windows.Controls;
using Page_Navigation_App.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Page_Navigation_App.Services;

namespace Page_Navigation_App.View
{
    public partial class Users : UserControl
    {
        private readonly UserService _userService;
        private readonly SecurityService _securityService;

        public Users()
        {
            InitializeComponent();
            _userService = App.ServiceProvider.GetRequiredService<UserService>();
            _securityService = App.ServiceProvider.GetRequiredService<SecurityService>();
            DataContext = new UserVM(_userService);
        }
    }
}