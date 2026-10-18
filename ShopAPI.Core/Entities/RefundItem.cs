namespace ShopAPI.Core.Entities;
public class RefundItem
{
    public int Id { get; set; }
    public int RefundRequestId { get; set; }
    public RefundRequest? RefundRequest { get; set; }
    
    public int OrderItemId { get; set; }
    public OrderItem? OrderItem { get; set; }
    
    public int Quantity { get; set; }
}