using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Page_Navigation_App.ViewModel;

namespace Page_Navigation_App.View
{
    /// <summary>
    /// Interaction logic for Orders.xaml
    /// </summary>
    public partial class Orders : UserControl
    {
        public Orders()
        {
            InitializeComponent();
        }
        
        private void ReloadData_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is OrderVM viewModel)
            {
                viewModel.ReloadData();
            }
        }
    }
}
