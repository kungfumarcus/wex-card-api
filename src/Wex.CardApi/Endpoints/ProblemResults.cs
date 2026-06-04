namespace Wex.CardApi.Endpoints;

internal static class ProblemResults
{
    /// <summary>
    /// 422 used when no exchange rate is available to convert (Requirement #3's required error,
    /// and the equivalent case for Requirement #4).
    /// </summary>
    public static IResult RateUnavailable(string subject, string currency) =>
        Results.Problem(
            statusCode: StatusCodes.Status422UnprocessableEntity,
            title: "Currency conversion unavailable",
            detail: $"{subject} cannot be converted to the target currency '{currency}'.");
}
