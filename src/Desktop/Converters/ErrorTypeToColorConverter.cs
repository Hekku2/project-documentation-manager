using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Desktop.Converters;

public class ErrorTypeToColorConverter : IValueConverter
{
    public static readonly ErrorTypeToColorConverter Instance = new();

    private static readonly IBrush _errorBrush = new ImmutableSolidColorBrush(Color.Parse("#FF6B6B")); // Red for errors
    private static readonly IBrush _warningBrush = new ImmutableSolidColorBrush(Color.Parse("#FFD93D")); // Yellow for warnings
    private static readonly IBrush _defaultBrush = new ImmutableSolidColorBrush(Color.Parse("#CCCCCC")); // Default gray
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string errorType)
        {
            if (errorType.Equals("error", StringComparison.OrdinalIgnoreCase))
                return _errorBrush;
            if (errorType.Equals("warning", StringComparison.OrdinalIgnoreCase))
                return _warningBrush;
            return _defaultBrush;
        }

        return _defaultBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}