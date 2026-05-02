using System;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace Darwin.Mobile.Shared.Security
{
    /// <summary>
    /// Default device id provider.
    /// - Tries to persist a generated GUID using SecureStorage (MAUI) for security and persistence.
    /// - Falls back to Preferences when SecureStorage fails (platform limitations) or to an in-memory value for tests.
    /// The method is resilient and will never throw for the common failure modes, returning a stable id.
    /// </summary>
    public sealed class DeviceIdProvider : IDeviceIdProvider
    {
        private const string Key = "darwin_device_id";
        private static string? _cached;

        public async Task<string> GetDeviceIdAsync()
        {
            if (!string.IsNullOrWhiteSpace(_cached))
            {
                return _cached;
            }

            try
            {
                var existing = await SecureStorage.GetAsync(Key).ConfigureAwait(false);
                if (IsValidDeviceId(existing))
                {
                    _cached = existing!;
                    return _cached;
                }

                // If an older app run had to fall back to Preferences, migrate that stable id
                // back into SecureStorage instead of changing the device binding identity.
                var fallbackId = ReadPreferenceDeviceId();
                var id = IsValidDeviceId(fallbackId) ? fallbackId! : CreateDeviceId();

                await SecureStorage.SetAsync(Key, id).ConfigureAwait(false);
                _cached = id;
                return _cached;
            }
            catch
            {
                // If SecureStorage fails (e.g., running in unit tests or desktop environment),
                // fall back to Preferences which is less secure but persistent.
                try
                {
                    var pref = ReadPreferenceDeviceId();
                    if (IsValidDeviceId(pref))
                    {
                        _cached = pref!;
                        return _cached;
                    }

                    var id = CreateDeviceId();
                    Preferences.Set(Key, id);
                    _cached = id;
                    return _cached;
                }
                catch
                {
                    // Last resort: keep an in-memory id (non-persistent). Good enough for tests.
                    _cached = CreateDeviceId();
                    return _cached;
                }
            }
        }

        /// <summary>
        /// Reads the fallback device id without letting platform preference errors escape auth startup.
        /// </summary>
        private static string? ReadPreferenceDeviceId()
        {
            try
            {
                return Preferences.Get(Key, null);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a compact, non-guessable local device identifier for refresh-token device binding.
        /// </summary>
        private static string CreateDeviceId() => Guid.NewGuid().ToString("N");

        /// <summary>
        /// Validates persisted device ids so corrupted platform storage does not poison future auth requests.
        /// </summary>
        private static bool IsValidDeviceId(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length != 32)
            {
                return false;
            }

            foreach (var ch in value)
            {
                var isHex =
                    ch >= '0' && ch <= '9' ||
                    ch >= 'a' && ch <= 'f' ||
                    ch >= 'A' && ch <= 'F';

                if (!isHex)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
