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
[Route("api/refunds")]
[Authorize]
public sealed class RefundsController(ShopDbContext db, RefundService refunds) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RefundResponse>>> List(CancellationToken ct)
    {
        var query = db.RefundRequests.Include(x => x.Items).AsNoTracking().AsQueryable();
        if (!User.IsInRole("Admin")) query = query.Where(x => x.UserId == User.UserId());
        return Ok((await query.OrderByDescending(x => x.Id).Take(100).ToListAsync(ct)).Select(RefundResponse.From));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RefundResponse>> Get(int id, CancellationToken ct)
    {
        var query = db.RefundRequests.Include(x => x.Items).AsNoTracking().Where(x => x.Id == id);
        if (!User.IsInRole("Admin")) query = query.Where(x => x.UserId == User.UserId());
        return Ok(RefundResponse.From(await query.SingleOrDefaultAsync(ct)
            ?? throw ShopException.NotFound("Refund request not found.")));
    }

    [HttpPost("request")]
    public async Task<ActionResult<RefundResponse>> RequestRefund([FromBody] RefundRequestDto request, CancellationToken ct)
    {
        var refund = await refunds.RequestAsync(User.UserId(), request.OrderId,
            request.Items.Select(x => (x.OrderItemId, x.Quantity)).ToArray(), ct);
        return Created($"/api/refunds/{refund.Id}", RefundResponse.From(refund));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        await refunds.DecideAsync(id, true, ct);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, CancellationToken ct)
    {
        await refunds.DecideAsync(id, false, ct);
        return NoContent();
    }
}
