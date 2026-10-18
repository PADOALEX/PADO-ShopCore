using System.Security.Claims;
using ShopAPI.Core.Exceptions;

namespace ShopAPI.Web.Infrastructure;

public static class IdentityExtensions
{
    public static int UserId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id > 0
            ? id : throw ShopException.BadRequest("Invalid user identity.");
}
