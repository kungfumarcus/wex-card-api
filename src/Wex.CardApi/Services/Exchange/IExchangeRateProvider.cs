namespace Wex.CardApi.Services.Exchange;

/// <summary>A single exchange rate observation from the Treasury dataset.</summary>
public record ExchangeRate(string Currency, decimal Rate, DateOnly RecordDate);

public interface IExchangeRateProvider
{
    /// <summary>
    /// The most recent rate dated on or before <paramref name="date"/>, within the prior
    /// 6 months. Returns null if no rate exists in that window (Requirement #3).
    /// </summary>
    Task<ExchangeRate?> GetRateOnOrBeforeAsync(string currency, DateOnly date, CancellationToken ct = default);

    /// <summary>
    /// The latest available rate for the currency. Returns null if the currency is unknown
    /// or no rate exists (Requirement #4).
    /// </summary>
    Task<ExchangeRate?> GetLatestRateAsync(string currency, CancellationToken ct = default);

    /// <summary>
    /// The currencies available for conversion, as reported in the most recent Treasury
    /// publication (their <c>country_currency_desc</c> values, sorted).
    /// </summary>
    Task<IReadOnlyList<string>> GetAvailableCurrenciesAsync(CancellationToken ct = default);
}
