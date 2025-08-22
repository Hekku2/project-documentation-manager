using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Desktop.Converters;

public class CountToBooleanConverter : IValueConverter
{
    public static readonly CountToBooleanConverter ZeroIsTrue = new() { TrueIfZero = true };
    public static readonly CountToBooleanConverter ZeroIsFalse = new() { TrueIfZero = false };

    public bool TrueIfZero { get; init; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return TrueIfZero ? count == 0 : count > 0;
        }

        return TrueIfZero;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}