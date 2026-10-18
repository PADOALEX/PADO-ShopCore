using ShopAPI.Core.Entities;
using ShopAPI.Core.Services;
using Xunit;

namespace ShopAPI.UnitTests;

public sealed class PricingCalculatorTests
{
    [Fact]
    public void BestPersonalDiscountWins_NotStacked()
    {
        var discounts = new[]
        {
            new PersonalDiscount { Type = DiscountType.Percentage, Value = 10 },
            new PersonalDiscount { Type = DiscountType.Fixed, Value = 150 }
        };
        Assert.Equal(850m, PricingCalculator.ItemPrice(1000m, discounts));
    }

    [Fact]
    public void DiscountCannotMakePriceNegative() =>
        Assert.Equal(0m, PricingCalculator.ItemPrice(100m,
            [new PersonalDiscount { Type = DiscountType.Fixed, Value = 9999m }]));

    [Fact]
    public void PromoRoundsAwayFromZeroAtCurrencyPrecision() =>
        Assert.Equal(9.01m, PricingCalculator.ApplyPromo(10.01m, 10));

    [Fact]
    public void ZeroDiscountLeavesPriceIntact() =>
        Assert.Equal(123.45m, PricingCalculator.ItemPrice(123.45m, []));
}
