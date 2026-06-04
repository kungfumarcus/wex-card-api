using Wex.CardApi.Contracts;

namespace Wex.CardApi.Endpoints;

public static class CardEndpoints
{
    public static IEndpointRouteBuilder MapCardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/cards").WithTags("Cards");

        // Requirement #1: Create a card with a credit limit.
        group.MapPost("/", (CreateCardRequest request) => NotImplemented())
            .WithName("CreateCard")
            .Produces<CardResponse>(StatusCodes.Status201Created);

        // Requirement #4: Retrieve a card's available balance in a specified currency.
        group.MapGet("/{cardId:guid}/balance", (Guid cardId, string currency) => NotImplemented())
            .WithName("GetCardBalance")
            .Produces<BalanceResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static IResult NotImplemented() =>
        Results.Problem(statusCode: StatusCodes.Status501NotImplemented, title: "Not implemented yet.");
}
