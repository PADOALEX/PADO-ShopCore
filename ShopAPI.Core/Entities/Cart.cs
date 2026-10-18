namespace ShopAPI.Core.Entities;

public class Cart
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public User Customer { get; set; } = null!;
    public List<CartItem> Items { get; set; } = new();
}