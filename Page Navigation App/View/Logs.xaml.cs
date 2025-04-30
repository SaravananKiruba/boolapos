using System.Windows.Controls;
using Page_Navigation_App.ViewModel;

namespace Page_Navigation_App.View
{
    public partial class Logs : UserControl
    {
        public Logs()
        {
            InitializeComponent();
            DataContext = new LogsVM();
        }
    }
}