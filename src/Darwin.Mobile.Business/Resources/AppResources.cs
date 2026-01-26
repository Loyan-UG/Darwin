using System.Globalization;
using System.Resources;

namespace Darwin.Mobile.Business.Resources;

/// <summary>
/// Provides strongly typed access to localized application strings.
/// </summary>
public static class AppResources
{
    public static CultureInfo? Culture { get; set; }

    private static readonly ResourceManager ResourceManager =
        new ResourceManager("Darwin.Mobile.Business.Resources.Strings", typeof(AppResources).Assembly);

    public static string HomeTitle => ResourceManager.GetString(nameof(HomeTitle), Culture) ?? "Home";
    public static string StartButton => ResourceManager.GetString(nameof(StartButton), Culture) ?? "Start";
    public static string ComingSoonTitle => ResourceManager.GetString(nameof(ComingSoonTitle), Culture) ?? "Coming soon";

    // Newly added keys for scanner functionality
    public static string ScanTitle => ResourceManager.GetString(nameof(ScanTitle), Culture) ?? "Scan";
    public static string LastTokenLabel => ResourceManager.GetString(nameof(LastTokenLabel), Culture) ?? "Last token";
    public static string PointsLabel => ResourceManager.GetString(nameof(PointsLabel), Culture) ?? "Points";
    public static string AccrueButton => ResourceManager.GetString(nameof(AccrueButton), Culture) ?? "Confirm Accrual";
    public static string RedeemButton => ResourceManager.GetString(nameof(RedeemButton), Culture) ?? "Confirm Redemption";
}
