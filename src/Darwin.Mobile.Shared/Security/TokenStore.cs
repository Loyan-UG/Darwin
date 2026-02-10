using System;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Security
{
    /// <summary>
    /// Default token store implementation used by mobile apps.
    /// 
    /// Behavior:
    /// - When building for MAUI mobile TFMs (NET10_0_ANDROID / NET10_0_IOS / NET10_0_MACCATALYST)
    ///   this implementation stores tokens using <see cref="Microsoft.Maui.Storage.SecureStorage"/>.
    /// - For non-mobile TFMs (tests, desktop helpers) it falls back to an in-memory store so the
    ///   Shared library can be used in unit tests and on CI without requiring MAUI platform APIs.
    /// 
    /// Rationale:
    /// - Refresh tokens are long-lived sensitive secrets; on-device secure storage is required for production mobile builds.
    /// - Tests and CI runners typically do not have MAUI platform APIs available; an in-memory fallback is pragmatic.
    /// 
    /// Pitfalls:
    /// - Do NOT use the in-memory fallback for production mobile builds. Ensure CI and unit-test projects
    ///   target non-MAUI TFMs so they use the fallback, while actual MAUI app projects target NET10_0_* TFMs.
    /// - If you change the target framework in the mobile projects, update these preprocessor symbols accordingly.
    /// 
    /// Example:
    /// - In Darwin.Mobile.Consumer (MAUI) the project file should target e.g.:
    ///     <TargetFrameworks>net10.0-android;net10.0-ios;net10.0-maccatalyst</TargetFrameworks>
    ///   so the MAUI SecureStorage branch is compiled in.
    /// </summary>
    public sealed class TokenStore : ITokenStore
    {
#if NET10_0_ANDROID || NET10_0_IOS || NET10_0_MACCATALYST
        /// <inheritdoc />
        public async Task SaveAsync(string accessToken, DateTime accessExpiresUtc, string refreshToken, DateTime refreshExpiresUtc)
        {
            // Save tokens as ISO-8601 for dates; avoid culture-sensitive formats.
            // Use ConfigureAwait(false) in library code to avoid deadlocks in some sync-over-async environments.
            await SecureStorage.SetAsync("access_token", accessToken).ConfigureAwait(false);
            await SecureStorage.SetAsync("access_expires", accessExpiresUtc.ToString("O")).ConfigureAwait(false);
            await SecureStorage.SetAsync("refresh_token", refreshToken).ConfigureAwait(false);
            await SecureStorage.SetAsync("refresh_expires", refreshExpiresUtc.ToString("O")).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<(string? AccessToken, DateTime? AccessExpiresUtc)> GetAccessAsync()
        {
            var at = await SecureStorage.GetAsync("access_token").ConfigureAwait(false);
            var exp = await SecureStorage.GetAsync("access_expires").ConfigureAwait(false);

            return (at, DateTime.TryParse(exp, null, System.Globalization.DateTimeStyles.RoundtripKind, out var d) ? d : null);
        }

        /// <inheritdoc />
        public async Task<(string? RefreshToken, DateTime? RefreshExpiresUtc)> GetRefreshAsync()
        {
            var rt = await SecureStorage.GetAsync("refresh_token").ConfigureAwait(false);
            var exp = await SecureStorage.GetAsync("refresh_expires").ConfigureAwait(false);

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
}