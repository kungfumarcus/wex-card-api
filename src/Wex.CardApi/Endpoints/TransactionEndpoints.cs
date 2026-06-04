using Wex.CardApi.Contracts;

namespace Wex.CardApi.Endpoints;

public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        // Requirement #2: Store a purchase transaction against a card.
        app.MapPost("/cards/{cardId:guid}/transactions",
                (Guid cardId, CreateTransactionRequest request) => NotImplemented())
            .WithTags("Transactions")
            .WithName("CreateTransaction")
            .Produces<TransactionResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status404NotFound);

        // Requirement #3: Retrieve a transaction converted into a specified currency.
        app.MapGet("/transactions/{transactionId:guid}",
                (Guid transactionId, string currency) => NotImplemented())
            .WithTags("Transactions")
            .WithName("GetConvertedTransaction")
            .Produces<ConvertedTransactionResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            // Returned when no exchange rate exists within 6 months on/before the purchase date.
            .Produces(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    private static IResult NotImplemented() =>
        Results.Problem(statusCode: StatusCodes.Status501NotImplemented, title: "Not implemented yet.");
}
