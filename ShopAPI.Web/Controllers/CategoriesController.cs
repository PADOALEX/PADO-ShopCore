using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopAPI.Core.Entities;
using ShopAPI.Core.Exceptions;
using ShopAPI.Infrastructure;
using ShopAPI.Web.Contracts;

namespace ShopAPI.Web.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(ShopDbContext db) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> List(CancellationToken ct) =>
        Ok((await db.Categories.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct)).Select(CategoryResponse.From));

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> Get(int id, CancellationToken ct) =>
        Ok(CategoryResponse.From(await db.Categories.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ShopException.NotFound("Category not found.")));

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create([FromBody] CategoryRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) throw ShopException.BadRequest("Category name is required.");
        var category = new Category { Name = request.Name.Trim() };
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        return Created($"/api/categories/{category.Id}", CategoryResponse.From(category));
    }
}
