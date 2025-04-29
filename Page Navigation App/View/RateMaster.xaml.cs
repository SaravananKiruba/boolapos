using System.Windows.Controls;
using Page_Navigation_App.Services;
using Page_Navigation_App.ViewModel;

namespace Page_Navigation_App.View
{
    public partial class RateMaster : UserControl
    {
        public RateMaster()
        {
            InitializeComponent();
        }

        public RateMaster(RateMasterService rateService)
        {
            InitializeComponent();
            DataContext = new RateMasterVM(rateService);
        }
    }
}