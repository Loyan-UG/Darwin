using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Common;
using Darwin.Shared.Results;
using Darwin.Mobile.Shared.Resilience;

namespace Darwin.Mobile.Shared.Api
{
    /// <summary>
    /// Default implementation of <see cref="IApiClient"/> for the mobile apps.
    /// Responsibilities:
    /// - Configuring Authorization headers (Bearer)
    /// - JSON (de)serialization using System.Text.Json with Web defaults
    /// - Returning functional Result/Result{T} instead of throwing on HTTP errors
    /// - Extracting error messages from ProblemDetails or plain text when possible
    /// - Executing network calls under an <see cref="IRetryPolicy"/> to improve resilience
    /// 
    /// Rationale:
    /// - Mobile code must avoid throwing for common HTTP error scenarios and instead return
    ///   Result objects so UI code may present friendly messages without crashing.
    /// - ReadAsResultAsync centralizes interpretation of HTTP responses so services can be simpler.
    ///
    /// Pitfalls:
    /// - The client intentionally returns a Failed Result for 204 No Content (to distinguish
    ///   "no payload" from a typed payload). Callers that expect 204 semantics should use
    ///   PutNoContentAsync or check the well-known NoContentResultMessage constant.
    /// </summary>
    public sealed class ApiClient : IApiClient
    {
        // Public constant so callers do not rely on a magic string and can detect the NoContent case reliably.
        public const string NoContentResultMessage = "Server returned no content.";

        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _http;
        private readonly IRetryPolicy _retry;

        /// <summary>
        /// Creates a new instance bound to a pre-configured <see cref="HttpClient"/> and an <see cref="IRetryPolicy"/>.
        /// </summary>
        /// <param name="httpClient">Http client configured via IHttpClientFactory (base address, timeouts, handlers).</param>
        /// <param name="retryPolicy">Retry policy used for transient network errors. Must not be null.</param>
        public ApiClient(HttpClient httpClient, IRetryPolicy retryPolicy)
        {
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _retry = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        }

