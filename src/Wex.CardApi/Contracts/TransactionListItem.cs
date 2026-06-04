namespace Wex.CardApi.Contracts;

/// <summary>
/// A row in the transactions list. Conversion fields are populated only when a target currency
/// is requested; <see cref="ExchangeRate"/> and <see cref="ConvertedAmount"/> are null for a row
/// that can't be converted (no rate within 6 months of its purchase date).
/// </summary>
public record TransactionListItem(
    Guid Id,
    string Description,
    DateOnly TransactionDate,
    decimal Amount,
    string? TargetCurrency,
    decimal? ExchangeRate,
    decimal? ConvertedAmount);
