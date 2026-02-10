using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Identity;
using Darwin.Contracts.Meta;
using Darwin.Mobile.Shared.Api;
using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.Security;

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
    /// </summary>
    public sealed class AuthService : IAuthService
    {
        private readonly IApiClient _api;
        private readonly ITokenStore _store;
        private readonly ApiOptions _opts;
        private readonly IDeviceIdProvider _deviceIdProvider;

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
            // Resolve an effective device id if caller did not provide one.
            var effectiveDeviceId = deviceId;
            if (string.IsNullOrWhiteSpace(effectiveDeviceId))
            {
                // This will hit your breakpoint if provider is registered and working.
                effectiveDeviceId = await _deviceIdProvider.GetDeviceIdAsync().ConfigureAwait(false);
            }

            // Call login endpoint
            var token = await _api.PostAsync<PasswordLoginRequest, TokenResponse>(
                ApiRoutes.Auth.Login,
                new PasswordLoginRequest
                {
                    Email = email,
                    Password = password,
                    DeviceId = effectiveDeviceId
                },
                ct).ConfigureAwait(false) ?? throw new InvalidOperationException("Empty token response.");

            // Basic token validation depending on app role to prevent cross-app login.
            ValidateTokenForApp(token.AccessToken, _opts);

            // Persist tokens and set bearer for subsequent requests
            await _store.SaveAsync(token.AccessToken, token.AccessTokenExpiresAtUtc, token.RefreshToken, token.RefreshTokenExpiresAtUtc).ConfigureAwait(false);
            _api.SetBearerToken(token.AccessToken);

            // Fetch bootstrap data and populate options
            var boot = await _api.GetAsync<AppBootstrapResponse>(ApiRoutes.Meta.Bootstrap, ct).ConfigureAwait(false)
                       ?? new AppBootstrapResponse();

            _opts.JwtAudience = boot.JwtAudience;
            _opts.QrRefreshSeconds = boot.QrTokenRefreshSeconds;
            _opts.MaxOutbox = boot.MaxOutboxItems;

            return boot;
        }

        /// <inheritdoc />
        public async Task<bool> TryRefreshAsync(CancellationToken ct)
        {
            var (rt, rtex) = await _store.GetRefreshAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(rt) || rtex is null || rtex <= DateTime.UtcNow)
                return false;

            var res = await _api.PostAsync<RefreshTokenRequest, TokenResponse>(
                ApiRoutes.Auth.Refresh,
                new RefreshTokenRequest { RefreshToken = rt!, DeviceId = null },
                ct).ConfigureAwait(false);

            if (res is null)
                return false;

            // Validate refreshed token as well
            ValidateTokenForApp(res.AccessToken, _opts);

            await _store.SaveAsync(res.AccessToken, res.AccessTokenExpiresAtUtc, res.RefreshToken, res.RefreshTokenExpiresAtUtc).ConfigureAwait(false);
            _api.SetBearerToken(res.AccessToken);
            return true;
        }

        /// <inheritdoc />
        public async Task LogoutAsync(CancellationToken ct)
        {
            var (rt, _) = await _store.GetRefreshAsync().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(rt))
            {
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
            // The endpoint accepts POST without body; sending an empty object "{}" is acceptable.
            var result = await _api.PostResultAsync<object, object?>(
                ApiRoutes.Auth.LogoutAll,
                new { },
                ct).ConfigureAwait(false);

            if (result.Succeeded)
            {
                // Clear local tokens for good measure.
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

            return await _api.PostAsync<RegisterRequest, RegisterResponse>(
                ApiRoutes.Auth.Register,
                request,
                ct).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct)
        {
            var result = await _api.PostResultAsync<ChangePasswordRequest, object?>(
                ApiRoutes.Auth.ChangePassword,
                new ChangePasswordRequest
                {
                    CurrentPassword = currentPassword,
                    NewPassword = newPassword
                },
                ct).ConfigureAwait(false);

            return result.Succeeded;
        }

        /// <inheritdoc />
        public async Task<bool> RequestPasswordResetAsync(string email, CancellationToken ct)
        {
            var result = await _api.PostResultAsync<RequestPasswordResetRequest, object?>(
                ApiRoutes.Auth.RequestPasswordReset,
                new RequestPasswordResetRequest { Email = email },
                ct).ConfigureAwait(false);

            // The API always returns 200/OK to avoid user enumeration; treat success as true.
            return result.Succeeded;
        }

        /// <inheritdoc />
        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken ct)
        {
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
    }
}