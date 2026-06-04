namespace Wex.CardApi.Contracts;

public record TransactionResponse(
    Guid Id,
    Guid CardId,
    string Description,
    DateOnly TransactionDate,
    decimal Amount);
