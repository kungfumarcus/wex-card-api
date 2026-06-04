using System.Net;
using Xunit;

namespace Wex.CardApi.IntegrationTests;

public class HealthCheckTests(CardApiFactory factory) : IClassFixture<CardApiFactory>
{
    private readonly CardApiFactory _factory = factory;

    [Fact]
    public async Task Health_endpoint_returns_ok()
    {
        // Booting the client starts the app, which runs EnsureCreated() against the
        // Testcontainers Postgres. A 200 here proves the whole harness is wired up:
        // app host + real database + HTTP pipeline.
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
