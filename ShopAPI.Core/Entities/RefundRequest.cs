namespace ShopAPI.Core.Entities;

public class RefundRequest
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    
    public RefundStatus Status { get; set; } = RefundStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public List<RefundItem> Items { get; set; } = new();
}