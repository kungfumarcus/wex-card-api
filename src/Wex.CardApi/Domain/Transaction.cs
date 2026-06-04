namespace Wex.CardApi.Domain;

/// <summary>
/// A purchase transaction recorded against a <see cref="Card"/>.
/// </summary>
public class Transaction
{
    public Guid Id { get; set; }

    public Guid CardId { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>Calendar date of the purchase (no time component).</summary>
    public DateOnly TransactionDate { get; set; }

    /// <summary>Amount in the card's base currency (USD). Stored as numeric(19,4).</summary>
    public decimal Amount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Card? Card { get; set; }
}
