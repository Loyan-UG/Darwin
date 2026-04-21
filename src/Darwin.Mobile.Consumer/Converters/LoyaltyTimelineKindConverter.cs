using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Consumer.Resources;

namespace Darwin.Mobile.Consumer.Converters;

public sealed class LoyaltyTimelineKindConverter : Microsoft.Maui.Controls.IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => value switch
        {
            LoyaltyTimelineEntryKind.PointsTransaction => AppResources.FeedTimelineKindPointsTransaction,
            LoyaltyTimelineEntryKind.RewardRedemption => AppResources.FeedTimelineKindRewardRedemption,
            _ => AppResources.FeedTimelineKindUnknown
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}
