namespace AzureAI.Api.Models;

/// <summary>Uniform HTTP response envelope returned by all endpoints.</summary>
public sealed record ApiResponse<T>(
    bool Success,
    T? Data,
    string? Error = null,
    IReadOnlyList<string>? ValidationErrors = null);

/// <summary>Factory helpers for building <see cref="ApiResponse{T}"/> instances.</summary>
public static class ApiResponse
{
    /// <summary>Wraps a successful result.</summary>
    public static ApiResponse<T> Ok<T>(T data) => new(true, data);

    /// <summary>Wraps a failure with a single error message.</summary>
    public static ApiResponse<object?> Fail(string error) => new(false, null, error);

    /// <summary>Wraps a validation failure with per-field messages.</summary>
    public static ApiResponse<object?> ValidationFail(IEnumerable<string> errors) =>
        new(false, null, "Validation failed.", errors.ToList());
}
