using Microsoft.EntityFrameworkCore;

using Page_Navigation_App.Model; // Ensure this namespace is correct

namespace Page_Navigation_App.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<Subcategory> Subcategories { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Finance> Finances { get; set; }
        public DbSet<Category> Categories { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
