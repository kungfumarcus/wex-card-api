using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Wex.CardApi.Services.Exchange;
using Xunit;

namespace Wex.CardApi.UnitTests;

public class TreasuryExchangeRateProviderTests
{
    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    private static TreasuryExchangeRateProvider CreateSut(StubHttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://treasury.test/") },
            new MemoryCache(new MemoryCacheOptions()));

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task GetRateOnOrBefore_builds_six_month_window_and_parses_rate()
    {
        var handler = new StubHttpMessageHandler(_ => Json(
            """{"data":[{"country_currency_desc":"Canada-Dollar","exchange_rate":"1.3650","record_date":"2024-06-30"}]}"""));
        var sut = CreateSut(handler);

        var result = await sut.GetRateOnOrBeforeAsync("Canada-Dollar", new DateOnly(2024, 7, 15), TestCancellationToken);

        Assert.NotNull(result);
        Assert.Equal(1.3650m, result!.Rate);
        Assert.Equal(new DateOnly(2024, 6, 30), result.RecordDate);

        var url = Uri.UnescapeDataString(handler.LastRequest!.RequestUri!.ToString());
        Assert.Contains("country_currency_desc:eq:Canada-Dollar", url);
        Assert.Contains("record_date:lte:2024-07-15", url);     // on or before the purchase date
        Assert.Contains("record_date:gte:2024-01-15", url);     // within the prior 6 months
        Assert.Contains("sort=-record_date", url);              // newest first
    }

    [Fact]
    public async Task GetLatestRate_has_no_date_bounds()
    {
        var handler = new StubHttpMessageHandler(_ => Json(
            """{"data":[{"country_currency_desc":"Canada-Dollar","exchange_rate":"1.40","record_date":"2025-12-31"}]}"""));
        var sut = CreateSut(handler);

        var result = await sut.GetLatestRateAsync("Canada-Dollar", TestCancellationToken);

        Assert.NotNull(result);
        Assert.Equal(1.40m, result!.Rate);

        var url = Uri.UnescapeDataString(handler.LastRequest!.RequestUri!.ToString());
        Assert.Contains("country_currency_desc:eq:Canada-Dollar", url);
        Assert.DoesNotContain("record_date:lte", url);
        Assert.DoesNotContain("record_date:gte", url);
    }

    [Fact]
    public async Task Returns_null_when_no_rows()
    {
        var sut = CreateSut(new StubHttpMessageHandler(_ => Json("""{"data":[]}""")));

        var result = await sut.GetRateOnOrBeforeAsync("Nowhere-Coin", new DateOnly(2024, 7, 15), TestCancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task Encodes_currency_with_spaces()
    {
        var handler = new StubHttpMessageHandler(_ => Json(
            """{"data":[{"country_currency_desc":"Euro Zone-Euro","exchange_rate":"0.92","record_date":"2025-12-31"}]}"""));
        var sut = CreateSut(handler);

        await sut.GetLatestRateAsync("Euro Zone-Euro", TestCancellationToken);

        // The escaped (wire) form keeps the percent-encoding; ToString() would decode it.
        Assert.Contains("Euro%20Zone-Euro", handler.LastRequest!.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetAvailableCurrencies_lists_currencies_for_the_latest_date()
    {
        // The list query selects country_currency_desc; the latest-date probe does not.
        var handler = new StubHttpMessageHandler(req =>
            req.RequestUri!.Query.Contains("country_currency_desc")
                ? Json("""{"data":[{"country_currency_desc":"Canada-Dollar","record_date":"2025-12-31"},{"country_currency_desc":"Euro Zone-Euro","record_date":"2025-12-31"}]}""")
                : Json("""{"data":[{"record_date":"2025-12-31"}]}"""));
        var sut = CreateSut(handler);

        var list = await sut.GetAvailableCurrenciesAsync(TestCancellationToken);

        Assert.Contains("Canada-Dollar", list);
        Assert.Contains("Euro Zone-Euro", list);
    }
}
