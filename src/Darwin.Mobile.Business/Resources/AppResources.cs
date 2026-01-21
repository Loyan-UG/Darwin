using System.Globalization;
using System.Resources;

namespace Darwin.Mobile.Business.Resources;

/// <summary>
/// Provides strongly typed access to localized application strings.
/// </summary>
public static class AppResources
{
    /// <summary>
    /// Gets or sets the culture used for resource lookup.
    /// </summary>
    public static CultureInfo? Culture { get; set; }

    private static readonly ResourceManager ResourceManager =
        new ResourceManager("Darwin.Mobile.Business.Resources.Strings", typeof(AppResources).Assembly);

    /// <summary>
    /// Title for the home page.
    /// </summary>
    public static string HomeTitle =>
        ResourceManager.GetString(nameof(HomeTitle), Culture) ?? "Home";

    /// <summary>
    /// Text for the start button on the home page.
    /// </summary>
    public static string StartButton =>
        ResourceManager.GetString(nameof(StartButton), Culture) ?? "Start";

    /// <summary>
    /// Title shown on pages that are not yet implemented.
    /// </summary>
    public static string ComingSoonTitle =>
        ResourceManager.GetString(nameof(ComingSoonTitle), Culture) ?? "Coming soon";
}
