using System;
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

    public sealed class AuthService : IAuthService
    {
        private readonly IApiClient _api;
        private readonly ITokenStore _store;
        private readonly ApiOptions _opts;

        public AuthService(IApiClient api, ITokenStore store, ApiOptions opts)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _opts = opts ?? throw new ArgumentNullException(nameof(opts));
        }

        /// <inheritdoc />
        public async Task<AppBootstrapResponse> LoginAsync(string email, string password, string? deviceId, CancellationToken ct)
        {
            var token = await _api.PostAsync<PasswordLoginRequest, TokenResponse>(
                ApiRoutes.Auth.Login,
                new PasswordLoginRequest
                {
                    Email = email,
                    Password = password,
                    DeviceId = deviceId
                },
                ct).ConfigureAwait(false) ?? throw new InvalidOperationException("Empty token response.");

            await _store.SaveAsync(token.AccessToken, token.AccessTokenExpiresAtUtc, token.RefreshToken, token.RefreshTokenExpiresAtUtc).ConfigureAwait(false);
            _api.SetBearerToken(token.AccessToken);

            // Pull bootstrap options that guide the mobile runtime (audience, QR refresh cadence, etc.)
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
    }
}