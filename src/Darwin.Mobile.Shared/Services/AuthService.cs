using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
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

            // Resolve an effective device id if caller did not provide one.
            var effectiveDeviceId = deviceId;
            if (string.IsNullOrWhiteSpace(effectiveDeviceId))
            {
                effectiveDeviceId = await _deviceIdProvider.GetDeviceIdAsync().ConfigureAwait(false);
            }

            // Optional: log/debug the effective device id to verify it is non-null.
            System.Diagnostics.Debug.WriteLine($"AuthService.LoginAsync using deviceId={effectiveDeviceId}");

            var token = await _api.PostAsync<PasswordLoginRequest, TokenResponse>(
                ApiRoutes.Auth.Login,
                new PasswordLoginRequest
                {
                    Email = email,
                    Password = password,
                    DeviceId = effectiveDeviceId
                },
                ct).ConfigureAwait(false) ?? throw new InvalidOperationException("Empty token response.");

            await _store.SaveAsync(token.AccessToken, token.AccessTokenExpiresAtUtc, token.RefreshToken, token.RefreshTokenExpiresAtUtc).ConfigureAwait(false);
            _api.SetBearerToken(token.AccessToken);

            // Fetch bootstrap data and populate options
            var boot = await _api.GetAsync<AppBootstrapResponse>(ApiRoutes.Meta.Bootstrap, ct).ConfigureAwait(false)
                       ?? new AppBootstrapResponse();

            // Populate runtime options from bootstrap
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
                // Re-read stored refresh token: another caller may have refreshed while we waited.
                var (currentRt, currentRtex) = await _store.GetRefreshAsync().ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(currentRt) || currentRtex is null || currentRtex <= DateTime.UtcNow)
                    return false;

                // If token changed since initial read, assume refresh already completed successfully.
                if (!string.Equals(currentRt, rt, StringComparison.Ordinal))
                    return true;

                // Perform refresh network call.
                var res = await _api.PostAsync<RefreshTokenRequest, TokenResponse>(
                    ApiRoutes.Auth.Refresh,
                    new RefreshTokenRequest { RefreshToken = currentRt!, DeviceId = null },
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
        public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

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
    }
}