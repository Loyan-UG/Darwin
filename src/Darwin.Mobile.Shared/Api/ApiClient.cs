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
using Darwin.Mobile.Shared.Security;
using Darwin.Mobile.Shared.Services;
using Microsoft.Extensions.DependencyInjection;

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
        private const int MaxErrorMessageLength = 512;
        private const int MaxPlainTextErrorBytes = 4096;
        private const string NetworkFailureMessage = "Network error while contacting the server.";

        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _http;
        private readonly IRetryPolicy _retry;
        private readonly ITokenStore _tokenStore;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeProvider _timeProvider;
        private string? _explicitAccessToken;

        /// <summary>
        /// Creates a new instance bound to a pre-configured <see cref="HttpClient"/>,
        /// an <see cref="IRetryPolicy"/>, and persistent <see cref="ITokenStore"/>.
        /// </summary>
        public ApiClient(
            HttpClient httpClient,
            IRetryPolicy retryPolicy,
            ITokenStore tokenStore,
            IServiceProvider serviceProvider,
            TimeProvider timeProvider)
        {
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _retry = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
            _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        /// <inheritdoc />
        public void SetBearerToken(string? accessToken)
        {
            _explicitAccessToken = string.IsNullOrWhiteSpace(accessToken) ? null : accessToken.Trim();
        }

        /// <inheritdoc />
        public async Task<Result<TResponse>> GetResultAsync<TResponse>(string route, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(route))
                return Result<TResponse>.Fail("Route is required.");

            var normalized = ApiRoutes.Normalize(route);

            try
            {
                return await _retry.ExecuteAsync(async token =>
                {
                    using var response = await SendWithAuthenticationRetryAsync(
                        normalized,
                        (authorization, retryToken) => SendAsync(HttpMethod.Get, normalized, authorization, retryToken),
                        token).ConfigureAwait(false);

                    return await ReadAsResultAsync<TResponse>(response, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                return Result<TResponse>.Fail(NetworkFailureMessage);
            }
        }

        /// <inheritdoc />
        public async Task<Result<string>> GetStringResultAsync(string route, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(route))
                return Result<string>.Fail("Route is required.");

            var normalized = ApiRoutes.Normalize(route);

            try
            {
                return await _retry.ExecuteAsync(async token =>
                {
                    using var response = await SendWithAuthenticationRetryAsync(
                        normalized,
                        (authorization, retryToken) => SendAsync(HttpMethod.Get, normalized, authorization, retryToken),
                        token).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == HttpStatusCode.NoContent)
                            return Result<string>.Fail(NoContentResultMessage);

                        var content = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                        return string.IsNullOrEmpty(content)
                            ? Result<string>.Fail("Empty text payload from server.")
                            : Result<string>.Ok(content);
                    }

                    var errorMessage = await TryReadErrorMessageAsync(response, token).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(errorMessage))
                        errorMessage = $"Request failed with status {(int)response.StatusCode} ({response.ReasonPhrase}).";

                    return Result<string>.Fail(errorMessage);
                }, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                return Result<string>.Fail(NetworkFailureMessage);
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
                    using var response = await SendWithAuthenticationRetryAsync(
                        normalized,
                        (authorization, retryToken) => SendJsonAsync(HttpMethod.Post, normalized, request, authorization, retryToken),
                        token).ConfigureAwait(false);

                    return await ReadAsResultAsync<TResponse>(response, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                return Result<TResponse>.Fail(NetworkFailureMessage);
            }
        }

        /// <inheritdoc />
        public async Task<Result<TResponse>> GetEnvelopeResultAsync<TResponse>(string route, CancellationToken ct)
        {
            var raw = await GetResultAsync<ApiEnvelope<TResponse>>(route, ct).ConfigureAwait(false);
            if (!raw.Succeeded || raw.Value is null)
                return Result<TResponse>.Fail(raw.Error ?? "Envelope request failed.");

            var envelope = raw.Value;
            if (!envelope.Success)
                return Result<TResponse>.Fail(envelope.Message ?? "Request failed.");

            if (envelope.Data is null)
                return Result<TResponse>.Fail("Response envelope did not contain data.");

            return Result<TResponse>.Ok(envelope.Data);
        }


        /// <inheritdoc />
        public async Task<Result<TResponse>> PostEnvelopeResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
        {
            var raw = await PostResultAsync<TRequest, ApiEnvelope<TResponse>>(route, request, ct).ConfigureAwait(false);
            if (!raw.Succeeded || raw.Value is null)
                return Result<TResponse>.Fail(raw.Error ?? "Envelope request failed.");

            var envelope = raw.Value;
            if (!envelope.Success)
                return Result<TResponse>.Fail(envelope.Message ?? "Request failed.");

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
                    using var response = await SendWithAuthenticationRetryAsync(
                        normalized,
                        (authorization, retryToken) => SendJsonAsync(HttpMethod.Put, normalized, request, authorization, retryToken),
                        token).ConfigureAwait(false);

                    return await ReadAsResultAsync<TResponse>(response, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                return Result<TResponse>.Fail(NetworkFailureMessage);
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
                    using var response = await SendWithAuthenticationRetryAsync(
                        normalized,
                        (authorization, retryToken) => SendJsonAsync(HttpMethod.Put, normalized, request, authorization, retryToken),
                        token).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
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
            catch (Exception)
            {
                return Result.Fail(NetworkFailureMessage);
            }
        }

        /// <inheritdoc />
        public async Task<Result> PostNoContentAsync<TRequest>(string route, TRequest request, CancellationToken ct)
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
                    using var response = await SendWithAuthenticationRetryAsync(
                        normalized,
                        (authorization, retryToken) => SendJsonAsync(HttpMethod.Post, normalized, request, authorization, retryToken),
                        token).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
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
            catch (Exception)
            {
                return Result.Fail(NetworkFailureMessage);
            }
        }

        private async Task<AuthenticationHeaderValue?> ResolveAuthorizationAsync(string route, CancellationToken ct)
        {
            var (accessToken, expiresAtUtc) = await _tokenStore.GetAccessAsync().ConfigureAwait(false);
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

            if (!string.IsNullOrWhiteSpace(accessToken) &&
                (!expiresAtUtc.HasValue || expiresAtUtc.Value > nowUtc))
            {
                return new AuthenticationHeaderValue("Bearer", accessToken);
            }

            if (RouteRequiresAuthentication(route) && await TryRefreshSessionAsync(ct).ConfigureAwait(false))
            {
                var refreshed = await _tokenStore.GetAccessAsync().ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(refreshed.AccessToken) &&
                    (!refreshed.AccessExpiresUtc.HasValue || refreshed.AccessExpiresUtc.Value > _timeProvider.GetUtcNow().UtcDateTime))
                {
                    return new AuthenticationHeaderValue("Bearer", refreshed.AccessToken);
                }
            }

            if (!string.IsNullOrWhiteSpace(_explicitAccessToken) &&
                (!expiresAtUtc.HasValue || expiresAtUtc.Value > nowUtc))
            {
                return new AuthenticationHeaderValue("Bearer", _explicitAccessToken);
            }

            return null;
        }

        private async Task<HttpResponseMessage> SendWithAuthenticationRetryAsync(
            string route,
            Func<AuthenticationHeaderValue?, CancellationToken, Task<HttpResponseMessage>> sendAsync,
            CancellationToken ct)
        {
            var authorization = await ResolveAuthorizationAsync(route, ct).ConfigureAwait(false);
            var response = await sendAsync(authorization, ct).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.Unauthorized || !RouteRequiresAuthentication(route))
            {
                return response;
            }

            var refreshed = await TryRefreshSessionAsync(ct).ConfigureAwait(false);
            if (!refreshed)
            {
                return response;
            }

            response.Dispose();
            authorization = await ResolveAuthorizationAsync(route, ct).ConfigureAwait(false);
            return await sendAsync(authorization, ct).ConfigureAwait(false);
        }

        private async Task<HttpResponseMessage> SendAsync(
            HttpMethod method,
            string route,
            AuthenticationHeaderValue? authorization,
            CancellationToken ct)
        {
            using var request = CreateRequest(method, route, authorization);
            return await _http.SendAsync(request, ct).ConfigureAwait(false);
        }

        private async Task<HttpResponseMessage> SendJsonAsync<TRequest>(
            HttpMethod method,
            string route,
            TRequest payload,
            AuthenticationHeaderValue? authorization,
            CancellationToken ct)
        {
            using var request = CreateRequest(method, route, authorization);
            request.Content = JsonContent.Create(payload, options: _jsonOptions);
            return await _http.SendAsync(request, ct).ConfigureAwait(false);
        }

        private static HttpRequestMessage CreateRequest(
            HttpMethod method,
            string route,
            AuthenticationHeaderValue? authorization)
        {
            var request = new HttpRequestMessage(method, route);
            if (authorization is not null)
            {
                request.Headers.Authorization = authorization;
            }

            return request;
        }

        private async Task<bool> TryRefreshSessionAsync(CancellationToken ct)
        {
            var authService = _serviceProvider.GetService<IAuthService>();
            if (authService is null)
            {
                return false;
            }

            try
            {
                return await authService.TryRefreshAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return false;
            }
        }

        private static bool RouteRequiresAuthentication(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
            {
                return false;
            }

            var normalized = ApiRoutes.Normalize(route);

            if (normalized.StartsWith("api/v1/public/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(normalized, ApiRoutes.Auth.Login, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, ApiRoutes.Auth.Refresh, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, ApiRoutes.Auth.Register, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, ApiRoutes.Auth.RequestPasswordReset, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, ApiRoutes.Auth.ResetPassword, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, ApiRoutes.Auth.RequestEmailConfirmation, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, ApiRoutes.BusinessAuth.PreviewInvitation, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, ApiRoutes.BusinessAuth.AcceptInvitation, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, ApiRoutes.Meta.Health, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, ApiRoutes.Meta.Info, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return normalized.StartsWith("api/v1/member/", StringComparison.OrdinalIgnoreCase) ||
                   normalized.StartsWith("api/v1/business/", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<Result<TResponse>> ReadAsResultAsync<TResponse>(HttpResponseMessage response, CancellationToken ct)
        {
            if (response is null)
                return Result<TResponse>.Fail("No HTTP response.");

            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NoContent)
                    return Result<TResponse>.Fail(NoContentResultMessage);

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

        private static async Task<string?> TryReadErrorMessageAsync(HttpResponseMessage response, CancellationToken ct)
        {
            try
            {
                var contentType = response.Content.Headers.ContentType?.MediaType;

                if (!string.IsNullOrWhiteSpace(contentType) &&
                    contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
                {
                    var pd = await response.Content.ReadFromJsonAsync<ProblemDetails>(_jsonOptions, ct).ConfigureAwait(false);
                    if (pd is not null)
                    {
                        var problemMessage = SanitizeErrorMessage(pd.Detail) ?? SanitizeErrorMessage(pd.Title);
                        if (!string.IsNullOrWhiteSpace(problemMessage))
                        {
                            return problemMessage;
                        }
                    }
                }

                if (!CanReadPlainTextError(response, contentType))
                {
                    return null;
                }

                var text = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return SanitizeErrorMessage(text);
            }
            catch
            {
                // Best-effort parser: ignore payload parsing issues and let caller
                // fallback to generic HTTP status based messaging.
            }

            return null;
        }

        private static bool CanReadPlainTextError(HttpResponseMessage response, string? contentType)
        {
            if (!string.IsNullOrWhiteSpace(contentType) &&
                contentType.Contains("html", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var contentLength = response.Content.Headers.ContentLength;
            return !contentLength.HasValue || contentLength.Value <= MaxPlainTextErrorBytes;
        }

        private static string? SanitizeErrorMessage(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return null;
            }

            var trimmed = message.Trim();
            if (trimmed.Length <= MaxErrorMessageLength)
            {
                return trimmed;
            }

            return trimmed[..MaxErrorMessageLength].TrimEnd() + "...";
        }

        private sealed class ApiEnvelope<T>
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public T? Data { get; set; }
        }
    }
}

