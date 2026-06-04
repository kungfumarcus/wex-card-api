using Microsoft.EntityFrameworkCore;
using Wex.CardApi.Contracts;
using Wex.CardApi.Domain;
using Wex.CardApi.Infrastructure;
using Wex.CardApi.Services.Exchange;

namespace Wex.CardApi.Services;

public class TransactionService(AppDbContext db, IExchangeRateProvider rates)
{
    /// <summary>Requirement #2: store a purchase transaction against a card.</summary>
    public async Task<ServiceResult<TransactionResponse>> CreateAsync(
        Guid cardId, CreateTransactionRequest request, CancellationToken ct)
    {
        var cardExists = await db.Cards.AnyAsync(c => c.Id == cardId, ct);
        if (!cardExists)
            return ServiceResult<TransactionResponse>.Fail(ServiceError.NotFound);

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            CardId = cardId,
            Description = request.Description,
            TransactionDate = request.TransactionDate,
            Amount = request.Amount,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Transactions.Add(transaction);
        await db.SaveChangesAsync(ct);

        return ServiceResult<TransactionResponse>.Success(new TransactionResponse(
            transaction.Id, transaction.CardId, transaction.Description,
            transaction.TransactionDate, transaction.Amount));
    }

    /// <summary>
    /// Requirement #3: retrieve a transaction converted to the target currency, using the
    /// rate dated on or before the purchase date (within the prior 6 months).
    /// </summary>
    public async Task<ServiceResult<ConvertedTransactionResponse>> GetConvertedAsync(
        Guid transactionId, string currency, CancellationToken ct)
    {
        var transaction = await db.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId, ct);
        if (transaction is null)
            return ServiceResult<ConvertedTransactionResponse>.Fail(ServiceError.NotFound);

        var rate = await rates.GetRateOnOrBeforeAsync(currency, transaction.TransactionDate, ct);
        if (rate is null)
            return ServiceResult<ConvertedTransactionResponse>.Fail(ServiceError.RateUnavailable);

        var converted = CurrencyConverter.Convert(transaction.Amount, rate.Rate);
        return ServiceResult<ConvertedTransactionResponse>.Success(new ConvertedTransactionResponse(
            transaction.Id, transaction.Description, transaction.TransactionDate,
            transaction.Amount, currency, rate.Rate, converted));
    }

    /// <summary>
    /// Lists a card's transactions, newest first, paged. When <paramref name="currency"/> is set,
    /// each row is converted at the rate for its own purchase date (Requirement #3); a row with no
    /// rate in the 6-month window keeps its USD amount with null conversion fields rather than
    /// failing the whole page.
    /// </summary>
    public async Task<ServiceResult<PagedResult<TransactionListItem>>> ListAsync(
        Guid cardId, int page, int pageSize, string? currency, CancellationToken ct)
    {
        var cardExists = await db.Cards.AnyAsync(c => c.Id == cardId, ct);
        if (!cardExists)
            return ServiceResult<PagedResult<TransactionListItem>>.Fail(ServiceError.NotFound);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Transactions.Where(t => t.CardId == cardId);
        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = new List<TransactionListItem>(rows.Count);
        foreach (var t in rows)
        {
            if (string.IsNullOrWhiteSpace(currency))
            {
                items.Add(new TransactionListItem(t.Id, t.Description, t.TransactionDate, t.Amount, null, null, null));
                continue;
            }

            var rate = await rates.GetRateOnOrBeforeAsync(currency, t.TransactionDate, ct);
            items.Add(rate is null
                ? new TransactionListItem(t.Id, t.Description, t.TransactionDate, t.Amount, currency, null, null)
                : new TransactionListItem(t.Id, t.Description, t.TransactionDate, t.Amount, currency,
                    rate.Rate, CurrencyConverter.Convert(t.Amount, rate.Rate)));
        }

        return ServiceResult<PagedResult<TransactionListItem>>.Success(
            new PagedResult<TransactionListItem>(page, pageSize, total, items));
    }
}
