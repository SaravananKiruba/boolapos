using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Model;

namespace Page_Navigation_App.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Subcategory> Subcategories { get; set; }
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<StockLedger> StockLedgers { get; set; }
        public DbSet<Finance> Finances { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<LogEntry> LogEntries { get; set; }
        public DbSet<SecurityLog> SecurityLogs { get; set; }
        public DbSet<RateMaster> RateMaster { get; set; }
        public DbSet<RepairJob> RepairJobs { get; set; }
        public DbSet<BusinessInfo> BusinessInfo { get; set; }
        public DbSet<Model.Setting> Settings { get; set; }
        public DbSet<EmailSettings> EmailSettings { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<EMI> EMIs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User-Role relationship through UserRole
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany()
                .HasForeignKey(ur => ur.UserID);

            // Configure EMI-Order relationship
            modelBuilder.Entity<EMI>()
                .HasOne(e => e.Order)
                .WithMany()
                .HasForeignKey(e => e.OrderID)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure EMI-Customer relationship
            modelBuilder.Entity<EMI>()
                .HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.OrderID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
