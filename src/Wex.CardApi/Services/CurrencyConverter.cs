namespace Wex.CardApi.Services;

/// <summary>
/// Converts a USD amount to a target currency using a Treasury exchange rate.
/// Rounds to 2 decimal places, half away from zero (standard for currency).
/// </summary>
public static class CurrencyConverter
{
    public static decimal Convert(decimal amount, decimal rate) =>
        Math.Round(amount * rate, 2, MidpointRounding.AwayFromZero);
}
