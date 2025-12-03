using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Identity;
using Darwin.Contracts.Meta;
using Darwin.Mobile.Shared.Api;
using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.Security;

namespace Darwin.Mobile.Shared.Services;

/// <summary>
/// Handles login/refresh/logout and token persistence for the app.
/// </summary>
public interface IAuthService
{
    Task<AppBootstrapResponse> LoginAsync(string email, string password, string? deviceId, CancellationToken ct);
    Task<bool> TryRefreshAsync(CancellationToken ct);
    Task LogoutAsync(CancellationToken ct);
}

public sealed class AuthService : IAuthService
{
    private readonly IApiClient _api;
    private readonly ITokenStore _store;
    private readonly ApiOptions _opts;

    public AuthService(IApiClient api, ITokenStore store, ApiOptions opts)
    {
        _api = api; _store = store; _opts = opts;
    }

    public async Task<AppBootstrapResponse> LoginAsync(string email, string password, string? deviceId, CancellationToken ct)
    {
        var token = await _api.PostAsync<PasswordLoginRequest, TokenResponse>("auth/login", new PasswordLoginRequest
        {
            Email = email,
            Password = password,
            DeviceId = deviceId
        }, ct).ConfigureAwait(false) ?? throw new InvalidOperationException("Empty token response.");

        await _store.SaveAsync(token.AccessToken, token.AccessTokenExpiresAtUtc, token.RefreshToken, token.RefreshTokenExpiresAtUtc);
        _api.SetBearer(token.AccessToken);

        var boot = await _api.GetAsync<AppBootstrapResponse>("meta/bootstrap", ct) ?? new AppBootstrapResponse();
        _opts.JwtAudience = boot.JwtAudience;
        _opts.QrRefreshSeconds = boot.QrTokenRefreshSeconds;
        _opts.MaxOutbox = boot.MaxOutboxItems;

        return boot;
    }

    public async Task<bool> TryRefreshAsync(CancellationToken ct)
    {
        var (rt, rtex) = await _store.GetRefreshAsync();
        if (string.IsNullOrWhiteSpace(rt) || rtex is null || rtex <= DateTime.UtcNow) return false;

        var res = await _api.PostAsync<RefreshTokenRequest, TokenResponse>("auth/refresh", new RefreshTokenRequest { RefreshToken = rt! }, ct);
        if (res is null) return false;

        await _store.SaveAsync(res.AccessToken, res.AccessTokenExpiresAtUtc, res.RefreshToken, res.RefreshTokenExpiresAtUtc);
        _api.SetBearer(res.AccessToken);
        return true;
    }

    public async Task LogoutAsync(CancellationToken ct)
    {
        var (rt, _) = await _store.GetRefreshAsync();
        if (!string.IsNullOrWhiteSpace(rt))
            _ = await _api.PostAsync<LogoutRequest, object?>("auth/logout", new LogoutRequest { RefreshToken = rt! }, ct);
        await _store.ClearAsync();
        _api.SetBearer(null);
    }
}
