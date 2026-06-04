namespace Wex.CardApi.Domain;

/// <summary>
/// A payment card with a credit limit. Identified by a server-assigned <see cref="Id"/>.
/// </summary>
public class Card
{
    public Guid Id { get; set; }

    /// <summary>Credit limit in the card's base currency (USD). Stored as numeric(19,4).</summary>
    public decimal CreditLimit { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