        /// <inheritdoc />
        /// <remarks>
        /// Sets or clears the Authorization: Bearer header on the underlying HttpClient.
        /// Using DefaultRequestHeaders is acceptable because the HttpClient instance is expected
        /// to be a typed/hosted client per app registration. Trim input to avoid accidental spaces.
        /// </remarks>
        public void SetBearerToken(string? accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                _http.DefaultRequestHeaders.Authorization = null;
                return;
            }

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Trim());
        }

        /// <inheritdoc />
        public async Task<Result<TResponse>> GetResultAsync<TResponse>(string route, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(route))
                return Result<TResponse>.Fail("Route is required.");

            var normalized = ApiRoutes.Normalize(route);

            try
            {
                // Entire network + parse operation is executed under the retry policy.
                return await _retry.ExecuteAsync(async token =>
                {
                    using var response = await _http.GetAsync(normalized, token).ConfigureAwait(false);
                    return await ReadAsResultAsync<TResponse>(response, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Respect cancellation semantics: propagate cancellation to caller.
                throw;
            }
            catch (Exception ex)
            {
                // Do not expose stack traces; produce a concise network error message.
                return Result<TResponse>.Fail($"Network error: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<TResponse>> PostResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(route))
                return Result<TResponse>.Fail("Route is required.");

            if (request is null)
                return Result<TResponse>.Fail("Request payload is required.");

            var normalized = ApiRoutes.Normalize(route);

            try
            {
                return await _retry.ExecuteAsync(async token =>
                {
                    using var response = await _http.PostAsJsonAsync(normalized, request, _jsonOptions, token).ConfigureAwait(false);
                    return await ReadAsResultAsync<TResponse>(response, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<TResponse>.Fail($"Network error: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<TResponse>> GetEnvelopeResultAsync<TResponse>(string route, CancellationToken ct)
        {
            var raw = await GetResultAsync<ApiEnvelope<TResponse>>(route, ct).ConfigureAwait(false);
            if (!raw.Succeeded)
                return Result<TResponse>.Fail(raw.Error ?? "Request failed.");

            var envelope = raw.Value;
            if (envelope is null)
                return Result<TResponse>.Fail("Empty response envelope from server.");

            if (!envelope.Succeeded)
            {
                var msg = !string.IsNullOrWhiteSpace(envelope.Message) ? envelope.Message : "Request failed.";
                if (!string.IsNullOrWhiteSpace(envelope.ErrorCode))
                    msg = $"{msg} (code: {envelope.ErrorCode})";
                return Result<TResponse>.Fail(msg);
            }

            if (envelope.Data is null)
                return Result<TResponse>.Fail("Response envelope did not contain data.");

            return Result<TResponse>.Ok(envelope.Data);
        }

        /// <inheritdoc />
        public async Task<Result<TResponse>> PostEnvelopeResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
        {
            var raw = await PostResultAsync<TRequest, ApiEnvelope<TResponse>>(route, request, ct).ConfigureAwait(false);
            if (!raw.Succeeded)
                return Result<TResponse>.Fail(raw.Error ?? "Request failed.");

            var envelope = raw.Value;
            if (envelope is null)
                return Result<TResponse>.Fail("Empty response envelope from server.");

            if (!envelope.Succeeded)
            {
                var msg = !string.IsNullOrWhiteSpace(envelope.Message) ? envelope.Message : "Request failed.";
                if (!string.IsNullOrWhiteSpace(envelope.ErrorCode))
                    msg = $"{msg} (code: {envelope.ErrorCode})";
                return Result<TResponse>.Fail(msg);
            }

            if (envelope.Data is null)
                return Result<TResponse>.Fail("Response envelope did not contain data.");

            return Result<TResponse>.Ok(envelope.Data);
        }

        /// <inheritdoc />
        public async Task<TResponse?> GetAsync<TResponse>(string route, CancellationToken ct)
        {
            var result = await GetResultAsync<TResponse>(route, ct).ConfigureAwait(false);
            return result.Succeeded ? result.Value : default;
        }

        /// <inheritdoc />
        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
        {
            var result = await PostResultAsync<TRequest, TResponse>(route, request, ct).ConfigureAwait(false);
            return result.Succeeded ? result.Value : default;
        }

        /// <inheritdoc />
        public async Task<Result<TResponse>> PutResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(route))
                return Result<TResponse>.Fail("Route is required.");

            if (request is null)
                return Result<TResponse>.Fail("Request payload is required.");

            var normalized = ApiRoutes.Normalize(route);

            try
            {
                return await _retry.ExecuteAsync(async token =>
                {
                    using var response = await _http.PutAsJsonAsync(normalized, request, _jsonOptions, token).ConfigureAwait(false);
                    return await ReadAsResultAsync<TResponse>(response, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<TResponse>.Fail($"Network error: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<TResponse?> PutAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
        {
            var result = await PutResultAsync<TRequest, TResponse>(route, request, ct).ConfigureAwait(false);
            return result.Succeeded ? result.Value : default;
        }

        /// <inheritdoc />
        public async Task<Result> PutNoContentAsync<TRequest>(string route, TRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(route))
                return Result.Fail("Route is required.");

            if (request is null)
                return Result.Fail("Request payload is required.");

            var normalized = ApiRoutes.Normalize(route);

            try
            {
                return await _retry.ExecuteAsync(async token =>
                {
                    using var response = await _http.PutAsJsonAsync(normalized, request, _jsonOptions, token).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        // Treat 204 (No Content) as a successful update.
                        // Some APIs may return 200/201 with empty or ignored payloads; also treat them as success.
                        return Result.Ok();
                    }

                    var errorMessage = await TryReadErrorMessageAsync(response, token).ConfigureAwait(false)
                                      ?? $"Request failed with status {(int)response.StatusCode} ({response.ReasonPhrase}).";

                    return Result.Fail(errorMessage);
                }, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result.Fail($"Network error: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads the response content into TResponse and wraps it as Result{T}.
        /// Note: For 204 No Content this returns a failed Result by design; prefer PutNoContentAsync for 204 patterns.
        /// </summary>
        private static async Task<Result<TResponse>> ReadAsResultAsync<TResponse>(HttpResponseMessage response, CancellationToken ct)
        {
            if (response is null)
                return Result<TResponse>.Fail("No HTTP response.");

            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NoContent)
                    return Result<TResponse>.Fail(NoContentResultMessage); // use constant

                try
                {
                    var payload = await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions, ct).ConfigureAwait(false);
                    if (payload is null)
                        return Result<TResponse>.Fail("Empty JSON payload from server.");

                    return Result<TResponse>.Ok(payload);
                }
                catch (JsonException ex)
                {
                    return Result<TResponse>.Fail($"Invalid JSON payload: {ex.Message}");
                }
            }

            var errorMessage = await TryReadErrorMessageAsync(response, ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(errorMessage))
                errorMessage = $"Request failed with status {(int)response.StatusCode} ({response.ReasonPhrase}).";

            return Result<TResponse>.Fail(errorMessage);
        }

        /// <summary>
        /// Attempts to parse server error payloads into a human-readable message.
        /// Priority:
        /// 1) Darwin.Contracts.Common.ProblemDetails
        /// 2) Plain text
        /// </summary>
        private static async Task<string?> TryReadErrorMessageAsync(HttpResponseMessage response, CancellationToken ct)
        {
            try
            {
                var contentType = response.Content.Headers.ContentType?.MediaType;

                // Try ProblemDetails-like payload first (preferred).
                if (!string.IsNullOrWhiteSpace(contentType) && contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
                {
                    var pd = await response.Content.ReadFromJsonAsync<ProblemDetails>(_jsonOptions, ct).ConfigureAwait(false);
                    if (pd is not null)
                    {
                        var title = !string.IsNullOrWhiteSpace(pd.Title) ? pd.Title : "Request failed.";
                        var detail = !string.IsNullOrWhiteSpace(pd.Detail) ? pd.Detail : null;

                        return detail is null ? title : $"{title} - {detail}";
                    }
                }

                // Fallback to plain text.
                var text = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
            }
            catch
            {
                // Parsing error; swallow and return null so caller can fallback to status code messaging.
                return null;
            }
        }
    }
}