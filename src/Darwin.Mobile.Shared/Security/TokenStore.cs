using System;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Security;

/// <summary>
/// Default token store. Uses platform secure storage where available.
/// On net9.0 (non-MAUI), falls back to in-memory (for tests/desktop tools).
/// </summary>
public sealed class TokenStore : ITokenStore
{
#if NET9_0_ANDROID || NET9_0_IOS || NET9_0_MACCATALYST
    public async Task SaveAsync(string accessToken, DateTime accessExpiresUtc, string refreshToken, DateTime refreshExpiresUtc)
    {
        await SecureStorage.SetAsync("access_token", accessToken);
        await SecureStorage.SetAsync("access_expires", accessExpiresUtc.ToString("O"));
        await SecureStorage.SetAsync("refresh_token", refreshToken);
        await SecureStorage.SetAsync("refresh_expires", refreshExpiresUtc.ToString("O"));
    }

    public async Task<(string? AccessToken, DateTime? AccessExpiresUtc)> GetAccessAsync()
    {
        var at = await SecureStorage.GetAsync("access_token");
        var exp = await SecureStorage.GetAsync("access_expires");
        return (at, DateTime.TryParse(exp, null, System.Globalization.DateTimeStyles.RoundtripKind, out var d) ? d : null);
    }

    public async Task<(string? RefreshToken, DateTime? RefreshExpiresUtc)> GetRefreshAsync()
    {
        var rt = await SecureStorage.GetAsync("refresh_token");
        var exp = await SecureStorage.GetAsync("refresh_expires");
        return (rt, DateTime.TryParse(exp, null, System.Globalization.DateTimeStyles.RoundtripKind, out var d) ? d : null);
    }

    public Task ClearAsync()
    {
        SecureStorage.Remove("access_token");
        SecureStorage.Remove("access_expires");
        SecureStorage.Remove("refresh_token");
        SecureStorage.Remove("refresh_expires");
        return Task.CompletedTask;
    }
#else
    private string? _at, _rt;
    private DateTime? _atex, _rtex;

    public Task SaveAsync(string accessToken, DateTime accessExpiresUtc, string refreshToken, DateTime refreshExpiresUtc)
    { _at = accessToken; _atex = accessExpiresUtc; _rt = refreshToken; _rtex = refreshExpiresUtc; return Task.CompletedTask; }

    public Task<(string? AccessToken, DateTime? AccessExpiresUtc)> GetAccessAsync() => Task.FromResult((_at, _atex));
    public Task<(string? RefreshToken, DateTime? RefreshExpiresUtc)> GetRefreshAsync() => Task.FromResult((_rt, _rtex));
    public Task ClearAsync() { _at = _rt = null; _atex = _rtex = null; return Task.CompletedTask; }
#endif
}
