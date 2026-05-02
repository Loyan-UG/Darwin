using Darwin.Contracts.Loyalty;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// Single-list projection used by the Discover page.
/// </summary>
/// <remarks>
/// The Discover screen has two mutually exclusive tabs, but both tabs should render through one virtualized list.
/// This wrapper keeps the page performant by avoiding nested collection controls while still preserving the
/// different card layouts and navigation policies for joined accounts and explore results.
/// </remarks>
public sealed class DiscoverDisplayItem
{
    private DiscoverDisplayItem(LoyaltyAccountSummary? joinedAccount, DiscoverExploreItem? exploreItem)
    {
        JoinedAccount = joinedAccount;
        ExploreItem = exploreItem;
    }

    /// <summary>
    /// Gets the joined loyalty account represented by this row, when the "My Businesses" tab is active.
    /// </summary>
    public LoyaltyAccountSummary? JoinedAccount { get; }

    /// <summary>
    /// Gets the explore result represented by this row, when the "Explore" tab is active.
    /// </summary>
    public DiscoverExploreItem? ExploreItem { get; }

    /// <summary>
    /// Gets whether this row should render the joined-account card template.
    /// </summary>
    public bool IsJoinedAccount => JoinedAccount is not null;

    /// <summary>
    /// Gets whether this row should render the explore-business card template.
    /// </summary>
    public bool IsExploreBusiness => ExploreItem is not null;

    /// <summary>
    /// Creates a display row for an existing joined loyalty account.
    /// </summary>
    /// <param name="account">Joined account payload.</param>
    /// <returns>A display row for the joined-account tab.</returns>
    public static DiscoverDisplayItem FromJoinedAccount(LoyaltyAccountSummary account)
    {
        ArgumentNullException.ThrowIfNull(account);
        return new DiscoverDisplayItem(account, null);
    }

    /// <summary>
    /// Creates a display row for an explore search result.
    /// </summary>
    /// <param name="item">Explore result payload.</param>
    /// <returns>A display row for the explore tab.</returns>
    public static DiscoverDisplayItem FromExploreItem(DiscoverExploreItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new DiscoverDisplayItem(null, item);
    }
}
