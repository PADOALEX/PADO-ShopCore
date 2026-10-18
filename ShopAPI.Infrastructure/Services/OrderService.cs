using System.Data;
using Microsoft.EntityFrameworkCore;
using ShopAPI.Core.Entities;
using ShopAPI.Core.Exceptions;
using ShopAPI.Core.Services;

namespace ShopAPI.Infrastructure.Services;

public sealed class OrderService(ShopDbContext db)
{
    // PostgreSQL SERIALIZABLE guarantees that simultaneous checkouts cannot both commit from
    // inconsistent reads. Conditional SQL UPDATE provides a second inventory invariant.
    // Serialization failures are returned as HTTP 409; clients must retry deliberately.
    public async Task<Order> CheckoutAsync(int userId, string? promoCodeText, CancellationToken ct = default)
    {
        if (!db.Database.IsNpgsql()) throw new InvalidOperationException("Checkout requires PostgreSQL transaction semantics.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var cart = await db.Carts.Include(c => c.Items).SingleOrDefaultAsync(c => c.CustomerId == userId, ct);
        if (cart is null || cart.Items.Count == 0) throw ShopException.Conflict("Cart is empty.");

        var now = DateTime.UtcNow;
        var discounts = await db.PersonalDiscounts.AsNoTracking()
            .Where(d => d.UserId == userId && d.IsActive && d.StartDate <= now && d.EndDate >= now)
            .ToListAsync(ct);

        decimal total = 0m;
        var items = new List<OrderItem>();
        foreach (var cartItem in cart.Items.OrderBy(x => x.ProductId))
        {
            if (cartItem.Quantity <= 0) throw ShopException.Conflict("Cart contains an invalid quantity.");
            var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == cartItem.ProductId, ct)
                ?? throw ShopException.NotFound("A product in your cart no longer exists.");
            if (!product.IsActive) throw ShopException.Conflict($"Product {product.Id} is inactive.");
            var updated = await db.Products.Where(p => p.Id == product.Id && p.IsActive && p.StockQuantity >= cartItem.Quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity - cartItem.Quantity), ct);
            if (updated != 1) throw ShopException.Conflict($"Insufficient stock for product {product.Id}.");
            var unitPrice = PricingCalculator.ItemPrice(product.Price, discounts.Where(d => d.ProductId == product.Id));
            total += unitPrice * cartItem.Quantity;
            items.Add(new OrderItem { ProductId = product.Id, Quantity = cartItem.Quantity,
                PriceAtPurchase = unitPrice, Price = unitPrice });
        }
        total = decimal.Round(total, 2, MidpointRounding.AwayFromZero);
        PromoCode? promo = null;
        if (!string.IsNullOrWhiteSpace(promoCodeText))
        {
            var code = promoCodeText.Trim().ToUpperInvariant();
            promo = await db.PromoCodes.AsNoTracking().SingleOrDefaultAsync(p => p.Code == code, ct)
                ?? throw ShopException.BadRequest("Invalid promo code.");
            if (!promo.IsActive || promo.ExpirationDate <= now || promo.CurrentUsages >= promo.MaxUsages ||
                promo.DiscountPercentage is <= 0 or > 100)
                throw ShopException.Conflict("Promo code is expired or exhausted.");
            var changed = await db.PromoCodes.Where(p => p.Id == promo.Id && p.IsActive &&
                    p.ExpirationDate > now && p.CurrentUsages < p.MaxUsages)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.CurrentUsages, p => p.CurrentUsages + 1), ct);
            if (changed != 1) throw ShopException.Conflict("Promo code is exhausted.");
            total = PricingCalculator.ApplyPromo(total, promo.DiscountPercentage);
        }
        var order = new Order { CustomerId = userId, TotalAmount = total, Status = OrderStatus.Created,
            PromoCodeId = promo?.Id, Items = items };
        db.Orders.Add(order);
        db.Carts.Remove(cart);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return order;
    }

    public async Task CancelAsync(int orderId, int userId, bool isAdmin, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var order = await db.Orders.Include(x => x.Items).AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == orderId && (isAdmin || x.CustomerId == userId), ct)
            ?? throw ShopException.NotFound("Order not found.");
        if (order.Status != OrderStatus.Created) throw ShopException.Conflict("Only created orders can be cancelled.");
        var changed = await db.Orders.Where(x => x.Id == orderId && x.Status == OrderStatus.Created)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, OrderStatus.Cancelled), ct);
        if (changed != 1) throw ShopException.Conflict("Order status changed concurrently.");
        foreach (var item in order.Items.OrderBy(x => x.ProductId))
            await db.Products.Where(p => p.Id == item.ProductId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity + item.Quantity), ct);
        if (order.PromoCodeId is int promoId)
            await db.PromoCodes.Where(p => p.Id == promoId && p.CurrentUsages > 0)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.CurrentUsages, p => p.CurrentUsages - 1), ct);
        await tx.CommitAsync(ct);
    }

    public async Task ChangeOrderStatusAsync(int orderId, OrderStatus next, CancellationToken ct = default)
    {
        var order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == orderId, ct)
            ?? throw ShopException.NotFound("Order not found.");
        var allowed = (order.Status, next) is
            (OrderStatus.Created, OrderStatus.Paid) or
            (OrderStatus.Paid, OrderStatus.Shipped) or
            (OrderStatus.Shipped, OrderStatus.Delivered);
        if (!allowed) throw ShopException.Conflict("Invalid order status transition.");
        var updated = await db.Orders.Where(x => x.Id == orderId && x.Status == order.Status)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, next), ct);
        if (updated != 1) throw ShopException.Conflict("Order status changed concurrently.");
    }
}
