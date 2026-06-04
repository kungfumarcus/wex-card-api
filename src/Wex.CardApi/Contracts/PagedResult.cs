namespace Wex.CardApi.Contracts;

public record PagedResult<T>(int Page, int PageSize, int Total, IReadOnlyList<T> Items);
