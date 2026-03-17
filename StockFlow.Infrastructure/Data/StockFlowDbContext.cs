using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StockFlow.Domain.Entities;
using StockFlow.Infrastructure.Identity;


namespace StockFlow.Infrastructure.Data;

// EF Core DbContext: represents the database session and manages entity persistence
public class StockFlowDbContext : IdentityDbContext<ApplicationUser>
{
  // Constructor used by dependency injection to pass database configuration
  public StockFlowDbContext(DbContextOptions<StockFlowDbContext> options) : base(options) { }

  // DbSet representing the Products table
  public DbSet<Product> Products => Set<Product>();
  public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
  public DbSet<Order> Orders => Set<Order>();
  public DbSet<OrderLine> OrderLines => Set<OrderLine>();
  public DbSet<StockMovement> StockMovements => Set<StockMovement>();
  public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
  

  // Configure entity mappings and database schema rules
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    // Configure Product entity mapping
    modelBuilder.Entity<Product>( b =>
    {
      // Map entity to Products table
      b.ToTable("Products");
      // Define primary key
      b.HasKey(x => x.Id);

      // Configure SKU column (required, max length 50)
      b.Property(x => x.Sku).IsRequired().HasMaxLength(50);
      // Configure Name column (required, max length 200)
      b.Property(x => x.Name).IsRequired().HasMaxLength(200);

      // Create unique index on SKU to prevent duplicates
      b.HasIndex(x => x.Sku).IsUnique();
    });

    modelBuilder.Entity<InventoryItem>(b =>
    {
      b.ToTable("InventoryItems");
      b.HasKey( x => x.Id);

      b.Property( x => x.OnHand).IsRequired();
      b.Property( x => x.Reserved).IsRequired();
      b.Property( x => x.UpdatedAt).IsRequired();
      b.Property( x => x.RowVersion).IsRowVersion();

      // Create a unique index on ProductId
      // Business rule: one product can only have ONE inventory record
      b.HasIndex( x => x.ProductId).IsUnique();
      
      // Configure relationship between InventoryItem and Product
      b.HasOne(x => x.Product)                    // Each InventoryItem belongs to ONE Product
          .WithMany()                             // Product side does not define a collection navigation
          .HasForeignKey(x => x.ProductId)        // ProductId is the foreign key
          .OnDelete(DeleteBehavior.Cascade);      // If a Product is deleted, its InventoryItem will also be deleted automatically
    });

    modelBuilder.Entity<Order>(b =>
    {
      b.ToTable("Orders");
      b.HasKey(x => x.Id);

      b.Property(x => x.OrderNumber).IsRequired().HasMaxLength(50);
      b.Property(x => x.Status).IsRequired().HasMaxLength(30);
      b.Property(x => x.CustomerName).IsRequired().HasMaxLength(200);
      b.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
      b.Property(x => x.CreatedAt).IsRequired();

      b.HasMany(x => x.Lines).WithOne(x => x.Order).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<OrderLine>(b =>
    {
      b.ToTable("OrderLines");
      b.HasKey(x => x.Id);

      b.Property(x => x.Quantity).IsRequired();
      b.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");

      b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

    });
 
    modelBuilder.Entity<StockMovement>(b =>
    {
      b.ToTable("StockMovements");
      b.HasKey(x => x.Id);

      b.Property(x => x.QtyDelta).IsRequired();
      b.Property(x => x.Type).IsRequired().HasMaxLength(30);
      b.Property(x => x.RefId).HasMaxLength(100);
      b.Property(x => x.CreatedAt).IsRequired();

      b.HasOne(x => x.Product)
        .WithMany()
        .HasForeignKey(x => x.ProductId)
        .OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<IdempotencyRecord>(b =>
    {
      b.ToTable("IdempotencyRecords");
      b.HasKey(x => x.Id);

      b.Property(x => x.Key).IsRequired().HasMaxLength(200);
      b.Property(x => x.RequestType).IsRequired().HasMaxLength(200);
      b.Property(x => x.RequestHash).IsRequired().HasMaxLength(200);
      b.Property(x => x.Status).IsRequired().HasMaxLength(30);
      b.Property(x => x.CreatedAt).IsRequired();
      b.Property(x => x.CompletedAt);

      b.HasIndex(x => new { x.Key, x.RequestType}).IsUnique();

      b.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
    });
 }


}