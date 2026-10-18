using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopAPI.Core.Exceptions;
using ShopAPI.Infrastructure;
using ShopAPI.Infrastructure.Services;
using ShopAPI.Web.Contracts;
using ShopAPI.Web.Infrastructure;

namespace ShopAPI.Web.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public sealed class OrdersController(ShopDbContext db, OrderService orders) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> List(CancellationToken ct)
    {
        var query = db.Orders.Include(x => x.Items).AsNoTracking().AsQueryable();
        if (!User.IsInRole("Admin")) query = query.Where(x => x.CustomerId == User.UserId());
        return Ok((await query.OrderByDescending(x => x.Id).Take(100).ToListAsync(ct)).Select(OrderResponse.From));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> Get(int id, CancellationToken ct)
    {
        var query = db.Orders.Include(x => x.Items).AsNoTracking().Where(x => x.Id == id);
        if (!User.IsInRole("Admin")) query = query.Where(x => x.CustomerId == User.UserId());
        return Ok(OrderResponse.From(await query.SingleOrDefaultAsync(ct) ?? throw ShopException.NotFound("Order not found.")));
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<OrderResponse>> Checkout([FromBody] CheckoutRequest request, CancellationToken ct)
    {
        var order = await orders.CheckoutAsync(User.UserId(), request.PromoCode, ct);
        return CreatedAtAction(nameof(Get), new { id = order.Id }, OrderResponse.From(order));
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        await orders.CancelAsync(id, User.UserId(), User.IsInRole("Admin"), ct);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> Status(int id, [FromBody] OrderStatusRequest request, CancellationToken ct)
    {
        await orders.ChangeOrderStatusAsync(id, request.Status, ct);
        return NoContent();
    }
}
