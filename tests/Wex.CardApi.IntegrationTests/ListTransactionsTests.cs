using System.Net;
using System.Net.Http.Json;
using Wex.CardApi.Contracts;
using Xunit;

namespace Wex.CardApi.IntegrationTests;

public class ListTransactionsTests(CardApiFactory factory) : IClassFixture<CardApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task List_without_cardId_returns_400()
    {
        var response = await _client.GetAsync("/transactions", TestCancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_for_unknown_card_returns_404()
    {
        var response = await _client.GetAsync($"/transactions?cardId={Guid.NewGuid()}", TestCancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_defaults_to_usd_with_no_conversion()
    {
        var card = await CreateCardAsync(1000m);
        await AddTransactionAsync(card.Id, 10m, new DateOnly(2024, 6, 1));
        await AddTransactionAsync(card.Id, 20m, new DateOnly(2024, 6, 2));

        var page = await GetPageAsync($"/transactions?cardId={card.Id}");

        Assert.Equal(2, page!.Total);
        Assert.All(page.Items, i => Assert.Null(i.ConvertedAmount));
        Assert.All(page.Items, i => Assert.Null(i.ExchangeRate));
    }

    [Fact]
    public async Task List_with_currency_converts_each_row_at_its_own_date()
    {
        var card = await CreateCardAsync(1000m);
        await AddTransactionAsync(card.Id, 100m, new DateOnly(2024, 6, 15));

        var page = await GetPageAsync($"/transactions?cardId={card.Id}&currency=Canada-Dollar");

        var item = Assert.Single(page!.Items);
        Assert.Equal(FakeExchangeRateProvider.OnOrBeforeRate, item.ExchangeRate!.Value);
        Assert.Equal(200.00m, item.ConvertedAmount!.Value);   // 100 * 2.0
        Assert.Equal("Canada-Dollar", item.TargetCurrency);
    }

    [Fact]
    public async Task List_pages_results()
    {
        var card = await CreateCardAsync(1000m);
        for (var i = 0; i < 3; i++)
            await AddTransactionAsync(card.Id, 5m, new DateOnly(2024, 6, 1).AddDays(i));

        var page1 = await GetPageAsync($"/transactions?cardId={card.Id}&page=1&pageSize=2");
        var page2 = await GetPageAsync($"/transactions?cardId={card.Id}&page=2&pageSize=2");

        Assert.Equal(3, page1!.Total);
        Assert.Equal(2, page1.Items.Count);
        Assert.Single(page2!.Items);
    }

    private async Task<PagedResult<TransactionListItem>?> GetPageAsync(string url)
    {
        var response = await _client.GetAsync(url, TestCancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PagedResult<TransactionListItem>>(TestCancellationToken);
    }

    private async Task<CardResponse> CreateCardAsync(decimal limit)
    {
        var response = await _client.PostAsJsonAsync("/cards", new CreateCardRequest(limit), TestCancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CardResponse>(TestCancellationToken))!;
    }

    private async Task AddTransactionAsync(Guid cardId, decimal amount, DateOnly date)
    {
        var response = await _client.PostAsJsonAsync(
            $"/cards/{cardId}/transactions", new CreateTransactionRequest("test purchase", date, amount), TestCancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
