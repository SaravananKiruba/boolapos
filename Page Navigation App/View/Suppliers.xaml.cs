using System.Windows.Controls;
using Page_Navigation_App.Services;
using Page_Navigation_App.ViewModel;

namespace Page_Navigation_App.View
{
    public partial class Suppliers : UserControl
    {
        public Suppliers()
        {
            InitializeComponent();
        }

        public Suppliers(SupplierService supplierService)
        {
            InitializeComponent();
            DataContext = new SupplierVM(supplierService);
        }
    }
}