namespace Darwin.Contracts.Common;

/// <summary>
/// Standard API envelope for non-paged responses to ensure a uniform shape across endpoints.
/// </summary>
/// <typeparam name="T">Payload type.</typeparam>
public sealed class ApiEnvelope<T>
{
    /// <summary>Indicates whether the operation succeeded.</summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Optional message to be shown to the user or logged. 
    /// Keep short, localized server-side if applicable.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>Optional machine-readable error code for clients.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>Payload value in case of success; null on failures.</summary>
    public T? Data { get; init; }

    /// <summary>Create a success envelope.</summary>
    public static ApiEnvelope<T> Ok(T data, string? message = null)
        => new() { Succeeded = true, Data = data, Message = message };

    /// <summary>Create a failure envelope with optional error code.</summary>
    public static ApiEnvelope<T> Fail(string message, string? errorCode = null)
        => new() { Succeeded = false, Message = message, ErrorCode = errorCode };
}
