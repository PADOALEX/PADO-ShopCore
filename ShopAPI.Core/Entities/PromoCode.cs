namespace ShopAPI.Core.Entities;

public class PromoCode
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal DiscountPercentage { get; set; } 
    public DateTime ExpirationDate { get; set; }
    public int MaxUsages { get; set; }
    public int CurrentUsages { get; set; }
    public bool IsActive { get; set; }
    public bool IsValid() => IsActive && ExpirationDate > DateTime.UtcNow && CurrentUsages < MaxUsages;
}