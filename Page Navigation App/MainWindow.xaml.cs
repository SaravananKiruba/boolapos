using Page_Navigation_App.Data;
using Page_Navigation_App.ViewModel;
using System.Windows;
using MahApps.Metro.Controls;

namespace Page_Navigation_App
{
    public partial class MainWindow : MetroWindow
    {
        private readonly NavigationVM _navigationVM;
        private readonly AppDbContext _Context;

        public MainWindow(AppDbContext dbContext, NavigationVM navigationVM)
        {
            InitializeComponent();

            _navigationVM = navigationVM;
            _Context = dbContext;

            // Bind the DataContext to NavigationVM
            DataContext = _navigationVM;
        }

        private void CloseApp_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}