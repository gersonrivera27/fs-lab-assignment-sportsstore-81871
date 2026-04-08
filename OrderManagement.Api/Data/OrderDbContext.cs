using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Data.Entities;

namespace OrderManagement.Api.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryRecord> InventoryRecords => Set<InventoryRecord>();
    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();
    public DbSet<ShipmentRecord> ShipmentRecords => Set<ShipmentRecord>();
    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.InventoryRecord)
            .WithOne(r => r.Order)
            .HasForeignKey<InventoryRecord>(r => r.OrderId);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.PaymentRecord)
            .WithOne(r => r.Order)
            .HasForeignKey<PaymentRecord>(r => r.OrderId);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.ShipmentRecord)
            .WithOne(r => r.Order)
            .HasForeignKey<ShipmentRecord>(r => r.OrderId);

        modelBuilder.Entity<Order>()
            .HasMany(o => o.StatusHistory)
            .WithOne(h => h.Order)
            .HasForeignKey(h => h.OrderId);

        modelBuilder.Entity<Order>()
            .Property(o => o.Status)
            .HasConversion<string>();

        // Seed products mirroring existing SportsStore data
        modelBuilder.Entity<Product>().HasData(
            new Product { ProductId = 1, Name = "Kayak", Description = "A boat for one person", Price = 275, Category = "Watersports", StockQuantity = 50 },
            new Product { ProductId = 2, Name = "Lifejacket", Description = "Protective and fashionable", Price = 48.95m, Category = "Watersports", StockQuantity = 200 },
            new Product { ProductId = 3, Name = "Soccer Ball", Description = "FIFA-approved size and weight", Price = 19.50m, Category = "Soccer", StockQuantity = 150 },
            new Product { ProductId = 4, Name = "Corner Flags", Description = "Give your pitch a professional touch", Price = 34.95m, Category = "Soccer", StockQuantity = 75 },
            new Product { ProductId = 5, Name = "Stadium", Description = "Flat-packed 35,000-seat stadium", Price = 79500m, Category = "Soccer", StockQuantity = 5 },
            new Product { ProductId = 6, Name = "Thinking Cap", Description = "Improve brain efficiency by 75%", Price = 16m, Category = "Chess", StockQuantity = 300 },
            new Product { ProductId = 7, Name = "Unsteady Chair", Description = "Secretly give your opponent a disadvantage", Price = 29.95m, Category = "Chess", StockQuantity = 50 },
            new Product { ProductId = 8, Name = "Human Chess Board", Description = "A fun game for the family", Price = 75m, Category = "Chess", StockQuantity = 30 },
            new Product { ProductId = 9, Name = "Bling-Bling King", Description = "Gold-plated, diamond-studded King", Price = 1200m, Category = "Chess", StockQuantity = 10 }
        );
    }
}
