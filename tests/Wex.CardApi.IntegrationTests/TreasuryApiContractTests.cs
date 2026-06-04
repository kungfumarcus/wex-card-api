using Microsoft.Extensions.Caching.Memory;
using Wex.CardApi.Services.Exchange;
using Xunit;

namespace Wex.CardApi.IntegrationTests;

/// <summary>
/// Validates that our assumptions about the live Treasury Reporting Rates of Exchange API still
/// hold: the endpoint is reachable, the JSON field names match our DTO, and the values parse.
/// These run against the real API and are marked Explicit, so a normal "run all"
/// (plain <c>dotnet test</c>) skips them. They run when selected by a filter, and need
/// network access rather than Docker:
/// <code>dotnet test --filter "FullyQualifiedName~TreasuryApiContractTests"</code>
/// </summary>
public class TreasuryApiContractTests
{
    private static TreasuryExchangeRateProvider CreateProvider()
    {
        var http = new HttpClient
        {
            BaseAddress = new Uri("https://api.fiscaldata.treasury.gov/services/api/fiscal_service/")
        };
        return new TreasuryExchangeRateProvider(http, new MemoryCache(new MemoryCacheOptions()));
    }

    [Fact(Explicit = true)]
    public async Task Latest_rate_for_a_known_currency_parses()
    {
        var provider = CreateProvider();

        var rate = await provider.GetLatestRateAsync("Canada-Dollar", Xunit.TestContext.Current.CancellationToken);

        Assert.NotNull(rate);
        Assert.Equal("Canada-Dollar", rate!.Currency);
        Assert.True(rate.Rate > 0, "Expected a positive exchange rate.");
        Assert.True(rate.RecordDate > new DateOnly(2000, 1, 1), "Expected a plausible record date.");
    }

    [Fact(Explicit = true)]
    public async Task Historical_rate_is_within_the_six_month_window()
    {
        var provider = CreateProvider();
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        var rate = await provider.GetRateOnOrBeforeAsync("Canada-Dollar", asOf, Xunit.TestContext.Current.CancellationToken);

        Assert.NotNull(rate);
        Assert.True(rate!.RecordDate <= asOf, "Rate must be dated on or before the requested date.");
        Assert.True(rate.RecordDate >= asOf.AddMonths(-6),
            "A major currency should have a Treasury rate within the last 6 months.");
    }

    [Fact(Explicit = true)]
    public async Task Unknown_currency_returns_null()
    {
        var provider = CreateProvider();

        var rate = await provider.GetLatestRateAsync("Atlantis-Doubloon", Xunit.TestContext.Current.CancellationToken);

        Assert.Null(rate);
    }
}
