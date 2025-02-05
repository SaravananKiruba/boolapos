using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Model; // Ensure this namespace is correct

namespace Page_Navigation_App.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Customer> Customers { get; set; }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
