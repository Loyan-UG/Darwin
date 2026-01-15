using System.Threading;
using System.Threading.Tasks;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Shared.Api
{
    /// <summary>
    /// Minimal HTTP abstraction used by the mobile apps with bearer handling and JSON (de)serialization.
    /// Design goals:
    /// - Keep call sites concise by returning functional Result/Result<T>...</T>
    /// - Avoid throwing exceptions for expected HTTP errors (except cancellations).
    /// - Provide helpers for endpoints that intentionally return 204 No Content.
    /// </summary>
    public interface IApiClient
    {
        /// <summary>
        /// Sets or clears the Authorization: Bearer header for subsequent requests.
        /// </summary>
        /// <param name="accessToken">Access token; when null/empty, the header will be cleared.</param>
        void SetBearerToken(string? accessToken);

        /// <summary>
        /// Issues a GET request and returns a functional result.
        /// </summary>
        Task<Result<TResponse>> GetResultAsync<TResponse>(string route, CancellationToken ct);

        /// <summary>
        /// Issues a POST request and returns a functional result.
        /// </summary>
        Task<Result<TResponse>> PostResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct);

        /// <summary>
        /// Issues a GET request and attempts to unwrap ApiEnvelope&lt;TResponse&gt;.
        /// </summary>
        Task<Result<TResponse>> GetEnvelopeResultAsync<TResponse>(string route, CancellationToken ct);

        /// <summary>
        /// Issues a POST request and attempts to unwrap ApiEnvelope&lt;TResponse&gt;.
        /// </summary>
        Task<Result<TResponse>> PostEnvelopeResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct);

        /// <summary>
        /// Backward-compatible GET that returns null on failures.
        /// Prefer <see cref="GetResultAsync{TResponse}"/>.
        /// </summary>
        Task<TResponse?> GetAsync<TResponse>(string route, CancellationToken ct);

        /// <summary>
        /// Backward-compatible POST that returns null on failures.
        /// Prefer <see cref="PostResultAsync{TRequest, TResponse}"/>.
        /// </summary>
        Task<TResponse?> PostAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct);

        /// <summary>
        /// Issues a PUT request and returns a functional result (deserializing a JSON body on success).
        /// Use this for endpoints that respond with a JSON payload.
        /// </summary>
        Task<Result<TResponse>> PutResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct);

        /// <summary>
        /// Backward-compatible PUT that returns null on failures.
        /// Prefer <see cref="PutResultAsync{TRequest, TResponse}"/>.
        /// </summary>
        Task<TResponse?> PutAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct);

        /// <summary>
        /// Issues a PUT request to endpoints that intentionally return 204 No Content on success.
        /// This is commonly used for optimistic concurrency "update" operations.
        /// </summary>
        /// <remarks>
        /// - Returns Result.Ok() for 204 responses.
        /// - For non-204 2xx responses, still returns Ok (body is ignored).
        /// - For non-2xx responses, attempts to parse a meaningful error (ProblemDetails/text).
        /// </remarks>
        Task<Result> PutNoContentAsync<TRequest>(string route, TRequest request, CancellationToken ct);
    }
}