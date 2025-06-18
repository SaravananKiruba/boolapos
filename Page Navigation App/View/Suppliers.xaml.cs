using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Page_Navigation_App.Services;
using Page_Navigation_App.ViewModel;

namespace Page_Navigation_App.View
{
    public partial class Suppliers : UserControl
    {
        public Suppliers()
        {
            InitializeComponent();
            
            // Get the SupplierService from the service provider if not already set
            if (DataContext == null)
            {
                var supplierService = App.ServiceProvider.GetRequiredService<SupplierService>();
                DataContext = new SupplierVM(supplierService);
            }
        }
    }
}