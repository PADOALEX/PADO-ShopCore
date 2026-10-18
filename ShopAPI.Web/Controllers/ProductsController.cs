using Microsoft.AspNetCore.Authorization;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopAPI.Core.Entities;
using ShopAPI.Core.Exceptions;
using ShopAPI.Infrastructure;
using ShopAPI.Web.Contracts;

namespace ShopAPI.Web.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(ShopDbContext db) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponse>>> List([FromQuery] int? categoryId, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 100) throw ShopException.BadRequest("Invalid pagination parameters.");
        var products = db.Products.AsNoTracking().Where(p => p.IsActive);
        if (categoryId is not null) products = products.Where(p => p.CategoryId == categoryId);
        return Ok((await products.OrderBy(p => p.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct)).Select(ProductResponse.From));
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponse>> Get(int id, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && (x.IsActive || User.IsInRole("Admin")), ct)
            ?? throw ShopException.NotFound("Product not found.");
        return Ok(ProductResponse.From(product));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create([FromBody] ProductRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) throw ShopException.BadRequest("Product name is required.");
        if (!await db.Categories.AnyAsync(x => x.Id == request.CategoryId, ct)) throw ShopException.NotFound("Category not found.");
        var product = new Product { Name = request.Name.Trim(), Price = request.Price, StockQuantity = request.StockQuantity,
            IsActive = request.IsActive, CategoryId = request.CategoryId };
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = product.Id }, ProductResponse.From(product));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductResponse>> Update(int id, [FromBody] ProductRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) throw ShopException.BadRequest("Product name is required.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var product = await db.Products.FindAsync([id], ct) ?? throw ShopException.NotFound("Product not found.");
        if (!await db.Categories.AnyAsync(x => x.Id == request.CategoryId, ct)) throw ShopException.NotFound("Category not found.");
        product.Name = request.Name.Trim(); product.Price = request.Price;
        product.StockQuantity = request.StockQuantity; product.IsActive = request.IsActive;
        product.CategoryId = request.CategoryId;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Ok(ProductResponse.From(product));
    }
}
