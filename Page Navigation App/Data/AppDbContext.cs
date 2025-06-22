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
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<StockItem> StockItems { get; set; }
        public DbSet<StockLedger> StockLedgers { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Finance> Finances { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<LogEntry> LogEntries { get; set; }
        public DbSet<SecurityLog> SecurityLogs { get; set; }public DbSet<RateMaster> RateMaster { get; set; }
        public DbSet<BusinessInfo> BusinessInfo { get; set; }
        public DbSet<Model.Setting> Settings { get; set; }
        public DbSet<EmailSettings> EmailSettings { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<EMI> EMIs { get; set; }
        public DbSet<ReportData> ReportData { get; set; }
        
        // Properties referenced in services
        public DbSet<RateMaster> RateMasters { get; set; }
        public DbSet<HUIDLog> HUIDLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);            // Configure User-Role relationships through UserRole
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserID);
                
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany()
                .HasForeignKey(ur => ur.RoleID);

            // Configure EMI-Order relationship
            modelBuilder.Entity<EMI>()
                .HasOne(e => e.Order)
                .WithMany()
                .HasForeignKey(e => e.OrderID)
                .OnDelete(DeleteBehavior.Cascade);            // Configure EMI-Customer relationship
            modelBuilder.Entity<EMI>()
                .HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerID)
                .OnDelete(DeleteBehavior.NoAction);
                
            // Add indexes for faster querying
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.HUID);
                
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.TagNumber);
                
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderDate);
                  modelBuilder.Entity<Customer>()
                .HasIndex(c => c.PhoneNumber);
        }
    }
}
