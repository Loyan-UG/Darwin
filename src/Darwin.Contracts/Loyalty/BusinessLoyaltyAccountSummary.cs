/// <summary>
/// Minimal loyalty account snapshot tailored for the business app when
/// processing a scan session.
/// </summary>
public sealed class BusinessLoyaltyAccountSummary
{
    /// <summary>
    /// Gets or sets the loyalty account identifier.
    /// </summary>
    public Guid LoyaltyAccountId { get; set; }

    /// <summary>
    /// Gets or sets the current points balance for the account.
    /// </summary>
    public int PointsBalance { get; set; }

    /// <summary>
    /// Gets or sets an optional human-friendly customer display name
    /// (if available on the server).
    /// </summary>
    public string? CustomerDisplayName { get; set; }
}
