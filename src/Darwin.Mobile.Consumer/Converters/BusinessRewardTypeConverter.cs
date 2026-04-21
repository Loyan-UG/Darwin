using System;
using System.Globalization;
using Darwin.Mobile.Consumer.Resources;

namespace Darwin.Mobile.Consumer.Converters;

public sealed class BusinessRewardTypeConverter : Microsoft.Maui.Controls.IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var rewardType = value?.ToString();

        return rewardType switch
        {
            "FreeItem" => AppResources.BusinessRewardTypeFreeItem,
            "PercentDiscount" => AppResources.BusinessRewardTypePercentDiscount,
            "AmountDiscount" => AppResources.BusinessRewardTypeAmountDiscount,
            _ => AppResources.BusinessRewardTypeUnknown
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
