using System.Data;
using Microsoft.EntityFrameworkCore;
using ShopAPI.Core.Entities;
using ShopAPI.Core.Exceptions;

namespace ShopAPI.Infrastructure.Services;

public sealed class RefundService(ShopDbContext db)
{
    // Refund approval is inventory-only; no payment is captured or reimbursed.
    public async Task<RefundRequest> RequestAsync(int userId, int orderId, IReadOnlyList<(int OrderItemId, int Quantity)> lines, CancellationToken ct = default)
    {
        if (lines.Count == 0 || lines.Any(x => x.Quantity <= 0) || lines.Select(x => x.OrderItemId).Distinct().Count() != lines.Count)
            throw ShopException.BadRequest("Provide distinct order items with positive quantities.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var order = await db.Orders.Include(x => x.Items).AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == orderId && x.CustomerId == userId, ct)
            ?? throw ShopException.NotFound("Order not found.");
        if (order.Status != OrderStatus.Delivered) throw ShopException.Conflict("Only delivered orders support refund requests.");
        var alreadyRequested = await db.RefundItems.AsNoTracking()
            .Where(x => x.RefundRequest!.OrderId == orderId && x.RefundRequest.Status != RefundStatus.Rejected)
            .GroupBy(x => x.OrderItemId)
            .Select(g => new { Id = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.Id, x => x.Quantity, ct);
        foreach (var line in lines)
        {
            var purchased = order.Items.SingleOrDefault(x => x.Id == line.OrderItemId)
                ?? throw ShopException.BadRequest("Refund item does not belong to the order.");
            if (line.Quantity > purchased.Quantity - alreadyRequested.GetValueOrDefault(line.OrderItemId))
                throw ShopException.Conflict("Refund quantity exceeds unclaimed purchased quantity.");
        }
        var refund = new RefundRequest { UserId = userId, OrderId = orderId,
            Items = lines.Select(x => new RefundItem { OrderItemId = x.OrderItemId, Quantity = x.Quantity }).ToList() };
        db.RefundRequests.Add(refund);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return refund;
    }

    public async Task DecideAsync(int id, bool approve, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var refund = await db.RefundRequests.Include(x => x.Items).ThenInclude(x => x.OrderItem)
            .AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ShopException.NotFound("Refund request not found.");
        var status = approve ? RefundStatus.Approved : RefundStatus.Rejected;
        var changed = await db.RefundRequests.Where(x => x.Id == id && x.Status == RefundStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, status), ct);
        if (changed != 1) throw ShopException.Conflict("Refund request was already decided.");
        if (approve)
        {
            foreach (var line in refund.Items.OrderBy(x => x.OrderItem!.ProductId))
                await db.Products.Where(p => p.Id == line.OrderItem!.ProductId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity + line.Quantity), ct);
        }
        await tx.CommitAsync(ct);
    }
}
