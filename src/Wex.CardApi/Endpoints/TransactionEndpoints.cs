using Wex.CardApi.Contracts;
using Wex.CardApi.Services;

namespace Wex.CardApi.Endpoints;

public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        // List a card's transactions (paged), optionally converted to a target currency.
        app.MapGet("/transactions",
                async (Guid? cardId, int? page, int? pageSize, string? currency,
                       TransactionService service, CancellationToken ct) =>
                {
                    if (cardId is null)
                        return Results.ValidationProblem(new Dictionary<string, string[]>
                        {
                            ["cardId"] = ["The 'cardId' query parameter is required."]
                        });

                    var result = await service.ListAsync(cardId.Value, page ?? 1, pageSize ?? 10, currency, ct);
                    return result.Error switch
                    {
                        ServiceError.None => Results.Ok(result.Value),
                        ServiceError.NotFound => Results.NotFound(),
                        _ => Results.Problem()
                    };
                })
            .WithTags("Transactions")
            .WithName("ListTransactions")
            .Produces<PagedResult<TransactionListItem>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        // Requirement #2: Store a purchase transaction against a card.
        app.MapPost("/cards/{cardId:guid}/transactions",
                async (Guid cardId, CreateTransactionRequest request, TransactionService service, CancellationToken ct) =>
                {
                    var errors = Validate(request);
                    if (errors.Count > 0)
                        return Results.ValidationProblem(errors);

                    var result = await service.CreateAsync(cardId, request, ct);
                    return result.Error switch
                    {
                        ServiceError.None => Results.Created($"/transactions/{result.Value!.Id}", result.Value),
                        ServiceError.NotFound => Results.NotFound(),
                        _ => Results.Problem()
                    };
                })
            .WithTags("Transactions")
            .WithName("CreateTransaction")
            .Produces<TransactionResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        // Requirement #3: Retrieve a transaction converted into a specified currency.
        app.MapGet("/transactions/{transactionId:guid}",
                async (Guid transactionId, string? currency, TransactionService service, CancellationToken ct) =>
                {
                    if (string.IsNullOrWhiteSpace(currency))
                        return Results.ValidationProblem(new Dictionary<string, string[]>
                        {
                            ["currency"] = ["The 'currency' query parameter is required."]
                        });

                    var result = await service.GetConvertedAsync(transactionId, currency, ct);
                    return result.Error switch
                    {
                        ServiceError.None => Results.Ok(result.Value),
                        ServiceError.NotFound => Results.NotFound(),
                        ServiceError.RateUnavailable => ProblemResults.RateUnavailable("The transaction", currency),
                        _ => Results.Problem()
                    };
                })
            .WithTags("Transactions")
            .WithName("GetConvertedTransaction")
            .Produces<ConvertedTransactionResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status422UnprocessableEntity)
            .ProducesValidationProblem();

        return app;
    }

    private static Dictionary<string, string[]> Validate(CreateTransactionRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Description))
            errors["description"] = ["Description is required."];
        else if (request.Description.Length > 200)
            errors["description"] = ["Description must be 200 characters or fewer."];

        if (request.Amount <= 0)
            errors["amount"] = ["Amount must be greater than zero."];

        return errors;
    }
}
