using System;
using System.Globalization;
using Darwin.Mobile.Business.Resources;

namespace Darwin.Mobile.Business.Converters;

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
