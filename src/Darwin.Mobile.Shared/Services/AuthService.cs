using Darwin.Contracts.Businesses;
using Darwin.Contracts.Identity;
using Darwin.Contracts.Meta;
using Darwin.Mobile.Shared.Api;
using Darwin.Mobile.Shared.Caching;
using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.Security;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Services;

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

    /// <summary>
    /// Ensures that a usable authenticated session exists by reusing a valid access token
    /// or refreshing it when the client can no longer rely on the current token lifetime.
    /// </summary>
    Task<bool> EnsureAuthenticatedSessionAsync(CancellationToken ct);

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

    /// <summary>Requests that a new account confirmation email be sent to the specified email address.</summary>
    Task<bool> RequestEmailConfirmationAsync(string email, CancellationToken ct);

    /// <summary>Confirms an account email using the supplied one-time token.</summary>
    Task<bool> ConfirmEmailAsync(string email, string token, CancellationToken ct);

    /// <summary>Loads invitation preview data for business onboarding.</summary>
    Task<BusinessInvitationPreviewResponse?> GetBusinessInvitationPreviewAsync(string token, CancellationToken ct);

    /// <summary>Accepts a business invitation and signs the operator into the Business app.</summary>
    Task<AppBootstrapResponse> AcceptBusinessInvitationAsync(AcceptBusinessInvitationRequest request, string? deviceId, CancellationToken ct);
}

/// <summary>
/// Default implementation of <see cref="IAuthService"/>.
/// Responsibilities include:
/// - Calling login/refresh/logout endpoints via <see cref="IApiClient"/>.
/// - Persisting tokens via <see cref="ITokenStore"/>.
/// - Rehydrating bootstrap configuration from local cache before network access.
/// - Validating returned JWT against the configured <see cref="ApiOptions.AppRole"/>.
/// - Coordinating concurrent refresh attempts (single-flight) to avoid refresh storms.
/// </summary>
public sealed class AuthService : IAuthService, IDisposable
{
    private static readonly TimeSpan AccessTokenRefreshWindow = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan BootstrapCacheTtl = TimeSpan.FromHours(12);
    private static readonly TimeSpan BootstrapFallbackMaxAge = TimeSpan.FromDays(7);
    private const string BootstrapCacheKey = "meta.bootstrap";

    private readonly IApiClient _api;
    private readonly IMobileCacheService _cache;
    private readonly ITokenStore _store;
    private readonly ApiOptions _opts;
    private readonly IDeviceIdProvider _deviceIdProvider;
    private readonly TimeProvider _timeProvider;

    // SemaphoreSlim is used instead of lock so concurrent async callers do not trigger refresh storms.
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private bool _disposed;

