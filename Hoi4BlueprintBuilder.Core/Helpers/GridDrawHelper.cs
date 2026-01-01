using Hoi4BlueprintBuilder.Core.Constants;

namespace Hoi4BlueprintBuilder.Core.Helpers;

public static class GridDrawHelper
{
    private static double CellWidth => FocusMapConstants.CellWidth;
    private static double CellHeight => FocusMapConstants.CellHeight;

    private static (int Start, int End) GetRange(
        double translate,
        double scale,
        double viewLength,
        double cellLength
    )
    {
        int start = (int)(-translate / (cellLength * scale));
        int end = (int)((viewLength - translate) / (cellLength * scale));
        return (start, end);
    }

    public static (int Start, int End) GetXRange(double translate, double scale, double viewLength)
    {
        return GetRange(translate, scale, viewLength, CellWidth);
    }

    private static double GetPos(double translate, double scale, int index, double cellLength)
    {
        return translate + index * cellLength * scale;
    }

    public static double GetX(double translate, double scale, int index)
    {
        return GetPos(translate, scale, index, CellWidth);
    }

    public static double GetY(double translate, double scale, int index)
    {
        return GetPos(translate, scale, index, CellHeight);
    }
}
