using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Desktop.Models;

namespace Desktop.Converters;

public class TabTypeToVisibilityConverter : IValueConverter
{
    public static readonly TabTypeToVisibilityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TabType tabType && parameter is string expectedType)
        {
            var expectedTabType = Enum.Parse<TabType>(expectedType);
            return tabType == expectedTabType;
        }

        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}