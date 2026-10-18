namespace ShopAPI.Core.Entities;

public class PersonalDiscount
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    
    public DiscountType Type { get; set; }
    public decimal Value { get; set; }
    
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsValid() => IsActive && DateTime.UtcNow >= StartDate && DateTime.UtcNow <= EndDate;
}