using System.ComponentModel.DataAnnotations;
using ShopAPI.Core.Entities;

namespace ShopAPI.Web.Contracts;

public sealed record RegisterRequest(
    [Required, StringLength(100, MinimumLength = 1)] string FirstName,
    [Required, StringLength(100, MinimumLength = 1)] string LastName,
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, MinLength(12), MaxLength(72)] string Password);

public sealed record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);

public sealed record CategoryRequest([Required, StringLength(100, MinimumLength = 1)] string Name);
public sealed record ProductRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [Range(typeof(decimal), "0.01", "999999999.99")] decimal Price,
    [Range(0, int.MaxValue)] int StockQuantity,
    bool IsActive,
    [Range(1, int.MaxValue)] int CategoryId);
public sealed record CartItemRequest([Range(1, int.MaxValue)] int ProductId, [Range(1, 1000000)] int Quantity);
public sealed record CheckoutRequest([StringLength(60)] string? PromoCode);
public sealed record OrderStatusRequest([EnumDataType(typeof(OrderStatus))] OrderStatus Status);
public sealed record DiscountRequest(
    [Range(1, int.MaxValue)] int UserId,
    [Range(1, int.MaxValue)] int ProductId,
    [EnumDataType(typeof(DiscountType))] DiscountType Type,
    [Range(typeof(decimal), "0.01", "999999999.99")] decimal Value,
    DateTime StartDate, DateTime EndDate);
public sealed record PromoCodeRequest(
    [Required, RegularExpression("^[A-Za-z0-9_-]{3,60}$")] string Code,
    [Range(typeof(decimal), "0.01", "100")] decimal DiscountPercentage,
    DateTime ExpirationDate,
    [Range(1, 1000000)] int MaxUsages,
    bool IsActive);
public sealed record RefundItemRequest([Range(1, int.MaxValue)] int OrderItemId, [Range(1, int.MaxValue)] int Quantity);
public sealed record RefundRequestDto([Range(1, int.MaxValue)] int OrderId, [Required, MinLength(1)] List<RefundItemRequest> Items);
