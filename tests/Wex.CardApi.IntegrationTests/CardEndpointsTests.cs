using System.Net;
using System.Net.Http.Json;
using Wex.CardApi.Contracts;
using Xunit;

namespace Wex.CardApi.IntegrationTests;

public class CardEndpointsTests(CardApiFactory factory) : IClassFixture<CardApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly CancellationToken CancellationToken = Xunit.TestContext.Current.CancellationToken;

    [Fact]
    public async Task Create_card_returns_201_with_generated_id()
    {
        var response = await _client.PostAsJsonAsync("/cards", new CreateCardRequest(1000m), CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var card = await response.Content.ReadFromJsonAsync<CardResponse>(CancellationToken);
        Assert.NotNull(card);
        Assert.NotEqual(Guid.Empty, card!.Id);
        Assert.Equal(1000m, card.CreditLimit);
    }

    [Fact]
    public async Task Create_card_with_negative_limit_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/cards", new CreateCardRequest(-1m), CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Balance_for_unknown_card_returns_404()
    {
        var response = await _client.GetAsync(
            $"/cards/{Guid.NewGuid()}/balance?currency=Canada-Dollar",
            CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Balance_subtracts_transactions_and_converts_at_latest_rate()
    {
        var card = await CreateCardAsync(1000m);
        await AddTransactionAsync(card.Id, 200m, new DateOnly(2024, 6, 1));
        await AddTransactionAsync(card.Id, 300m, new DateOnly(2024, 6, 2));

        var response = await _client.GetAsync(
            $"/cards/{card.Id}/balance?currency=Canada-Dollar",
            CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<BalanceResponse>(Xunit.TestContext.Current.CancellationToken);
        Assert.Equal(500m, body!.AvailableBalanceUsd);                  // 1000 - (200 + 300)
        Assert.Equal(FakeExchangeRateProvider.LatestRate, body.ExchangeRate);
        Assert.Equal(750.00m, body.ConvertedAvailableBalance);          // 500 * 1.5
    }

    [Fact]
    public async Task Balance_with_unconvertible_currency_returns_422()
    {
        var card = await CreateCardAsync(100m);

        var response = await _client.GetAsync(
            $"/cards/{card.Id}/balance?currency={FakeExchangeRateProvider.UnknownCurrency}",
            CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Balance_without_currency_returns_400()
    {
        var card = await CreateCardAsync(100m);

        var response = await _client.GetAsync(
            $"/cards/{card.Id}/balance", 
            Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<CardResponse> CreateCardAsync(decimal limit)
    {
        var response = await _client.PostAsJsonAsync("/cards", new CreateCardRequest(limit), CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CardResponse>())!;
    }

    private async Task AddTransactionAsync(Guid cardId, decimal amount, DateOnly date)
    {
        var response = await _client.PostAsJsonAsync(
            $"/cards/{cardId}/transactions", 
            new CreateTransactionRequest("test purchase", date, amount),
            CancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
