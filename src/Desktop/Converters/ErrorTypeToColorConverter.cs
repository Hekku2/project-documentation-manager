using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Desktop.Converters;

public class ErrorTypeToColorConverter : IValueConverter
{
    public static readonly ErrorTypeToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string errorType)
        {
            return errorType.ToLowerInvariant() switch
            {
                "error" => new SolidColorBrush(Color.Parse("#FF6B6B")), // Red for errors
                "warning" => new SolidColorBrush(Color.Parse("#FFD93D")), // Yellow for warnings
                _ => new SolidColorBrush(Color.Parse("#CCCCCC")) // Default gray
            };
        }

        return new SolidColorBrush(Color.Parse("#CCCCCC"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}