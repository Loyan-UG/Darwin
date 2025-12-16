using System.Threading;
using System.Threading.Tasks;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Shared.Api
{
    /// <summary>
    /// Minimal HTTP abstraction for the mobile apps with bearer handling and JSON (de)serialization.
    /// </summary>
    /// <remarks>
    /// The client is "Result-first" to avoid exceptions as control-flow and to keep call-sites concise.
    /// It also supports endpoints that return either raw DTOs or ApiEnvelope&lt;T&gt;.
    /// </remarks>
    public interface IApiClient
    {
        /// <summary>
        /// Sets or clears the bearer token for subsequent requests.
        /// </summary>
        /// <param name="accessToken">Access token; when null/empty, Authorization header will be cleared.</param>
        void SetBearerToken(string? accessToken);

        /// <summary>
        /// Issues a GET request and returns a functional result.
        /// </summary>
        Task<Result<TResponse>> GetResultAsync<TResponse>(
            string route,
            CancellationToken ct);

        /// <summary>
        /// Issues a POST request and returns a functional result.
        /// </summary>
        Task<Result<TResponse>> PostResultAsync<TRequest, TResponse>(
            string route,
            TRequest request,
            CancellationToken ct);

        /// <summary>
        /// Issues a GET request and attempts to unwrap ApiEnvelope&lt;TResponse&gt;.
        /// </summary>
        Task<Result<TResponse>> GetEnvelopeResultAsync<TResponse>(
            string route,
            CancellationToken ct);

        /// <summary>
        /// Issues a POST request and attempts to unwrap ApiEnvelope&lt;TResponse&gt;.
        /// </summary>
        Task<Result<TResponse>> PostEnvelopeResultAsync<TRequest, TResponse>(
            string route,
            TRequest request,
            CancellationToken ct);

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
    }
}
