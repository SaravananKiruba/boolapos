using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Page_Navigation_App.Data;

namespace Page_Navigation_App
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlite("Data Source=StockInventory.db");

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
