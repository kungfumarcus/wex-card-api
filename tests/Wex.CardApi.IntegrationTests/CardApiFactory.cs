using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Xunit;

namespace Wex.CardApi.IntegrationTests;

/// <summary>
/// Hosts the API in-process and points it at a real, throwaway PostgreSQL container
/// created per test run. The container is the only external dependency the tests need
/// (the reviewer just needs Docker running). The Treasury API will be faked separately
/// once the conversion endpoints exist, so integration tests stay deterministic.
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
    }

    public Task InitializeAsync() => _db.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _db.DisposeAsync();
        await base.DisposeAsync();
    }
}
