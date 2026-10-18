using Microsoft.EntityFrameworkCore;
using ShopAPI.Core.Entities;
using ShopAPI.Core.Exceptions;
using ShopAPI.Infrastructure;
using ShopAPI.Infrastructure.Services;
using Xunit;

namespace ShopAPI.UnitTests;

public sealed class CartServiceTests
{
    private static ShopDbContext CreateDb() => new(new DbContextOptionsBuilder<ShopDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    public async Task InvalidQuantityIsRejected(int quantity)
    {
        await using var db = CreateDb();
        await Assert.ThrowsAsync<ShopException>(() => new CartService(db).AddItemToCartAsync(1, 1, quantity));
    }

    [Fact]
    public async Task AddingSameProductCombinesQuantityAndDoesNotExceedStock()
    {
        await using var db = CreateDb();
        db.Users.Add(new User { Id = 1, Email = "u@test.invalid", PasswordHash = "test" });
        db.Categories.Add(new Category { Id = 1, Name = "Gaming" });
        db.Products.Add(new Product { Id = 1, Name = "Controller", Price = 99m, CategoryId = 1, StockQuantity = 4, IsActive = true });
        await db.SaveChangesAsync();
        var carts = new CartService(db);
        await carts.AddItemToCartAsync(1, 1, 2);
        var cart = await carts.AddItemToCartAsync(1, 1, 2);
        Assert.Equal(4, Assert.Single(cart.Items).Quantity);
        var ex = await Assert.ThrowsAsync<ShopException>(() => carts.AddItemToCartAsync(1, 1, 1));
        Assert.Equal(409, ex.StatusCode);
        Assert.Equal(4, Assert.Single(db.Carts.Include(x => x.Items)).Items.Single().Quantity);
    }

    [Fact]
    public async Task InactiveProductIsRejected()
    {
        await using var db = CreateDb();
        db.Products.Add(new Product { Id = 9, CategoryId = 1, IsActive = false, StockQuantity = 20 });
        await db.SaveChangesAsync();
        var ex = await Assert.ThrowsAsync<ShopException>(() => new CartService(db).AddItemToCartAsync(1, 9, 1));
        Assert.Equal(409, ex.StatusCode);
    }
}
