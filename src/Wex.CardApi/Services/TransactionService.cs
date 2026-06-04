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
}
