using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Desktop.Converters;

public class BoolToColorConverter : IValueConverter
{
    public static readonly BoolToColorConverter Instance = new();

    private static readonly ImmutableSolidColorBrush _activeBrush = new(Color.Parse("#007ACC"));
    private static readonly ImmutableSolidColorBrush _inactiveBrush = new(Color.Parse("#3E3E42"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive)
        {
            // Active tab: bright blue background
            // Inactive tab: dark gray background
            return isActive ? _activeBrush : _inactiveBrush;
        }

        return _inactiveBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}