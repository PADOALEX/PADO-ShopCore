using System.Data;
using Microsoft.EntityFrameworkCore;
using ShopAPI.Core.Entities;
using ShopAPI.Core.Exceptions;

namespace ShopAPI.Infrastructure.Services;

public sealed class CartService(ShopDbContext db)
{
    public async Task<Cart> GetCartAsync(int userId, CancellationToken ct = default) =>
        await db.Carts.Include(c => c.Items).ThenInclude(i => i.Product)
            .AsNoTracking().SingleOrDefaultAsync(c => c.CustomerId == userId, ct)
        ?? new Cart { CustomerId = userId };

    public async Task<Cart> AddItemToCartAsync(int userId, int productId, int quantity, CancellationToken ct = default)
    {
        if (quantity <= 0 || quantity > 1_000_000) throw ShopException.BadRequest("Quantity must be between 1 and 1,000,000.");
        if (productId <= 0) throw ShopException.BadRequest("Product ID must be positive.");
        // In-memory provider is used for isolated unit tests; all production writes use PostgreSQL transactions.
        await using var tx = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == productId, ct)
            ?? throw ShopException.NotFound("Product not found.");
        if (!product.IsActive) throw ShopException.Conflict("Product is inactive.");
        var cart = await db.Carts.Include(c => c.Items).SingleOrDefaultAsync(c => c.CustomerId == userId, ct);
        if (cart is null)
        {
            cart = new Cart { CustomerId = userId };
            db.Carts.Add(cart);
        }
        var item = cart.Items.SingleOrDefault(i => i.ProductId == productId);
        if (quantity > product.StockQuantity - (item?.Quantity ?? 0))
            throw ShopException.Conflict("Quantity exceeds available stock.");
        if (item is null) cart.Items.Add(new CartItem { ProductId = productId, Quantity = quantity });
        else item.Quantity += quantity;
        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return cart;
    }

    public async Task RemoveItemAsync(int userId, int productId, CancellationToken ct = default)
    {
        var affected = await db.CartItems.Where(x => x.Cart.CustomerId == userId && x.ProductId == productId)
            .ExecuteDeleteAsync(ct);
        if (affected == 0) throw ShopException.NotFound("Item is not in your cart.");
    }
}
