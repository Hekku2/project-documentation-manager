using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Desktop.Converters;

public class ErrorTypeToColorConverter : IValueConverter
{
    public static readonly ErrorTypeToColorConverter Instance = new();
    
    private static readonly SolidColorBrush _errorBrush = new(Color.Parse("#FF6B6B")); // Red for errors
    private static readonly SolidColorBrush _warningBrush = new(Color.Parse("#FFD93D")); // Yellow for warnings  
    private static readonly SolidColorBrush _defaultBrush = new(Color.Parse("#CCCCCC")); // Default gray

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string errorType)
        {
            return errorType.ToLowerInvariant() switch
            {
                "error" => _errorBrush,
                "warning" => _warningBrush,
                _ => _defaultBrush
            };
        }

        return _defaultBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}