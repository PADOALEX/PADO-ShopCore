using ShopAPI.Core.Entities;

namespace ShopAPI.Core.Services;

public static class PricingCalculator
{
    public static decimal ItemPrice(decimal basePrice, IEnumerable<PersonalDiscount> discounts)
    {
        if (basePrice < 0) throw new ArgumentOutOfRangeException(nameof(basePrice));
        var best = discounts.Select(d => d.Type == DiscountType.Percentage
            ? basePrice * Math.Clamp(d.Value, 0, 100) / 100m
            : Math.Clamp(d.Value, 0, basePrice)).DefaultIfEmpty(0m).Max();
        return decimal.Round(Math.Max(0m, basePrice - best), 2, MidpointRounding.AwayFromZero);
    }

    public static decimal ApplyPromo(decimal subtotal, decimal percentage) =>
        decimal.Round(subtotal * (1m - percentage / 100m), 2, MidpointRounding.AwayFromZero);
}
