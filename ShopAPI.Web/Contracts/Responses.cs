using ShopAPI.Core.Entities;

namespace ShopAPI.Web.Contracts;

public sealed record UserResponse(int Id, string FirstName, string LastName, string Email, Role Role)
{
    public static UserResponse From(User user) => new(user.Id, user.FirstName, user.LastName, user.Email, user.Role);
}
public sealed record CategoryResponse(int Id, string Name)
{
    public static CategoryResponse From(Category category) => new(category.Id, category.Name);
}
public sealed record ProductResponse(int Id, string Name, decimal Price, int StockQuantity, bool IsActive, int CategoryId)
{
    public static ProductResponse From(Product product) => new(product.Id, product.Name, product.Price, product.StockQuantity, product.IsActive, product.CategoryId);
}
public sealed record CartLineResponse(int ProductId, string ProductName, int Quantity, decimal CurrentUnitPrice);
public sealed record CartResponse(int Id, IReadOnlyList<CartLineResponse> Items);
public sealed record OrderLineResponse(int Id, int ProductId, int Quantity, decimal UnitPrice);
public sealed record OrderResponse(int Id, int CustomerId, decimal TotalAmount, OrderStatus Status, int? PromoCodeId, IReadOnlyList<OrderLineResponse> Items)
{
    public static OrderResponse From(Order order) => new(order.Id, order.CustomerId, order.TotalAmount, order.Status, order.PromoCodeId,
        order.Items.Select(x => new OrderLineResponse(x.Id, x.ProductId, x.Quantity, x.PriceAtPurchase)).ToList());
}
public sealed record RefundLineResponse(int OrderItemId, int Quantity);
public sealed record RefundResponse(int Id, int OrderId, int UserId, RefundStatus Status, DateTime CreatedAt, IReadOnlyList<RefundLineResponse> Items)
{
    public static RefundResponse From(ShopAPI.Core.Entities.RefundRequest refund) => new(refund.Id, refund.OrderId, refund.UserId, refund.Status,
        refund.CreatedAt, refund.Items.Select(x => new RefundLineResponse(x.OrderItemId, x.Quantity)).ToList());
}
public sealed record DiscountResponse(int Id, int UserId, int ProductId, DiscountType Type, decimal Value, DateTime StartDate, DateTime EndDate, bool IsActive)
{
    public static DiscountResponse From(PersonalDiscount d) => new(d.Id, d.UserId, d.ProductId, d.Type, d.Value, d.StartDate, d.EndDate, d.IsActive);
}
public sealed record PromoResponse(int Id, string Code, decimal DiscountPercentage, DateTime ExpirationDate, int MaxUsages, int CurrentUsages, bool IsActive)
{
    public static PromoResponse From(PromoCode p) => new(p.Id, p.Code, p.DiscountPercentage, p.ExpirationDate, p.MaxUsages, p.CurrentUsages, p.IsActive);
}
