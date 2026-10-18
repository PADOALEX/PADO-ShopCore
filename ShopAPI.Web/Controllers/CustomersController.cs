using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopAPI.Infrastructure;
using ShopAPI.Web.Contracts;

namespace ShopAPI.Web.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Roles = "Admin")]
public sealed class CustomersController(ShopDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> List(CancellationToken ct) =>
        Ok((await db.Users.AsNoTracking().OrderBy(u => u.Id).Take(100).ToListAsync(ct)).Select(UserResponse.From));
    // Public customer creation was removed. Only /api/auth/register can create a regular user.
}
