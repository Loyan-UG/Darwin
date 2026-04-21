using System;
using System.Globalization;
using Darwin.Mobile.Consumer.Resources;

namespace Darwin.Mobile.Consumer.Converters;

public sealed class LocalizedBooleanConverter : Microsoft.Maui.Controls.IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            true => AppResources.CommonYes,
            false => AppResources.CommonNo,
            _ => AppResources.CommonNo
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
