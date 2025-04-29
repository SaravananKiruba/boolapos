using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Model;

namespace Page_Navigation_App.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Subcategory> Subcategories { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<RepairJob> RepairJobs { get; set; }
        public DbSet<Finance> Finances { get; set; }
        public DbSet<RateMaster> RateMaster { get; set; }
        public DbSet<Setting> Settings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Product relationships
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Subcategory)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SubcategoryID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Supplier)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SupplierID)
                .OnDelete(DeleteBehavior.Restrict);

            // Subcategory relationships
            modelBuilder.Entity<Subcategory>()
                .HasOne(s => s.Category)
                .WithMany(c => c.Subcategories)
                .HasForeignKey(s => s.CategoryID)
                .OnDelete(DeleteBehavior.Restrict);

            // Order relationships
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Product)
                .WithMany(p => p.OrderDetails)
                .HasForeignKey(od => od.ProductID)
                .OnDelete(DeleteBehavior.Restrict);

            // Stock relationships
            modelBuilder.Entity<Stock>()
                .HasOne(s => s.Product)
                .WithMany(p => p.Stocks)
                .HasForeignKey(s => s.ProductID)
                .OnDelete(DeleteBehavior.Restrict);

            // RepairJob relationships
            modelBuilder.Entity<RepairJob>()
                .HasOne(r => r.Customer)
                .WithMany(c => c.RepairJobs)
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Finance relationships
            modelBuilder.Entity<Finance>()
                .HasOne(f => f.Customer)
                .WithMany(c => c.Payments)
                .HasForeignKey(f => f.CustomerID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Finance>()
                .HasOne(f => f.Order)
                .WithMany(o => o.Payments)
                .HasForeignKey(f => f.OrderID)
                .OnDelete(DeleteBehavior.Restrict);

            // Set decimal precision defaults
            modelBuilder.Entity<Product>()
                .Property(p => p.BasePrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<RateMaster>()
                .Property(r => r.Rate)
                .HasPrecision(18, 2);

            // Add indexes for better performance
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Barcode);

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.PhoneNumber);

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.GSTNumber);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderDate);

            modelBuilder.Entity<Stock>()
                .HasIndex(s => s.Location);

            modelBuilder.Entity<RateMaster>()
                .HasIndex(r => r.EffectiveDate);

            // Settings configuration
            modelBuilder.Entity<Setting>()
                .HasKey(s => s.Key);

            modelBuilder.Entity<Setting>()
                .Property(s => s.Value)
                .IsRequired();
        }
    }
}
