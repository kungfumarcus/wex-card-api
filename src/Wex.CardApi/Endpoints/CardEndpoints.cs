using Wex.CardApi.Contracts;
using Wex.CardApi.Services;

namespace Wex.CardApi.Endpoints;

public static class CardEndpoints
{
    public static IEndpointRouteBuilder MapCardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/cards").WithTags("Cards");

        // List all cards (populates the card chooser).
        group.MapGet("/", async (CardService service, CancellationToken ct) =>
                Results.Ok(await service.ListAsync(ct)))
            .WithName("ListCards")
            .Produces<IReadOnlyList<CardResponse>>(StatusCodes.Status200OK);

        // Requirement #1: Create a card with a credit limit.
        group.MapPost("/", async (CreateCardRequest request, CardService service, CancellationToken ct) =>
            {
                if (request.CreditLimit < 0)
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["creditLimit"] = ["Credit limit must be zero or greater."]
                    });

                var card = await service.CreateAsync(request.CreditLimit, ct);
                return Results.Created($"/cards/{card.Id}", card);
            })
            .WithName("CreateCard")
            .Produces<CardResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        // Requirement #4: Retrieve a card's available balance in a specified currency.
        group.MapGet("/{cardId:guid}/balance",
            async (Guid cardId, string? currency, CardService service, CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(currency))
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["currency"] = ["The 'currency' query parameter is required."]
                    });

                var result = await service.GetBalanceAsync(cardId, currency, ct);
                return result.Error switch
                {
                    ServiceError.None => Results.Ok(result.Value),
                    ServiceError.NotFound => Results.NotFound(),
                    ServiceError.RateUnavailable => ProblemResults.RateUnavailable("The available balance", currency),
                    _ => Results.Problem()
                };
            })
            .WithName("GetCardBalance")
            .Produces<BalanceResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status422UnprocessableEntity)
            .ProducesValidationProblem();

        return app;
    }
}
