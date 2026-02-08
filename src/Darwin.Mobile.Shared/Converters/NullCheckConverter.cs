using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Shared.Converters
{
    /// <summary>
    /// Returns true when the input value is not null; false otherwise.
    /// Intended for bindings such as IsVisible="{Binding QrImage, Converter={StaticResource NullCheckConverter}}".
    /// </summary>
    public sealed class NullCheckConverter : IValueConverter
    {
        /// <summary>
        /// Convert: returns true if value is not null.
        /// </summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            // Null-safe check - returns a boxed bool compatible with XAML bindings.
            return value is not null;
        }

        /// <summary>
        /// ConvertBack is not supported for this one-way converter.
        /// </summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            // Explicitly throw to signal unsupported operation instead of returning a default.
            throw new NotSupportedException("NullCheckConverter does not support ConvertBack.");
        }
    }
}