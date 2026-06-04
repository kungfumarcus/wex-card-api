using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;

namespace Wex.CardApi.Services.Exchange;

/// <summary>
/// Reads rates from the Treasury Reporting Rates of Exchange API:
/// https://fiscaldata.treasury.gov/datasets/treasury-reporting-rates-exchange/
///
/// Both lookups request a single top row sorted by descending record_date. Historical
/// rates are immutable, so results are cached; the "latest" lookup uses a short TTL.
/// </summary>
public sealed class TreasuryExchangeRateProvider(HttpClient http, IMemoryCache cache) : IExchangeRateProvider
{
    private const string Path = "v1/accounting/od/rates_of_exchange";
    private const string Fields = "country_currency_desc,exchange_rate,record_date";

    public Task<ExchangeRate?> GetRateOnOrBeforeAsync(string currency, DateOnly date, CancellationToken ct = default)
    {
        var earliest = date.AddMonths(-6);
        var key = $"rate:onbefore:{currency}:{date:yyyy-MM-dd}";

        return cache.GetOrCreateAsync(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
            var filter =
                $"country_currency_desc:eq:{Uri.EscapeDataString(currency)}," +
                $"record_date:gte:{earliest:yyyy-MM-dd}," +
                $"record_date:lte:{date:yyyy-MM-dd}";
            return QueryTopAsync(filter, ct);
        });
    }

    public Task<ExchangeRate?> GetLatestRateAsync(string currency, CancellationToken ct = default)
    {
        var key = $"rate:latest:{currency}";

        return cache.GetOrCreateAsync(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            var filter = $"country_currency_desc:eq:{Uri.EscapeDataString(currency)}";
            return QueryTopAsync(filter, ct);
        });
    }

    public async Task<IReadOnlyList<string>> GetAvailableCurrenciesAsync(CancellationToken ct = default)
    {
        var result = await cache.GetOrCreateAsync<IReadOnlyList<string>>("currencies", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);

            // The most recent publication date in the dataset...
            var latest = await http.GetFromJsonAsync<TreasuryResponse>(
                $"{Path}?fields=record_date&sort=-record_date&page[size]=1", ct);
            var latestDate = latest?.Data?.FirstOrDefault()?.RecordDate;
            if (string.IsNullOrEmpty(latestDate))
                return Array.Empty<string>();

            // ...then every currency reported on that date.
            var rows = await http.GetFromJsonAsync<TreasuryResponse>(
                $"{Path}?fields=country_currency_desc,record_date&filter=record_date:eq:{latestDate}" +
                "&sort=country_currency_desc&page[size]=500", ct);

            return rows?.Data?
                .Select(r => r.CountryCurrencyDesc)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct()
                .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? Array.Empty<string>();
        });

        return result ?? Array.Empty<string>();
    }

    private async Task<ExchangeRate?> QueryTopAsync(string filter, CancellationToken ct)
    {
        var url =
            $"{Path}?fields={Fields}&filter={filter}&sort=-record_date&page[size]=1";

        var response = await http.GetFromJsonAsync<TreasuryResponse>(url, ct);
        var row = response?.Data?.FirstOrDefault();
        if (row is null)
            return null;

        var rate = decimal.Parse(row.ExchangeRate, CultureInfo.InvariantCulture);
        var recordDate = DateOnly.ParseExact(row.RecordDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        return new ExchangeRate(row.CountryCurrencyDesc, rate, recordDate);
    }

    // The Treasury API returns every field as a string.
    private sealed record TreasuryResponse(
        [property: JsonPropertyName("data")] List<TreasuryRate>? Data);

    private sealed record TreasuryRate(
        [property: JsonPropertyName("country_currency_desc")] string CountryCurrencyDesc,
        [property: JsonPropertyName("exchange_rate")] string ExchangeRate,
        [property: JsonPropertyName("record_date")] string RecordDate);
}
