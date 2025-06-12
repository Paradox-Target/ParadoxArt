using System.Globalization;
using System.Windows.Data;

namespace Hoi4BlueprintEditor.Core.Converters;

public class CoordinateToPixelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (
            value is int coordinate
            && parameter is string multiplierStr
            && double.TryParse(multiplierStr, out double multiplier)
        )
        {
            return coordinate * multiplier;
        }
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
