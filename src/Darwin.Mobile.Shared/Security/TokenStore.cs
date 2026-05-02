using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace Darwin.Mobile.Shared.Security
{
    /// <summary>
    /// Default token store implementation used by mobile apps.
    ///
    /// Behavior:
    /// - When building for MAUI app TFMs (Android, iOS, MacCatalyst, Windows)
    ///   this implementation stores tokens using <see cref="SecureStorage"/>.
    /// - For non-MAUI TFMs (tests and helper processes) it falls back to an in-memory store so the
    ///   Shared library can still be loaded without platform secure-storage primitives.
    ///
    /// Rationale:
    /// - Access and refresh tokens must survive app restarts on real app targets so startup restore
    ///   and silent session rehydration behave consistently for end users.
    /// - Non-MAUI builds do not always have access to platform secure storage, therefore tests keep
    ///   a pragmatic in-memory fallback instead of coupling to device APIs.
    ///
    /// Pitfalls:
    /// - The in-memory fallback must never be used for actual app TFMs, otherwise sessions appear
    ///   to "randomly" disappear after every restart.
    /// - If additional MAUI target platforms are added later, extend the preprocessor condition below.
    /// </summary>
    public sealed class TokenStore : ITokenStore
    {
#if ANDROID || IOS || MACCATALYST || WINDOWS
        private const string AccessTokenKey = "access_token";
        private const string AccessExpiresKey = "access_expires";
        private const string RefreshTokenKey = "refresh_token";
        private const string RefreshExpiresKey = "refresh_expires";

        /// <inheritdoc />
        public async Task SaveAsync(string accessToken, DateTime accessExpiresUtc, string refreshToken, DateTime refreshExpiresUtc)
        {
            ValidateTokens(accessToken, refreshToken);

            await SecureStorage.SetAsync(AccessTokenKey, accessToken).ConfigureAwait(false);
            await SecureStorage.SetAsync(AccessExpiresKey, accessExpiresUtc.ToString("O", CultureInfo.InvariantCulture)).ConfigureAwait(false);
            await SecureStorage.SetAsync(RefreshTokenKey, refreshToken).ConfigureAwait(false);
            await SecureStorage.SetAsync(RefreshExpiresKey, refreshExpiresUtc.ToString("O", CultureInfo.InvariantCulture)).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<(string? AccessToken, DateTime? AccessExpiresUtc)> GetAccessAsync()
        {
            try
            {
                var accessToken = await SecureStorage.GetAsync(AccessTokenKey).ConfigureAwait(false);
                var accessExpiry = await SecureStorage.GetAsync(AccessExpiresKey).ConfigureAwait(false);

                return (accessToken, DateTime.TryParse(accessExpiry, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiresUtc) ? expiresUtc : null);
            }
            catch
            {
                // SecureStorage can become unreadable after OS backup/restore or keystore invalidation.
                // Clearing the session is safer than repeatedly failing startup restore with stale encrypted entries.
                await ClearAsync().ConfigureAwait(false);
                return (null, null);
            }
        }

        /// <inheritdoc />
        public async Task<(string? RefreshToken, DateTime? RefreshExpiresUtc)> GetRefreshAsync()
        {
            try
            {
                var refreshToken = await SecureStorage.GetAsync(RefreshTokenKey).ConfigureAwait(false);
                var refreshExpiry = await SecureStorage.GetAsync(RefreshExpiresKey).ConfigureAwait(false);

                return (refreshToken, DateTime.TryParse(refreshExpiry, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiresUtc) ? expiresUtc : null);
            }
            catch
            {
                // Refresh tokens must not be kept if the encrypted store cannot be read reliably.
                await ClearAsync().ConfigureAwait(false);
                return (null, null);
            }
        }

        /// <inheritdoc />
        public Task ClearAsync()
        {
            SecureStorage.Remove(AccessTokenKey);
            SecureStorage.Remove(AccessExpiresKey);
            SecureStorage.Remove(RefreshTokenKey);
            SecureStorage.Remove(RefreshExpiresKey);
            return Task.CompletedTask;
        }
#else
        private string? _accessToken;
        private string? _refreshToken;
        private DateTime? _accessExpiresUtc;
        private DateTime? _refreshExpiresUtc;

        /// <inheritdoc />
        public Task SaveAsync(string accessToken, DateTime accessExpiresUtc, string refreshToken, DateTime refreshExpiresUtc)
        {
            ValidateTokens(accessToken, refreshToken);

            _accessToken = accessToken;
            _accessExpiresUtc = accessExpiresUtc;
            _refreshToken = refreshToken;
            _refreshExpiresUtc = refreshExpiresUtc;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<(string? AccessToken, DateTime? AccessExpiresUtc)> GetAccessAsync()
            => Task.FromResult((_accessToken, _accessExpiresUtc));

        /// <inheritdoc />
        public Task<(string? RefreshToken, DateTime? RefreshExpiresUtc)> GetRefreshAsync()
            => Task.FromResult((_refreshToken, _refreshExpiresUtc));

        /// <inheritdoc />
        public Task ClearAsync()
        {
            _accessToken = null;
            _refreshToken = null;
            _accessExpiresUtc = null;
            _refreshExpiresUtc = null;
            return Task.CompletedTask;
        }
#endif

        private static void ValidateTokens(string accessToken, string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new ArgumentException("Access token is required.", nameof(accessToken));
            }

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new ArgumentException("Refresh token is required.", nameof(refreshToken));
            }
        }
    }
}
