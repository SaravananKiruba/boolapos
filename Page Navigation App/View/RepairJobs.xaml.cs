using System.Windows.Controls;
using Page_Navigation_App.Services;
using Page_Navigation_App.ViewModel;

namespace Page_Navigation_App.View
{
    public partial class RepairJobs : UserControl
    {
        public RepairJobs()
        {
            InitializeComponent();
        }

        public RepairJobs(RepairJobService repairService, CustomerService customerService)
        {
            InitializeComponent();
            DataContext = new RepairJobVM(repairService, customerService);
        }
    }
}