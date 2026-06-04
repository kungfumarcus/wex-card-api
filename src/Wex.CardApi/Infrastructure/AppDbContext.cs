using Microsoft.EntityFrameworkCore;
using Wex.CardApi.Domain;

namespace Wex.CardApi.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Card>(card =>
        {
            card.HasKey(c => c.Id);
            card.Property(c => c.CreditLimit).HasColumnType("numeric(19,4)");

            card.HasMany(c => c.Transactions)
                .WithOne(t => t.Card!)
                .HasForeignKey(t => t.CardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Transaction>(transaction =>
        {
            transaction.HasKey(t => t.Id);
            transaction.Property(t => t.Description).HasMaxLength(200).IsRequired();
            transaction.Property(t => t.Amount).HasColumnType("numeric(19,4)");
            transaction.HasIndex(t => t.CardId);
        });
    }
}
