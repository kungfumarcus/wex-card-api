namespace Wex.CardApi.Contracts;

/// <summary>
/// Requirement #4 output: a card's available balance (credit limit minus the sum of
/// transactions) converted to a target currency using the latest available Treasury rate.
/// </summary>
public record BalanceResponse(
    Guid CardId,
    decimal AvailableBalanceUsd,
    string TargetCurrency,
    decimal ExchangeRate,
    decimal ConvertedAvailableBalance);
