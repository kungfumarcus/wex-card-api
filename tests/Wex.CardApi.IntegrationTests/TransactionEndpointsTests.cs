using System.Net;
using System.Net.Http.Json;
using Wex.CardApi.Contracts;
using Xunit;

namespace Wex.CardApi.IntegrationTests;

public class TransactionEndpointsTests(CardApiFactory factory) : IClassFixture<CardApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly CancellationToken CancellationToken = Xunit.TestContext.Current.CancellationToken;

    [Fact]
    public async Task Create_transaction_for_unknown_card_returns_404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/cards/{Guid.NewGuid()}/transactions",
            new CreateTransactionRequest("coffee", new DateOnly(2024, 6, 1), 5m),
            CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_transaction_with_non_positive_amount_returns_400()
    {
        var card = await CreateCardAsync(100m);

        var response = await _client.PostAsJsonAsync(
            $"/cards/{card.Id}/transactions",
            new CreateTransactionRequest("freebie", new DateOnly(2024, 6, 1), 0m),
            CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_then_convert_transaction_uses_on_or_before_rate()
    {
        var card = await CreateCardAsync(1000m);
        var created = await CreateTransactionAsync(card.Id, 100m, new DateOnly(2024, 6, 15));

        var response = await _client.GetAsync($"/transactions/{created.Id}?currency=Canada-Dollar", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ConvertedTransactionResponse>(CancellationToken);
        Assert.Equal(created.Id, body!.Id);
        Assert.Equal(100m, body.OriginalAmount);
        Assert.Equal(FakeExchangeRateProvider.OnOrBeforeRate, body.ExchangeRate);
        Assert.Equal(200.00m, body.ConvertedAmount);                  // 100 * 2.0
        Assert.Equal("Canada-Dollar", body.TargetCurrency);
    }

    [Fact]
    public async Task Convert_unknown_transaction_returns_404()
    {
        var response = await _client.GetAsync(
            $"/transactions/{Guid.NewGuid()}?currency=Canada-Dollar",
            CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Convert_with_unconvertible_currency_returns_422()
    {
        var card = await CreateCardAsync(1000m);
        var created = await CreateTransactionAsync(card.Id, 100m, new DateOnly(2024, 6, 15));

        var response = await _client.GetAsync(
            $"/transactions/{created.Id}?currency={FakeExchangeRateProvider.UnknownCurrency}",
            CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    private async Task<CardResponse> CreateCardAsync(decimal limit)
    {
        var response = await _client.PostAsJsonAsync("/cards", new CreateCardRequest(limit), CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CardResponse>())!;
    }

    private async Task<TransactionResponse> CreateTransactionAsync(Guid cardId, decimal amount, DateOnly date)
    {
        var response = await _client.PostAsJsonAsync(
            $"/cards/{cardId}/transactions", 
            new CreateTransactionRequest("purchase", date, amount),
            CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TransactionResponse>())!;
    }
}
