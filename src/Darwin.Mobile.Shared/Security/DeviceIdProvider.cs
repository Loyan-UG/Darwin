using System;
using System.Threading.Tasks;
using Microsoft.Maui.Storage; // SecureStorage / Preferences

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
            // Return cached if available
            if (!string.IsNullOrWhiteSpace(_cached))
                return _cached;

            // Try SecureStorage first (preferred for secrets)
            try
            {
                var existing = await SecureStorage.GetAsync(Key).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(existing))
                {
                    _cached = existing;
                    return _cached;
                }

                // Generate new compact GUID and persist
                var id = Guid.NewGuid().ToString("N");

                // Persist using SecureStorage; may throw on some platforms/permissions.
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
                    var pref = Preferences.Get(Key, null);
                    if (!string.IsNullOrWhiteSpace(pref))
                    {
                        _cached = pref;
                        return _cached;
                    }

                    var id = Guid.NewGuid().ToString("N");
                    Preferences.Set(Key, id);
                    _cached = id;
                    return _cached;
                }
                catch
                {
                    // Last resort: keep an in-memory id (non-persistent). Good enough for tests.
                    _cached = Guid.NewGuid().ToString("N");
                    return _cached;
                }
            }
        }
    }
}