using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Business.Converters;

/// <summary>
/// Converts a nullable string to a boolean indicating whether it is not null or empty.
/// </summary>
public sealed class NullOrEmptyToBoolConverter : IValueConverter
{
    /// <summary>
    /// Returns true if the input string is not null or empty; otherwise false.
    /// </summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return !string.IsNullOrEmpty(value as string);
    }

    /// <summary>
    /// ConvertBack is not supported and throws <see cref="NotSupportedException"/>.
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
