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

namespace Darwin.Mobile.Shared.Api
{
    /// <summary>
    /// Default implementation of <see cref="IApiClient"/> for the mobile apps.
    /// </summary>
    /// <remarks>
    /// Design goals:
    /// - No exceptions as control-flow: return <see cref="Result{T}"/> for all primary methods.
    /// - Support raw DTO responses and <see cref="ApiEnvelope{T}"/> responses.
    /// - Attempt to parse server errors into a meaningful message (ProblemDetails / text).
    /// - Be strict about null-safety: never return Ok(null) for reference payloads.
    /// </remarks>
    public sealed class ApiClient : IApiClient
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _http;

        /// <summary>
        /// Creates a new instance bound to a pre-configured <see cref="HttpClient"/>.
        /// </summary>
        public ApiClient(HttpClient httpClient)
        {
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <inheritdoc />
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
                using var response = await _http.GetAsync(normalized, ct).ConfigureAwait(false);
                return await ReadAsResultAsync<TResponse>(response, ct).ConfigureAwait(false);
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
        public async Task<Result<TResponse>> PostResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(route))
                return Result<TResponse>.Fail("Route is required.");

            if (request is null)
                return Result<TResponse>.Fail("Request payload is required.");

            var normalized = ApiRoutes.Normalize(route);

            try
            {
                using var response = await _http.PostAsJsonAsync(normalized, request, _jsonOptions, ct).ConfigureAwait(false);
                return await ReadAsResultAsync<TResponse>(response, ct).ConfigureAwait(false);
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

        /// <summary>
        /// Reads the response content into <typeparamref name="TResponse"/> and wraps it as <see cref="Result{T}"/>.
        /// </summary>
        private static async Task<Result<TResponse>> ReadAsResultAsync<TResponse>(HttpResponseMessage response, CancellationToken ct)
        {
            if (response is null)
                return Result<TResponse>.Fail("No HTTP response.");

            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NoContent)
                    return Result<TResponse>.Fail("Server returned no content.");

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
        /// </summary>
        private static async Task<string?> TryReadErrorMessageAsync(HttpResponseMessage response, CancellationToken ct)
        {
            try
            {
                var contentType = response.Content.Headers.ContentType?.MediaType;

                // Try ProblemDetails-like payload first (preferred).
                if (!string.IsNullOrWhiteSpace(contentType) && contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
                {
                    // 1) Try Darwin.Contracts.Common.ProblemDetails (if server uses it)
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
                return null;
            }
        }
    }
}
