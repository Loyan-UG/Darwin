namespace Darwin.Mobile.Shared.Security
{
    /// <summary>
    /// Provides a stable, device-scoped installation identifier for this app instance.
    /// Implementations must persist the id so it survives app restarts.
    /// </summary>
    public interface IDeviceIdProvider
    {
        /// <summary>
        /// Returns a stable device id (non-null, non-empty). Implementation may generate and persist on first call.
        /// </summary>
        Task<string> GetDeviceIdAsync();
    }
}