namespace Wex.CardApi.Contracts;

/// <summary>Requirement #1 input: create a card with a credit limit (USD).</summary>
public record CreateCardRequest(decimal CreditLimit);
