namespace ShopAPI.Core.Entities;

public enum OrderStatus 
{ 
    Created, 
    Paid, 
    Shipped, 
    Delivered, 
    Cancelled 
}

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Created;
    
    public int? PromoCodeId { get; set; }
    public PromoCode? PromoCode { get; set; }
    
    public List<OrderItem> Items { get; set; } = new();
}