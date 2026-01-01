using System.Globalization;
using Avalonia.Data.Converters;

namespace Hoi4BlueprintBuilder.Core.Infrastructure.Converters;

public sealed class CoordinateToPixelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int coordinate && parameter is double multiplier)
        {
            return coordinate * multiplier;
        }
        return 0;
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        throw new NotSupportedException();
    }
}
