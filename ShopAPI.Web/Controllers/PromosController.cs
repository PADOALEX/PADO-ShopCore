using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopAPI.Core.Entities;
using ShopAPI.Core.Exceptions;
using ShopAPI.Infrastructure;
using ShopAPI.Web.Contracts;

namespace ShopAPI.Web.Controllers;

[ApiController]
[Route("api/promos")]
[Authorize(Roles = "Admin")]
public sealed class PromosController(ShopDbContext db) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PromoResponse>> Get(int id, CancellationToken ct) =>
        Ok(PromoResponse.From(await db.PromoCodes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ShopException.NotFound("Promo code not found.")));

    [HttpPost]
    public async Task<ActionResult<PromoResponse>> Create(PromoCodeRequest request, CancellationToken ct)
    {
        if (request.ExpirationDate.Kind != DateTimeKind.Utc || request.ExpirationDate <= DateTime.UtcNow)
            throw ShopException.BadRequest("ExpirationDate must be in the future (UTC).");
        var promo = new PromoCode { Code = request.Code.ToUpperInvariant(),
            DiscountPercentage = request.DiscountPercentage, ExpirationDate = request.ExpirationDate,
            MaxUsages = request.MaxUsages, IsActive = request.IsActive };
        db.PromoCodes.Add(promo);
        await db.SaveChangesAsync(ct);
        return Created($"/api/promos/{promo.Id}", PromoResponse.From(promo));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PromoResponse>>> List(CancellationToken ct) =>
        Ok((await db.PromoCodes.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct)).Select(PromoResponse.From));
}
