using System;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Security
{
    /// <summary>
    /// Default device id provider.
    /// - On mobile platforms uses SecureStorage to persist a generated GUID.
    /// - On non-mobile targets uses an in-memory value (suitable for desktop/test).
    /// The generated id uses Guid.NewGuid().ToString(\"N\") to produce a compact stable token.
    /// </summary>
    public sealed class DeviceIdProvider : IDeviceIdProvider
    {
#if NET9_0_ANDROID || NET9_0_IOS || NET9_0_MACCATALYST
        private const string Key = "darwin_device_id";

        public async Task<string> GetDeviceIdAsync()
        {
            // SecureStorage is platform-specific; on mobile it persists across launches.
            var existing = await SecureStorage.GetAsync(Key).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(existing))
                return existing;

            var id = Guid.NewGuid().ToString("N");
            await SecureStorage.SetAsync(Key, id).ConfigureAwait(false);
            return id;
        }
#else
        // Fallback for non-mobile (unit tests, desktop) to avoid runtime SecureStorage dependency.
        private static string? _cached;
        public Task<string> GetDeviceIdAsync()
        {
            if (!string.IsNullOrWhiteSpace(_cached))
                return Task.FromResult(_cached);

            _cached = Guid.NewGuid().ToString("N");
            return Task.FromResult(_cached);
        }
#endif
    }
}