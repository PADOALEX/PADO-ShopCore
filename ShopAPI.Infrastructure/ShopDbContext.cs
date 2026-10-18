using Microsoft.EntityFrameworkCore;
using ShopAPI.Core.Entities;

namespace ShopAPI.Infrastructure;

public class ShopDbContext(DbContextOptions<ShopDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<PersonalDiscount> PersonalDiscounts => Set<PersonalDiscount>();
    public DbSet<RefundRequest> RefundRequests => Set<RefundRequest>();
    public DbSet<RefundItem> RefundItems => Set<RefundItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().ToTable("User").HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Cart>().HasIndex(x => x.CustomerId).IsUnique();
        modelBuilder.Entity<CartItem>().HasIndex(x => new { x.CartId, x.ProductId }).IsUnique();
        modelBuilder.Entity<PromoCode>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Order>().HasIndex(x => x.CustomerId);
        modelBuilder.Entity<Order>().HasOne<User>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Product>().Property(x => x.Price).HasColumnType("numeric(18,2)");
        modelBuilder.Entity<Order>().Property(x => x.TotalAmount).HasColumnType("numeric(18,2)");
        modelBuilder.Entity<PromoCode>().Property(x => x.DiscountPercentage).HasColumnType("numeric(5,2)");
        modelBuilder.Entity<OrderItem>().Property(x => x.Price).HasColumnType("numeric(18,2)");
        modelBuilder.Entity<OrderItem>().Property(x => x.PriceAtPurchase).HasColumnType("numeric(18,2)");
        modelBuilder.Entity<PersonalDiscount>().Property(x => x.Value).HasColumnType("numeric(18,2)");
        modelBuilder.Entity<RefundItem>().HasIndex(x => new { x.RefundRequestId, x.OrderItemId }).IsUnique();
    }
}
