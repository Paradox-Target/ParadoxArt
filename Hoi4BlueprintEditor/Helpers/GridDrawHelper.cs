using Hoi4BlueprintEditor.Constants;

namespace Hoi4BlueprintEditor.Helpers;

public static class GridDrawHelper
{
    private static double CellWidth => FocusMapConstants.CellWidth;
    private static double CellHeight => FocusMapConstants.CellHeight;

    private static void GetRange(
        double translate,
        double scale,
        double viewLength,
        double cellLength,
        out int start,
        out int end
    )
    {
        start = (int)(-translate / (cellLength * scale));
        end = (int)((viewLength - translate) / (cellLength * scale));
    }

    public static void GetXRange(
        double translate,
        double scale,
        double viewLength,
        out int start,
        out int end
    )
    {
        GetRange(translate, scale, viewLength, CellWidth, out start, out end);
    }

    public static void GetYRange(
        double translate,
        double scale,
        double viewLength,
        out int start,
        out int end
    )
    {
        GetRange(translate, scale, viewLength, CellHeight, out start, out end);
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
