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
        public DbSet<Finance> Finances { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<LogEntry> LogEntries { get; set; }
        public DbSet<RateMaster> RateMaster { get; set; }
        public DbSet<BusinessInfo> BusinessInfo { get; set; }
        public DbSet<EmailSettings> EmailSettings { get; set; }
        public DbSet<ReportData> ReportData { get; set; }
        
        // New entities for workflow implementation
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<StockItem> StockItems { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
        
        // Properties referenced in services
        public DbSet<RateMaster> RateMasters { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

           
                
            // Add indexes for faster querying
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.HUID);
                
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.TagNumber);
                
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderDate);

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.PhoneNumber);

            // Configure Stock relationships
            modelBuilder.Entity<Stock>()
                .HasOne(s => s.Product)
                .WithMany()
                .HasForeignKey(s => s.ProductID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Stock>()
                .HasOne(s => s.Supplier)
                .WithMany()
                .HasForeignKey(s => s.SupplierID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure StockItem relationships
            modelBuilder.Entity<StockItem>()
                .HasOne(si => si.Product)
                .WithMany()
                .HasForeignKey(si => si.ProductID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StockItem>()
                .HasOne(si => si.Order)
                .WithMany()
                .HasForeignKey(si => si.OrderID)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<StockItem>()
                .HasOne(si => si.Customer)
                .WithMany()
                .HasForeignKey(si => si.CustomerID)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<StockItem>()
                .HasOne(si => si.PurchaseOrderItem)
                .WithMany()
                .HasForeignKey(si => si.PurchaseOrderItemID)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure PurchaseOrder relationships
            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(po => po.Supplier)
                .WithMany()
                .HasForeignKey(po => po.SupplierID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure PurchaseOrderItem relationships
            modelBuilder.Entity<PurchaseOrderItem>()
                .HasOne(poi => poi.PurchaseOrder)
                .WithMany(po => po.PurchaseOrderItems)
                .HasForeignKey(poi => poi.PurchaseOrderID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PurchaseOrderItem>()
                .HasOne(poi => poi.Product)
                .WithMany()
                .HasForeignKey(poi => poi.ProductID)
                .OnDelete(DeleteBehavior.Restrict);

            // Add indexes for performance
            modelBuilder.Entity<StockItem>()
                .HasIndex(si => si.UniqueTagID)
                .IsUnique();

            modelBuilder.Entity<StockItem>()
                .HasIndex(si => si.Barcode)
                .IsUnique();

            modelBuilder.Entity<StockItem>()
                .HasIndex(si => si.Status);

            modelBuilder.Entity<PurchaseOrder>()
                .HasIndex(po => po.PurchaseOrderNumber)
                .IsUnique();

            modelBuilder.Entity<Stock>()
                .HasIndex(s => new { s.ProductID, s.Status });
        }
    }
}
