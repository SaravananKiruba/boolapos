using System.Windows;
using MahApps.Metro.Controls;
using Page_Navigation_App.ViewModel;

namespace Page_Navigation_App.View
{
    public partial class LoginWindow : MetroWindow
    {
        public LoginWindow(LoginViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}