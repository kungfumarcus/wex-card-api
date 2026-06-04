namespace Wex.CardApi.Contracts;

/// <summary>Requirement #2 input: a purchase transaction (amount in USD).</summary>
public record CreateTransactionRequest(string Description, DateOnly TransactionDate, decimal Amount);
