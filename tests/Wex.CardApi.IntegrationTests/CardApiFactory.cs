using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Wex.CardApi.Services.Exchange;
using Xunit;

namespace Wex.CardApi.IntegrationTests;

/// <summary>
/// Hosts the API in-process against a real, throwaway PostgreSQL container (Testcontainers),
/// and swaps the Treasury exchange-rate provider for a deterministic fake so the endpoint
/// tests never depend on the live API or network. Docker is the only prerequisite.
/// </summary>
public sealed class CardApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _db.GetConnectionString()
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IExchangeRateProvider>();
            services.AddSingleton<IExchangeRateProvider, FakeExchangeRateProvider>();
        });
    }

    public ValueTask InitializeAsync() => new(_db.StartAsync());

    public override async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        await base.DisposeAsync();
    }
}
