using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopAPI.Core.Entities;
using ShopAPI.Core.Exceptions;
using ShopAPI.Infrastructure;
using ShopAPI.Web.Contracts;

namespace ShopAPI.Web.Controllers;

[ApiController]
[Route("api/discounts")]
[Authorize(Roles = "Admin")]
public sealed class DiscountsController(ShopDbContext db) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<DiscountResponse>> Get(int id, CancellationToken ct) =>
        Ok(DiscountResponse.From(await db.PersonalDiscounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ShopException.NotFound("Discount not found.")));

    [HttpPost]
    public async Task<ActionResult<DiscountResponse>> Create(DiscountRequest request, CancellationToken ct)
    {
        if (request.StartDate.Kind != DateTimeKind.Utc || request.EndDate.Kind != DateTimeKind.Utc || request.StartDate >= request.EndDate)
            throw ShopException.BadRequest("StartDate and EndDate must be UTC with StartDate before EndDate.");
        if (request.Type == DiscountType.Percentage && request.Value > 100)
            throw ShopException.BadRequest("Percentage discount cannot exceed 100.");
        if (!await db.Users.AnyAsync(x => x.Id == request.UserId, ct) ||
            !await db.Products.AnyAsync(x => x.Id == request.ProductId, ct)) throw ShopException.NotFound("User or product not found.");
        var d = new PersonalDiscount { UserId = request.UserId, ProductId = request.ProductId,
            Type = request.Type, Value = request.Value, StartDate = request.StartDate, EndDate = request.EndDate, IsActive = true };
        db.PersonalDiscounts.Add(d);
        await db.SaveChangesAsync(ct);
        return Created($"/api/discounts/{d.Id}", DiscountResponse.From(d));
    }
}
