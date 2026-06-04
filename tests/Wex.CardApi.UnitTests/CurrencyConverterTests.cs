using Wex.CardApi.Services;
using Xunit;

namespace Wex.CardApi.UnitTests;

public class CurrencyConverterTests
{
    [Fact]
    public void Multiplies_amount_by_rate()
        => Assert.Equal(13.65m, CurrencyConverter.Convert(10m, 1.365m));

    [Fact]
    public void Rounds_to_two_decimal_places()
        => Assert.Equal(15.13m, CurrencyConverter.Convert(10m, 1.5125m)); // 15.125 -> 15.13

    [Fact]
    public void Rounds_half_away_from_zero()
        => Assert.Equal(2.01m, CurrencyConverter.Convert(2.005m, 1m)); // exact in decimal -> 2.01

    [Fact]
    public void Handles_whole_results()
        => Assert.Equal(150.00m, CurrencyConverter.Convert(100m, 1.5m));

    [Fact]
    public void Handles_negative_balance()
        => Assert.Equal(-100.00m, CurrencyConverter.Convert(-50m, 2m));
}
