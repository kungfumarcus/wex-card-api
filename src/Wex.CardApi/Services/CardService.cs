using Microsoft.EntityFrameworkCore;
using Wex.CardApi.Contracts;
using Wex.CardApi.Domain;
using Wex.CardApi.Infrastructure;
using Wex.CardApi.Services.Exchange;

namespace Wex.CardApi.Services;

public class CardService(AppDbContext db, IExchangeRateProvider rates)
{
    /// <summary>Requirement #1: create a card with a credit limit.</summary>
    public async Task<CardResponse> CreateAsync(decimal creditLimit, CancellationToken ct)
    {
        var card = new Card
        {
            Id = Guid.NewGuid(),
            CreditLimit = creditLimit,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Cards.Add(card);
        await db.SaveChangesAsync(ct);

        return new CardResponse(card.Id, card.CreditLimit, card.CreatedAt);
    }

    /// <summary>
    /// Requirement #4: available balance (credit limit minus the sum of transactions),
    /// converted to the target currency using the latest available rate.
    /// </summary>
    public async Task<ServiceResult<BalanceResponse>> GetBalanceAsync(
        Guid cardId, string currency, CancellationToken ct)
    {
        var card = await db.Cards.FirstOrDefaultAsync(c => c.Id == cardId, ct);
        if (card is null)
            return ServiceResult<BalanceResponse>.Fail(ServiceError.NotFound);

        // Sum in the database; coalesce so an empty set yields 0 rather than throwing.
        var spent = await db.Transactions
            .Where(t => t.CardId == cardId)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0m;

        var availableUsd = card.CreditLimit - spent;

        var rate = await rates.GetLatestRateAsync(currency, ct);
        if (rate is null)
            return ServiceResult<BalanceResponse>.Fail(ServiceError.RateUnavailable);

        var converted = CurrencyConverter.Convert(availableUsd, rate.Rate);
        return ServiceResult<BalanceResponse>.Success(
            new BalanceResponse(cardId, availableUsd, currency, rate.Rate, converted));
    }
}
