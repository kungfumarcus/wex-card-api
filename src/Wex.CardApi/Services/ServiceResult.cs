namespace Wex.CardApi.Services;

public enum ServiceError
{
    None,
    NotFound,
    RateUnavailable
}

/// <summary>
/// A lightweight outcome wrapper so services can report "not found" or "no rate"
/// without throwing, and endpoints can map cleanly to HTTP status codes.
/// </summary>
public readonly record struct ServiceResult<T>(T? Value, ServiceError Error)
{
    public static ServiceResult<T> Success(T value) => new(value, ServiceError.None);
    public static ServiceResult<T> Fail(ServiceError error) => new(default, error);
}
