namespace Wex.CardApi.Contracts;

public record CardResponse(Guid Id, decimal CreditLimit, DateTimeOffset CreatedAt);
