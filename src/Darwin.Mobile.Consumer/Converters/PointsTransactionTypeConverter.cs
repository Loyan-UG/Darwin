using System;
using System.Globalization;
using Darwin.Mobile.Consumer.Resources;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Consumer.Converters;

public sealed class PointsTransactionTypeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var type = value?.ToString();
        if (string.IsNullOrWhiteSpace(type))
        {
            return string.Empty;
        }

        return type switch
        {
            "Accrual" => AppResources.RewardsTransactionTypeAccrual,
            "Redemption" => AppResources.RewardsTransactionTypeRedemption,
            "Adjustment" => AppResources.RewardsTransactionTypeAdjustment,
            _ => AppResources.RewardsTransactionTypeUnknown
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
