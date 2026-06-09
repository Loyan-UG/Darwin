using System;
namespace Darwin.Mobile.Consumer.Services.Navigation;

/// <summary>
/// Thread-safe in-memory handoff for QR tab navigation.
/// </summary>
/// <remarks>
/// MAUI Shell can drop object query parameters when switching to an already-created root tab.
/// This context keeps QR navigation deterministic while still allowing normal Shell query values.
/// It stores only transient UI context and no secret or token material.
/// </remarks>
public sealed class QrNavigationContext : IQrNavigationContext
{
    private readonly object _gate = new();
    private QrNavigationContextSnapshot? _current;

    /// <inheritdoc />
    public void Set(Guid businessId, string? businessName = null, bool joined = false)
    {
        if (businessId == Guid.Empty)
        {
            return;
        }

        var snapshot = new QrNavigationContextSnapshot(
            businessId,
            string.IsNullOrWhiteSpace(businessName) ? null : businessName.Trim(),
            joined);

        lock (_gate)
        {
            _current = snapshot;
        }
    }

    /// <inheritdoc />
    public bool TryConsume(out QrNavigationContextSnapshot context)
    {
        lock (_gate)
        {
            if (_current is { BusinessId: var businessId } current &&
                businessId != Guid.Empty)
            {
                _current = null;
                context = current;
                return true;
            }
        }

        context = default;
        return false;
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_gate)
        {
            _current = null;
        }
    }
}

/// <summary>
/// Immutable QR navigation context snapshot.
/// </summary>
/// <param name="BusinessId">Business identifier used to prepare the scan session.</param>
/// <param name="BusinessName">Optional business display name.</param>
/// <param name="Joined">Whether the navigation follows a join or joined-business action.</param>
public readonly record struct QrNavigationContextSnapshot(Guid BusinessId, string? BusinessName, bool Joined);
