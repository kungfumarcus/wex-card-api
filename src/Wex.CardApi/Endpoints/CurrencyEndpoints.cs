using Wex.CardApi.Services.Exchange;

namespace Wex.CardApi.Endpoints;

public static class CurrencyEndpoints
{
    public static IEndpointRouteBuilder MapCurrencyEndpoints(this IEndpointRouteBuilder app)
    {
        // Currencies available for conversion, sourced live from the Treasury dataset.
        app.MapGet("/currencies", async (IExchangeRateProvider rates, CancellationToken ct) =>
                Results.Ok(await rates.GetAvailableCurrenciesAsync(ct)))
            .WithTags("Currencies")
            .WithName("GetCurrencies")
            .Produces<IReadOnlyList<string>>(StatusCodes.Status200OK);

        return app;
    }
}
