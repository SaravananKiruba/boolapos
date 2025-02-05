using System.Windows.Controls;
using Page_Navigation_App.ViewModel;
using Page_Navigation_App.Data; // For AppDbContext

namespace Page_Navigation_App.View
{
    public partial class Customers : UserControl
    {
        public Customers(AppDbContext dbContext)
        {
            InitializeComponent();
            DataContext = new CustomerVM(dbContext); // ✅ Passing dbContext here
        }
    }
}