    public AuthService(
        IApiClient api,
        IMobileCacheService cache,
        ITokenStore store,
        ApiOptions opts,
        IDeviceIdProvider deviceIdProvider,
        TimeProvider timeProvider)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _opts = opts ?? throw new ArgumentNullException(nameof(opts));
        _deviceIdProvider = deviceIdProvider ?? throw new ArgumentNullException(nameof(deviceIdProvider));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
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
                DeviceId = effectiveDeviceId,
                BusinessId = null
            },
            ct).ConfigureAwait(false);

        if (!tokenResult.Succeeded || tokenResult.Value is null)
        {
            var message = string.IsNullOrWhiteSpace(tokenResult.Error)
                ? "Login failed."
                : tokenResult.Error;

            throw new InvalidOperationException(message);
        }

        return await CompleteAuthenticatedBootstrapAsync(tokenResult.Value, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> TryRefreshAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var (rt, rtex) = await _store.GetRefreshAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(rt) || rtex is null || rtex <= _timeProvider.GetUtcNow().UtcDateTime)
        {
            await ClearLocalSessionAsync(ct).ConfigureAwait(false);
            return false;
        }

        await _refreshLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var (currentRt, currentRtex) = await _store.GetRefreshAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(currentRt) || currentRtex is null || currentRtex <= _timeProvider.GetUtcNow().UtcDateTime)
            {
                return false;
            }

            if (!string.Equals(currentRt, rt, StringComparison.Ordinal))
            {
                return true;
            }

            var effectiveDeviceId = await ResolveEffectiveDeviceIdAsync(deviceId: null).ConfigureAwait(false);
            var preferredBusinessId = await TryGetPreferredBusinessIdFromStoredAccessTokenAsync().ConfigureAwait(false);

            var refreshResult = await _api.PostResultAsync<RefreshTokenRequest, TokenResponse>(
                ApiRoutes.Auth.Refresh,
                new RefreshTokenRequest
                {
                    RefreshToken = currentRt!,
                    DeviceId = effectiveDeviceId,
                    BusinessId = preferredBusinessId
                },
                ct).ConfigureAwait(false);

            if (!refreshResult.Succeeded || refreshResult.Value is null)
            {
                if (LooksLikeDefinitiveRefreshFailure(refreshResult.Error))
                {
                    await ClearLocalSessionAsync(ct).ConfigureAwait(false);
                }

                return false;
            }

            var res = refreshResult.Value;
            ValidateTokenForApp(res.AccessToken, _opts);

            await _store.SaveAsync(res.AccessToken, res.AccessTokenExpiresAtUtc, res.RefreshToken, res.RefreshTokenExpiresAtUtc).ConfigureAwait(false);
            _api.SetBearerToken(res.AccessToken);
            await TryRefreshBootstrapCacheAsync(ct).ConfigureAwait(false);

            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
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
        var (rt, _) = await _store.GetRefreshAsync().ConfigureAwait(false);

        try
        {
            ct.ThrowIfCancellationRequested();

            if (!string.IsNullOrWhiteSpace(rt))
            {
                _ = await _api.PostAsync<LogoutRequest, object?>(
                    ApiRoutes.Auth.Logout,
                    new LogoutRequest { RefreshToken = rt! },
                    ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Local logout must still complete when remote revoke exceeds the user-facing logout budget.
        }
        catch
        {
            // Remote revoke is best-effort; clearing local credentials is the security-critical step.
        }

        try
        {
            await _store.ClearAsync().ConfigureAwait(false);
        }
        catch
        {
            // SecureStorage cleanup failures should not keep the in-memory client authenticated.
        }

        try
        {
            await _cache.ClearAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Cache cleanup is best-effort during logout; stale cache entries are scoped and overwritten on next login.
        }

        _api.SetBearerToken(null);
    }

    /// <inheritdoc />
    public async Task<bool> LogoutAllAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var result = await _api.PostNoContentAsync(
            ApiRoutes.Auth.LogoutAll,
            new { },
            ct).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            return false;
        }

        await _store.ClearAsync().ConfigureAwait(false);
        await _cache.ClearAsync(ct).ConfigureAwait(false);
        _api.SetBearerToken(null);
        return true;
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

        var firstAttempt = await _api.PostNoContentAsync(
            ApiRoutes.Auth.ChangePassword,
            new ChangePasswordRequest
            {
                CurrentPassword = currentPassword,
                NewPassword = newPassword
            },
            ct).ConfigureAwait(false);

        if (firstAttempt.Succeeded)
        {
            return true;
        }

        if (!LooksUnauthorized(firstAttempt.Error))
        {
            return false;
        }

        var refreshed = await TryRefreshAsync(ct).ConfigureAwait(false);
        if (!refreshed)
        {
            return false;
        }

        var secondAttempt = await _api.PostNoContentAsync(
            ApiRoutes.Auth.ChangePassword,
            new ChangePasswordRequest
            {
                CurrentPassword = currentPassword,
                NewPassword = newPassword
            },
            ct).ConfigureAwait(false);

        return secondAttempt.Succeeded;
    }

    /// <inheritdoc />
    public async Task<bool> RequestPasswordResetAsync(string email, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var result = await _api.PostNoContentAsync(
            ApiRoutes.Auth.RequestPasswordReset,
            new RequestPasswordResetRequest { Email = email },
            ct).ConfigureAwait(false);

        return result.Succeeded;
    }

    /// <inheritdoc />
    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var result = await _api.PostNoContentAsync(
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

    /// <inheritdoc />
    public async Task<bool> RequestEmailConfirmationAsync(string email, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var result = await _api.PostNoContentAsync(
            ApiRoutes.Auth.RequestEmailConfirmation,
            new RequestEmailConfirmationRequest { Email = email },
            ct).ConfigureAwait(false);

        return result.Succeeded;
    }

    /// <inheritdoc />
    public async Task<bool> ConfirmEmailAsync(string email, string token, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var result = await _api.PostNoContentAsync(
            ApiRoutes.Auth.ConfirmEmail,
            new ConfirmEmailRequest
            {
                Email = email,
                Token = token
            },
            ct).ConfigureAwait(false);

        return result.Succeeded;
    }

    /// <inheritdoc />
    public async Task<BusinessInvitationPreviewResponse?> GetBusinessInvitationPreviewAsync(string token, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var trimmedToken = token?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedToken))
        {
            throw new InvalidOperationException("Invitation token is required.");
        }

        var route = $"{ApiRoutes.BusinessAuth.PreviewInvitation}?token={Uri.EscapeDataString(trimmedToken)}";
        var result = await _api.GetResultAsync<BusinessInvitationPreviewResponse>(route, ct).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            var message = string.IsNullOrWhiteSpace(result.Error)
                ? "Invitation preview failed."
                : result.Error;

            throw new InvalidOperationException(message);
        }

        return result.Value;
    }

    /// <inheritdoc />
    public async Task<AppBootstrapResponse> AcceptBusinessInvitationAsync(
        AcceptBusinessInvitationRequest request,
        string? deviceId,
        CancellationToken ct)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        ct.ThrowIfCancellationRequested();

        var effectiveDeviceId = await ResolveEffectiveDeviceIdAsync(deviceId ?? request.DeviceId).ConfigureAwait(false);

        var tokenResult = await _api.PostResultAsync<AcceptBusinessInvitationRequest, TokenResponse>(
            ApiRoutes.BusinessAuth.AcceptInvitation,
            new AcceptBusinessInvitationRequest
            {
                Token = request.Token?.Trim() ?? string.Empty,
                DeviceId = effectiveDeviceId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Password = request.Password
            },
            ct).ConfigureAwait(false);

        if (!tokenResult.Succeeded || tokenResult.Value is null)
        {
            var message = string.IsNullOrWhiteSpace(tokenResult.Error)
                ? "Invitation acceptance failed."
                : tokenResult.Error;

            throw new InvalidOperationException(message);
        }

        return await CompleteAuthenticatedBootstrapAsync(tokenResult.Value, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> EnsureAuthenticatedSessionAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await ApplyCachedBootstrapAsync(ct).ConfigureAwait(false);

        var (accessToken, accessExpiresUtc) = await _store.GetAccessAsync().ConfigureAwait(false);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        if (!string.IsNullOrWhiteSpace(accessToken) &&
            (!accessExpiresUtc.HasValue || accessExpiresUtc.Value > nowUtc))
        {
            _api.SetBearerToken(accessToken);

            if (!accessExpiresUtc.HasValue || accessExpiresUtc.Value > nowUtc.Add(AccessTokenRefreshWindow))
            {
                return true;
            }

            var refreshedWithinWindow = await TryRefreshAsync(ct).ConfigureAwait(false);
            if (refreshedWithinWindow)
            {
                return true;
            }

            var (currentAccessToken, currentAccessExpiresUtc) = await _store.GetAccessAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(currentAccessToken) ||
                (currentAccessExpiresUtc.HasValue && currentAccessExpiresUtc.Value <= _timeProvider.GetUtcNow().UtcDateTime))
            {
                _api.SetBearerToken(null);
                return false;
            }

            // Keep the client bound to the latest still-valid access token after a non-definitive refresh failure.
            _api.SetBearerToken(currentAccessToken);
            return true;
        }

        var refreshedFromExpiredSession = await TryRefreshAsync(ct).ConfigureAwait(false);
        if (!refreshedFromExpiredSession)
        {
            _api.SetBearerToken(null);
        }

        return refreshedFromExpiredSession;
    }

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

        return Guid.NewGuid().ToString("N");
    }

    private async Task<AppBootstrapResponse> CompleteAuthenticatedBootstrapAsync(TokenResponse token, CancellationToken ct)
    {
        ValidateTokenForApp(token.AccessToken, _opts);

        await _store.SaveAsync(
            token.AccessToken,
            token.AccessTokenExpiresAtUtc,
            token.RefreshToken,
            token.RefreshTokenExpiresAtUtc).ConfigureAwait(false);

        _api.SetBearerToken(token.AccessToken);

        var boot = await _api.GetAsync<AppBootstrapResponse>(ApiRoutes.Meta.Bootstrap, ct).ConfigureAwait(false)
                   ?? new AppBootstrapResponse();

        ApplyBootstrap(boot);
        await _cache.SetAsync(BootstrapCacheKey, boot, BootstrapCacheTtl, ct).ConfigureAwait(false);

        return boot;
    }

    private async Task TryRefreshBootstrapCacheAsync(CancellationToken ct)
    {
        try
        {
            var bootstrap = await _api.GetAsync<AppBootstrapResponse>(ApiRoutes.Meta.Bootstrap, ct).ConfigureAwait(false);
            if (bootstrap is null)
            {
                return;
            }

            ApplyBootstrap(bootstrap);
            await _cache.SetAsync(BootstrapCacheKey, bootstrap, BootstrapCacheTtl, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Token refresh must stay usable even if bootstrap revalidation is temporarily unavailable.
        }
    }

    private async Task<Guid?> TryGetPreferredBusinessIdFromStoredAccessTokenAsync()
    {
        var (accessToken, _) = await _store.GetAccessAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        return JwtClaimReader.GetBusinessId(accessToken);
    }

    private async Task ApplyCachedBootstrapAsync(CancellationToken ct)
    {
        var cached = await _cache.GetFreshAsync<AppBootstrapResponse>(BootstrapCacheKey, ct).ConfigureAwait(false)
            ?? await _cache.GetUsableAsync<AppBootstrapResponse>(BootstrapCacheKey, BootstrapFallbackMaxAge, ct).ConfigureAwait(false);

        if (cached is not null)
        {
            ApplyBootstrap(cached);
        }
    }

    private void ApplyBootstrap(AppBootstrapResponse bootstrap)
    {
        _opts.JwtAudience = bootstrap.JwtAudience;
        _opts.QrRefreshSeconds = bootstrap.QrTokenRefreshSeconds;
        _opts.MaxOutbox = bootstrap.MaxOutboxItems;
    }

    private static bool LooksUnauthorized(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return false;
        }

        return error.Contains("401", StringComparison.OrdinalIgnoreCase) ||
               error.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeDefinitiveRefreshFailure(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return false;
        }

        return error.Contains("401", StringComparison.OrdinalIgnoreCase) ||
               error.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) ||
               error.Contains("refresh token", StringComparison.OrdinalIgnoreCase) ||
               error.Contains("expired", StringComparison.OrdinalIgnoreCase) ||
               error.Contains("revoked", StringComparison.OrdinalIgnoreCase) ||
               error.Contains("invalid token", StringComparison.OrdinalIgnoreCase);
    }

    private async Task ClearLocalSessionAsync(CancellationToken ct)
    {
        await _store.ClearAsync().ConfigureAwait(false);
        await _cache.ClearAsync(ct).ConfigureAwait(false);
        _api.SetBearerToken(null);
    }

    /// <summary>
    /// Validates the JWT access token against the configured app role.
    /// Rules (conservative):
    /// - If AppRole == Business: token MUST contain a non-empty "business_id" claim.
    /// - If AppRole == Consumer: token MUST NOT contain "business_id" claim (prevent accidental business tokens).
    /// - If AppRole == Unknown: no app-type-specific validation is performed.
    /// Throws InvalidOperationException on validation failure.
    /// </summary>
    private static void ValidateTokenForApp(string accessToken, ApiOptions opts)
    {
        if (string.IsNullOrWhiteSpace(accessToken) || opts is null)
        {
            return;
        }

        var jwt = JwtClaimReader.TryReadToken(accessToken);
        if (jwt is null)
        {
            throw new InvalidOperationException("Invalid access token format.");
        }

        if (!string.IsNullOrWhiteSpace(opts.JwtAudience) && !jwt.Audiences.Contains(opts.JwtAudience))
        {
            throw new InvalidOperationException("Token audience does not match this app's expected audience.");
        }

        var hasBusinessId = JwtClaimReader.GetBusinessId(jwt).HasValue;

        if (opts.AppRole == MobileAppRole.Business && !hasBusinessId)
        {
            throw new InvalidOperationException("Received access token is not a Business token (missing business_id claim).");
        }

        if (opts.AppRole == MobileAppRole.Consumer && hasBusinessId)
        {
            throw new InvalidOperationException("Received access token appears to be a Business token. Please use a Consumer account.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _refreshLock.Dispose();
        _disposed = true;
    }
}
