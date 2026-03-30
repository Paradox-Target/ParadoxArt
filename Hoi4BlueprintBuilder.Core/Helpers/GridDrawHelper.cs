namespace Hoi4BlueprintBuilder.Core.Helpers;

public static class GridDrawHelper
{
    public static (int Start, int End) GetXRange(
        double translate,
        double scale,
        double viewLength,
        double cellWidth
    )
    {
        return GetRange(translate, scale, viewLength, cellWidth);
    }

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

    public static double GetX(double translate, double scale, int index, double cellWidth)
    {
        return GetPos(translate, scale, index, cellWidth);
    }

    public static double GetY(double translate, double scale, int index, double cellHeight)
    {
        return GetPos(translate, scale, index, cellHeight);
    }

    private static double GetPos(double translate, double scale, int index, double cellWidth)
    {
        return translate + index * cellWidth * scale;
    }
}
