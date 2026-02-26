using Darwin.Contracts.Identity;
using Darwin.Contracts.Meta;
using Darwin.Mobile.Shared.Api;
using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.Security;
using Darwin.Shared.Results;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Services
{
    /// <summary>
    /// Handles authentication flows (login, refresh) and token persistence,
    /// plus auxiliary account operations that are exposed by the public WebApi.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>Logs in with email/password and returns a bootstrap payload for the app.</summary>
        Task<AppBootstrapResponse> LoginAsync(string email, string password, string? deviceId, CancellationToken ct);

        /// <summary>Attempts a token refresh using the stored refresh token, if still valid.</summary>
        Task<bool> TryRefreshAsync(CancellationToken ct);

        /// <summary>Logs out this device by revoking its refresh token (if any) and clearing local storage.</summary>
        Task LogoutAsync(CancellationToken ct);

        /// <summary>Logs out from all devices by revoking all refresh tokens for the current user.</summary>
        Task<bool> LogoutAllAsync(CancellationToken ct);

        /// <summary>Registers a new consumer account.</summary>
        Task<RegisterResponse?> RegisterAsync(RegisterRequest request, CancellationToken ct);

        /// <summary>Changes the current user's password (authenticated).</summary>
        Task<bool> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct);

        /// <summary>Requests a password reset token to be sent to the specified email.</summary>
        Task<bool> RequestPasswordResetAsync(string email, CancellationToken ct);

        /// <summary>Completes a password reset using email, token and new password.</summary>
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken ct);
    }

    /// <summary>
    /// Default implementation of <see cref="IAuthService"/>.
    /// Responsibilities include:
    /// - Calling login/refresh/logout endpoints via <see cref="IApiClient"/>.
    /// - Persisting tokens via <see cref="ITokenStore"/>.
    /// - Validating returned JWT against the configured <see cref="ApiOptions.AppRole"/>.
    /// - Coordinating concurrent refresh attempts (single-flight) to avoid refresh storms.
    /// </summary>
    public sealed class AuthService : IAuthService, IDisposable
    {
        private readonly IApiClient _api;
        private readonly ITokenStore _store;
        private readonly ApiOptions _opts;
        private readonly IDeviceIdProvider _deviceIdProvider;

        // Single-flight refresh synchronization primitive.
        // SemaphoreSlim used instead of lock to allow async waiting and cancellation.
        private readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);
        private bool _disposed;

        public AuthService(IApiClient api, ITokenStore store, ApiOptions opts, IDeviceIdProvider deviceIdProvider)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _opts = opts ?? throw new ArgumentNullException(nameof(opts));
            _deviceIdProvider = deviceIdProvider ?? throw new ArgumentNullException(nameof(deviceIdProvider));
        }

        /// <inheritdoc />
        public async Task<AppBootstrapResponse> LoginAsync(string email, string password, string? deviceId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var effectiveDeviceId = await ResolveEffectiveDeviceIdAsync(deviceId).ConfigureAwait(false);

            // Use PostResultAsync so API errors (for example device-binding requirement)
            // are propagated as meaningful messages instead of collapsing to "Empty token response.".
            var tokenResult = await _api.PostResultAsync<PasswordLoginRequest, TokenResponse>(
                ApiRoutes.Auth.Login,
                new PasswordLoginRequest
                {
                    Email = email,
                    Password = password,
                    DeviceId = effectiveDeviceId
                },
                ct).ConfigureAwait(false);

            if (!tokenResult.Succeeded || tokenResult.Value is null)
            {
                var message = string.IsNullOrWhiteSpace(tokenResult.Error)
                    ? "Login failed."
                    : tokenResult.Error;

                throw new InvalidOperationException(message);
            }

            var token = tokenResult.Value;

            // Validate token shape/claims with app role (prevent cross-app logins).
            ValidateTokenForApp(token.AccessToken, _opts);

            await _store.SaveAsync(
                token.AccessToken,
                token.AccessTokenExpiresAtUtc,
                token.RefreshToken,
                token.RefreshTokenExpiresAtUtc).ConfigureAwait(false);

            _api.SetBearerToken(token.AccessToken);

            var boot = await _api.GetAsync<AppBootstrapResponse>(ApiRoutes.Meta.Bootstrap, ct).ConfigureAwait(false)
                       ?? new AppBootstrapResponse();

            // Populate runtime options from bootstrap.
            _opts.JwtAudience = boot.JwtAudience;
            _opts.QrRefreshSeconds = boot.QrTokenRefreshSeconds;
            _opts.MaxOutbox = boot.MaxOutboxItems;

            return boot;
        }

        /// <inheritdoc />
        public async Task<bool> TryRefreshAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            // Quick check: does a refresh token exist and is not expired?
            var (rt, rtex) = await _store.GetRefreshAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(rt) || rtex is null || rtex <= DateTime.UtcNow)
                return false;

            // Single-flight: ensure only one caller performs the network refresh.
            await _refreshLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                // Re-check state inside lock in case another waiter refreshed already.
                var (currentRt, currentRtex) = await _store.GetRefreshAsync().ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(currentRt) || currentRtex is null || currentRtex <= DateTime.UtcNow)
                    return false;

                // If token changed since initial read, assume refresh already completed successfully.
                if (!string.Equals(currentRt, rt, StringComparison.Ordinal))
                    return true;

                var effectiveDeviceId = await ResolveEffectiveDeviceIdAsync(deviceId: null).ConfigureAwait(false);

                // Perform refresh network call.
                var res = await _api.PostAsync<RefreshTokenRequest, TokenResponse>(
                    ApiRoutes.Auth.Refresh,
                    new RefreshTokenRequest { RefreshToken = currentRt!, DeviceId = effectiveDeviceId },
                    ct).ConfigureAwait(false);

                if (res is null)
                    return false;

                // Validate new access token against app role to avoid accepting wrong tokens.
                ValidateTokenForApp(res.AccessToken, _opts);

                // Persist tokens and apply bearer header.
                await _store.SaveAsync(res.AccessToken, res.AccessTokenExpiresAtUtc, res.RefreshToken, res.RefreshTokenExpiresAtUtc).ConfigureAwait(false);
                _api.SetBearerToken(res.AccessToken);

                return true;
            }
            catch (OperationCanceledException)
            {
                // Propagate cancellation to caller.
                throw;
            }
            catch
            {
                // Any error during refresh should be treated as 'refresh failed' (no throw).
                return false;
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        /// <inheritdoc />
        public async Task LogoutAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var (rt, _) = await _store.GetRefreshAsync().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(rt))
            {
                // Best-effort revoke; do not fail logout if revoke fails.
                _ = await _api.PostAsync<LogoutRequest, object?>(
                    ApiRoutes.Auth.Logout,
                    new LogoutRequest { RefreshToken = rt! },
                    ct).ConfigureAwait(false);
            }

            await _store.ClearAsync().ConfigureAwait(false);
            _api.SetBearerToken(null);
        }

        /// <inheritdoc />
        public async Task<bool> LogoutAllAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var result = await _api.PostResultAsync<object, object?>(
                ApiRoutes.Auth.LogoutAll,
                new { },
                ct).ConfigureAwait(false);

            if (result.Succeeded)
            {
                await _store.ClearAsync().ConfigureAwait(false);
                _api.SetBearerToken(null);
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public async Task<RegisterResponse?> RegisterAsync(RegisterRequest request, CancellationToken ct)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            ct.ThrowIfCancellationRequested();

            return await _api.PostAsync<RegisterRequest, RegisterResponse>(
                ApiRoutes.Auth.Register,
                request,
                ct).ConfigureAwait(false);
        }

        /// <inheritdoc />
        /// <inheritdoc />
        public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            // Try once with current token.
            var firstAttempt = await _api.PostResultAsync<ChangePasswordRequest, object?>(
                ApiRoutes.Auth.ChangePassword,
                new ChangePasswordRequest
                {
                    CurrentPassword = currentPassword,
                    NewPassword = newPassword
                },
                ct).ConfigureAwait(false);

            if (IsSuccessfulNoPayloadScenario(firstAttempt))
            {
                return true;
            }

            // If unauthorized, refresh token once and retry exactly one time.
            // This avoids infinite loops while still handling normal access-token expiry.
            if (LooksUnauthorized(firstAttempt.Error))
            {
                var refreshed = await TryRefreshAsync(ct).ConfigureAwait(false);
                if (!refreshed)
                {
                    return false;
                }

                var secondAttempt = await _api.PostResultAsync<ChangePasswordRequest, object?>(
                    ApiRoutes.Auth.ChangePassword,
                    new ChangePasswordRequest
                    {
                        CurrentPassword = currentPassword,
                        NewPassword = newPassword
                    },
                    ct).ConfigureAwait(false);

                return IsSuccessfulNoPayloadScenario(secondAttempt);
            }

            return false;
        }

        /// <summary>
        /// Treats endpoint responses that are semantically successful even if no JSON payload is returned.
        /// Some endpoints return 200/OK with empty body, which is acceptable for command-style actions.
        /// </summary>
        private static bool IsSuccessfulNoPayloadScenario(Result<object?> result)
        {
            if (result.Succeeded)
            {
                return true;
            }

            var error = result.Error ?? string.Empty;

            // ApiClient returns this message when status is success but JSON body is empty.
            if (error.Contains("Empty JSON payload from server.", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Defensive support for explicit 204 handling, if backend behavior changes later.
            if (error.Contains(ApiClient.NoContentResultMessage, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Detects whether a failed result indicates an authorization failure.
        /// </summary>
        private static bool LooksUnauthorized(string? error)
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                return false;
            }

            return error.Contains("401", StringComparison.OrdinalIgnoreCase) ||
                   error.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase);
        }


        /// <inheritdoc />
        public async Task<bool> RequestPasswordResetAsync(string email, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var result = await _api.PostResultAsync<RequestPasswordResetRequest, object?>(
                ApiRoutes.Auth.RequestPasswordReset,
                new RequestPasswordResetRequest { Email = email },
                ct).ConfigureAwait(false);

            return result.Succeeded;
        }

        /// <inheritdoc />
        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var result = await _api.PostResultAsync<ResetPasswordRequest, object?>(
                ApiRoutes.Auth.ResetPassword,
                new ResetPasswordRequest
                {
                    Email = email,
                    Token = token,
                    NewPassword = newPassword
                },
                ct).ConfigureAwait(false);

            return result.Succeeded;
        }

        /// <summary>
        /// Resolves a device id that can be sent to authentication endpoints.
        /// The method guarantees a non-empty value even when platform storage APIs are unavailable.
        /// </summary>
        private async Task<string> ResolveEffectiveDeviceIdAsync(string? deviceId)
        {
            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                return deviceId.Trim();
            }

            var provided = await _deviceIdProvider.GetDeviceIdAsync().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(provided))
            {
                return provided;
            }

            // Defensive fallback for extreme edge cases.
            return Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Validates the JWT access token against the configured app role.
        /// Rules (conservative):
        /// - If AppRole == Business: token MUST contain a non-empty "business_id" claim.
        /// - If AppRole == Consumer: token MUST NOT contain "business_id" claim (prevent accidental business tokens).
        /// - If AppRole == Unknown: no app-type-specific validation is performed.
        /// Throws InvalidOperationException on validation failure.
        /// </summary>
        /// <param name="accessToken">Raw JWT access token string.</param>
        /// <param name="opts">ApiOptions that may contain AppRole and audience expectations.</param>
        private static void ValidateTokenForApp(string accessToken, ApiOptions opts)
        {
            if (string.IsNullOrWhiteSpace(accessToken) || opts is null)
                return;

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(accessToken);

                // Optionally validate audience if present in options (best-effort on client side).
                if (!string.IsNullOrWhiteSpace(opts.JwtAudience))
                {
                    var audClaim = jwt.Audiences;
                    if (audClaim != null && audClaim.Any() && !audClaim.Contains(opts.JwtAudience))
                    {
                        throw new InvalidOperationException("Token audience does not match this app's expected audience.");
                    }
                }

                var hasBusinessId = jwt.Claims.Any(c => string.Equals(c.Type, "business_id", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(c.Value));

                if (opts.AppRole == MobileAppRole.Business && !hasBusinessId)
                {
                    // Business app must receive tokens that include a business_id claim bound to the business.
                    throw new InvalidOperationException("Received access token is not a Business token (missing business_id claim).");
                }

                if (opts.AppRole == MobileAppRole.Consumer && hasBusinessId)
                {
                    // Consumer app should not accept tokens tied to a business.
                    throw new InvalidOperationException("Received access token appears to be a Business token. Please use a Consumer account.");
                }
            }
            catch (ArgumentException)
            {
                // Malformed token — let higher layers handle the error.
                throw new InvalidOperationException("Invalid access token format.");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _refreshLock.Dispose();
            _disposed = true;
        }
    }
}
