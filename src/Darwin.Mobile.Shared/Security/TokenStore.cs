// src/Darwin.Mobile.Shared/Security/TokenStore.cs
// https://github.com/shahramvafadar/Darwin/blob/301147077eba61b84e0eec8656aec08e20a1795a/src/Darwin.Mobile.Shared/Security/TokenStore.cs

using System;
using System.Threading.Tasks;

#if NET9_0_ANDROID || NET9_0_IOS || NET9_0_MACCATALYST || NET10_0_ANDROID || NET10_0_IOS || NET10_0_MACCATALYST
// SecureStorage is provided by MAUI; compile this using only when targeting MAUI mobile TFMs.
// We include both NET9_0_* and NET10_0_* conditionals to be compatible with projects targeting either SDK.
using Microsoft.Maui.Storage;
#endif

namespace Darwin.Mobile.Shared.Security
{
    /// <summary>
    /// Default token store. Uses platform secure storage where available (MAUI).
    /// On non-mobile or test TFMs, falls back to an in-memory store (suitable for unit tests / desktop helpers).
    ///
    /// Rationale:
    /// - Mobile apps must store refresh tokens securely; SecureStorage exposes platform-specific protected storage.
    /// - For test runners and CI (non-mobile TFMs) an in-memory fallback is pragmatic and avoids platform references.
    ///
    /// Pitfalls:
    /// - Ensure the TFM constants used in the #if match your project targeting (NET10_0_* for .NET 10).
    /// - Do not use the in-memory fallback in production mobile builds.
    /// </summary>
    public sealed class TokenStore : ITokenStore
    {
#if NET9_0_ANDROID || NET9_0_IOS || NET9_0_MACCATALYST || NET10_0_ANDROID || NET10_0_IOS || NET10_0_MACCATALYST
        /// <inheritdoc />
        public async Task SaveAsync(string accessToken, DateTime accessExpiresUtc, string refreshToken, DateTime refreshExpiresUtc)
        {
            // Example: save tokens and expiry times as ISO-8601 strings
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

        /// <inheritdoc />
        public Task ClearAsync()
        {
            SecureStorage.Remove("access_token");
            SecureStorage.Remove("access_expires");
            SecureStorage.Remove("refresh_token");
            SecureStorage.Remove("refresh_expires");
            return Task.CompletedTask;
        }
#else
        // In-memory fallback for non-mobile TFMs (tests, desktop helpers).
        private string? _at, _rt;
        private DateTime? _atex, _rtex;

        /// <inheritdoc />
        public Task SaveAsync(string accessToken, DateTime accessExpiresUtc, string refreshToken, DateTime refreshExpiresUtc)
        {
            _at = accessToken;
            _atex = accessExpiresUtc;
            _rt = refreshToken;
            _rtex = refreshExpiresUtc;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<(string? AccessToken, DateTime? AccessExpiresUtc)> GetAccessAsync()
            => Task.FromResult((_at, _atex));

        /// <inheritdoc />
        public Task<(string? RefreshToken, DateTime? RefreshExpiresUtc)> GetRefreshAsync()
            => Task.FromResult((_rt, _rtex));

        /// <inheritdoc />
        public Task ClearAsync()
        {
            _at = _rt = null;
            _atex = _rtex = null;
            return Task.CompletedTask;
        }
#endif
    }
}