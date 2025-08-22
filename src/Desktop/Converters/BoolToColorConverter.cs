using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Desktop.Converters;

public class BoolToColorConverter : IValueConverter
{
    public static readonly BoolToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive)
        {
            // Active tab: bright blue background
            // Inactive tab: dark gray background
            return isActive ? new SolidColorBrush(Color.Parse("#007ACC")) : new SolidColorBrush(Color.Parse("#3E3E42"));
        }

        return new SolidColorBrush(Color.Parse("#3E3E42"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}