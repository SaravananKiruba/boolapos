using System.Windows.Controls;
using Page_Navigation_App.ViewModel;
using Page_Navigation_App.Services; // For CustomerService

namespace Page_Navigation_App.View
{
    public partial class Customers : UserControl
    {
        public Customers(CustomerService customerService)
        {
            InitializeComponent();
            DataContext = new CustomerVM(customerService); // ✅ Using CustomerService instead of AppDbContext
        }
    }
}
