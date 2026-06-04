using Wex.CardApi.Services.Exchange;

namespace Wex.CardApi.IntegrationTests;

/// <summary>
/// Deterministic stand-in for the Treasury API. Any currency converts at a fixed rate;
/// the sentinel <see cref="UnknownCurrency"/> has no rate, exercising the 422 path.
/// </summary>
public sealed class FakeExchangeRateProvider : IExchangeRateProvider
{
    public const string UnknownCurrency = "Nowhere-Coin";
    public const decimal OnOrBeforeRate = 2.0m;
    public const decimal LatestRate = 1.5m;

    public Task<ExchangeRate?> GetRateOnOrBeforeAsync(string currency, DateOnly date, CancellationToken ct = default)
        => Task.FromResult<ExchangeRate?>(currency == UnknownCurrency
            ? null
            : new ExchangeRate(currency, OnOrBeforeRate, date));

    public Task<ExchangeRate?> GetLatestRateAsync(string currency, CancellationToken ct = default)
        => Task.FromResult<ExchangeRate?>(currency == UnknownCurrency
            ? null
            : new ExchangeRate(currency, LatestRate, DateOnly.FromDateTime(DateTime.UtcNow)));
}
