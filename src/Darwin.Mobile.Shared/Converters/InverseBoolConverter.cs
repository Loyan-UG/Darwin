using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Shared.Converters
{
    /// <summary>
    /// Converts a boolean to its inverse value.
    /// Useful in XAML when you want IsEnabled = !IsBusy.
    /// This converter is intentionally simple and returns 'true' for non-bool inputs.
    /// </summary>
    public sealed class InverseBoolConverter : IValueConverter
    {
        /// <inheritdoc />
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            // If value is null or not a bool, return a sensible default (enabled = true).
            return true;
        }

        /// <inheritdoc />
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return true;
        }
    }
}