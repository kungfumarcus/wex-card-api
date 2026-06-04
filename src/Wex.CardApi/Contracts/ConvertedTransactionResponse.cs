namespace Wex.CardApi.Contracts;

/// <summary>
/// Requirement #3 output: a stored transaction converted to a target currency using the
/// Treasury rate active on or before the purchase date (within the prior 6 months).
/// </summary>
public record ConvertedTransactionResponse(
    Guid Id,
    string Description,
    DateOnly TransactionDate,
    decimal OriginalAmount,
    string TargetCurrency,
    decimal ExchangeRate,
    decimal ConvertedAmount);
