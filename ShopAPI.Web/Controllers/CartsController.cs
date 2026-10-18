using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopAPI.Infrastructure.Services;
using ShopAPI.Web.Contracts;
using ShopAPI.Web.Infrastructure;

namespace ShopAPI.Web.Controllers;

[ApiController]
[Route("api/carts")]
[Authorize]
public sealed class CartsController(CartService carts) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CartResponse>> Get(CancellationToken ct)
    {
        var cart = await carts.GetCartAsync(User.UserId(), ct);
        return Ok(new CartResponse(cart.Id, cart.Items.Select(x => new CartLineResponse(x.ProductId,
            x.Product?.Name ?? "", x.Quantity, x.Product?.Price ?? 0m)).ToList()));
    }

    [HttpPost("add-item")]
    public async Task<ActionResult<CartResponse>> AddItem([FromBody] CartItemRequest request, CancellationToken ct)
    {
        await carts.AddItemToCartAsync(User.UserId(), request.ProductId, request.Quantity, ct);
        return await Get(ct);
    }

    [HttpDelete("items/{productId:int}")]
    public async Task<IActionResult> Remove(int productId, CancellationToken ct)
    {
        await carts.RemoveItemAsync(User.UserId(), productId, ct);
        return NoContent();
    }
}
