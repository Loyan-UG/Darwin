using System;

namespace Darwin.Mobile.Consumer.Services.Navigation;

/// <summary>
/// Stores the short-lived business context used when opening the QR tab.
/// </summary>
public interface IQrNavigationContext
{
    /// <summary>
    /// Sets the business context that the next QR tab appearance should use.
    /// </summary>
    /// <param name="businessId">Business identifier used to prepare the scan session.</param>
    /// <param name="businessName">Optional business display name.</param>
    /// <param name="joined">Whether the QR open action follows a join or joined-business action.</param>
    void Set(Guid businessId, string? businessName = null, bool joined = false);

    /// <summary>
    /// Tries to read the latest QR navigation context.
    /// </summary>
    /// <param name="context">Resolved context when one is available.</param>
    /// <returns><c>true</c> when a non-empty business context is available.</returns>
    bool TryConsume(out QrNavigationContextSnapshot context);

    /// <summary>
    /// Clears any pending context once Shell query parameters were delivered successfully.
    /// </summary>
    void Clear();
}
